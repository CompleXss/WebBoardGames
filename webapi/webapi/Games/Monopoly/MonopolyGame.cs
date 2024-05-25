using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using webapi.Extensions;
using webapi.Models;

namespace webapi.Games.Monopoly;

public class MonopolyGame : PlayableGame
{
	public static void Test() { }



	private const int START_MONEY = 16_000;
	private const int PRISON_EXIT_PRICE = 500;
	private const int MAX_PRISON_FREE_TRIES = 3;
	private const int GAME_START_LAP_MONEY = 2000;
	private const int GAME_START_START_BONUS = 1000;

	private static readonly MonopolyMap map;
	private static readonly IReadOnlyList<string> layoutWithCornerCells;
	private static readonly object emptyObject = new();
	private static readonly IReadOnlyList<string> availablePlayerColors = ["#d98381", "#add884", "#dabc6a", "#44a7df", "#a281b6"];
	private static readonly int TOTAL_CELLS_COUNT;
	private static readonly int START_CELL_INDEX;
	private static readonly int PRISON_CELL_INDEX;
	private static readonly int PRISON_ENTER_CELL_INDEX;
	private readonly Random random = new();

	private readonly int[] playersMoney;
	private readonly int[] playerPositions;
	private readonly bool[] playersDead;
	private readonly string[] playerColors;
	private readonly (bool state, int freeExitTries)[] playersInPrison;
	private readonly MonopolyCellState?[] cellStates;
	private readonly MonopolyOfferManager offerManager;
	private readonly MonopolyLogger chatLogger;

	private readonly List<MonopolyPlayerAction.Type> expectedActionTypes = new(4);
	private int actingPlayerIndex;
	private int lapMoney = GAME_START_LAP_MONEY;
	private int startBonus = GAME_START_START_BONUS;
	private int payToPlayerMultiplier = 1;
	private int lastDiceSum;
	private bool lastDiceIsDouble;
	private byte doublesInRow;

	static MonopolyGame()
	{
		var readMap = Utils.ReadAsJson<MonopolyMap>("Games/Monopoly/monopoly_map.json");
		if (readMap is null)
			throw new Exception("Could not read and parse monopoly_map.json file.");

		var set = new Dictionary<string, int>();
		for (int i = 0; i < readMap.Layout.Count; i++)
		{
			var x = readMap.Layout[i];
			set.TryAdd(x, -1);
			readMap.Layout[i] += "_" + ++set[x];
		}

		map = readMap;
		TOTAL_CELLS_COUNT = map.CardsInLine * 4 + 4;

		var layout = new string[TOTAL_CELLS_COUNT];
		int corner = 0;

		for (int i = 0; i < TOTAL_CELLS_COUNT; i++)
		{
			if (i % (map.CardsInLine + 1) == 0)
			{
				layout[i] = map.CornerCards[corner].ID;
				corner++;
				continue;
			}

			layout[i] = map.Layout[i - corner];
		}

		layoutWithCornerCells = layout;


		START_CELL_INDEX = layout.IndexOf(x => x == "start");
		if (START_CELL_INDEX == -1) throw new Exception("Monopoly: Start cell not found");

		PRISON_CELL_INDEX = layout.IndexOf(x => x == "prison");
		if (PRISON_CELL_INDEX == -1) throw new Exception("Monopoly: Prison cell not found");

		PRISON_ENTER_CELL_INDEX = layout.IndexOf(x => x == "prisonEnter");
		if (PRISON_ENTER_CELL_INDEX == -1) throw new Exception("Monopoly: PrisonEnter cell not found");

		// todo portal cell index ???
	}

	public MonopolyGame(GameCore gameCore, IHubContext hub, IReadOnlyList<string> playerIDs)
		: base(gameCore, hub, playerIDs)
	{
		playersMoney = Enumerable.Repeat(START_MONEY, playerIDs.Count).ToArray();
		playerPositions = Enumerable.Repeat(0, playerIDs.Count).ToArray();
		playersDead = Enumerable.Repeat(false, playerIDs.Count).ToArray();
		playersInPrison = new (bool, int)[playerIDs.Count];
		playerColors = availablePlayerColors
			.Shuffle(random)
			.Take(playerIDs.Count)
			.ToArray();

		actingPlayerIndex = random.Next(0, playerIDs.Count);
		expectedActionTypes.Add(MonopolyPlayerAction.Type.DiceToMove);

		offerManager = new(SendHubMessage);
		offerManager.SetLastOfferIfNull(() => offerManager.OfferDiceRoll(actingPlayerIndex));
		chatLogger = new MonopolyLogger(128);

		cellStates = new MonopolyCellState?[layoutWithCornerCells.Count]; // entries for event cards are unused
		for (int i = 0; i < layoutWithCornerCells.Count; i++)
		{
			var cellID = layoutWithCornerCells[i];
			if (!cellID.StartsWith("g_"))
			{
				cellStates[i] = null;
				continue;
			}

			var group = GetCellGroup(cellID);
			if (group is null) // this is bad actually
				continue;

			if (!int.TryParse(cellID[(cellID.LastIndexOf('_') + 1)..], out var cardGroupIndex))
				continue; // also bad

			var card = group.Cards[cardGroupIndex];
			if (card is null)
				continue; // also bad

			cellStates[i] = new MonopolyCellState()
			{
				OwnerIndex = -1,
				Cost = card.BuyCost,
				Type = group.Type,
				Info = card,
				Multipliers = group.Multipliers,
			};
		}

		PlayerConnected += playerIndex => offerManager.RepeatLastOffer(playerIndex);
	}



	protected override bool IsPlayerTurn_Internal(string playerID)
	{
		return playerID == PlayerIDs[actingPlayerIndex];
	}

	protected override bool Surrender_Internal(string playerID)
	{
		int playerIndex = PlayerIDs.IndexOf(playerID);

		if (playerIndex == -1 || playersDead.Count(x => !x) < 2)
			return false;

		playersDead[playerIndex] = true;
		MakeNextPlayerActing();

		if (playersDead.Count(x => !x) == 1)
			WinnerID = PlayerIDs[playersDead.IndexOf(x => !x)];

		return true;
	}

	protected override bool Request_Internal(string playerID, object? data)
	{
		data = data?.ToString();

		if (data is not string request)
			return false;

		int playerIndex = PlayerIDs.IndexOf(playerID);
		if (playerIndex == -1)
			return false;

		if (request == MonopolyHubPaths.RepeatLastOffer)
		{
			offerManager.RepeatLastOffer(playerIndex);
			return true;
		}

		return false;
	}

	protected override bool TryUpdateState_Internal(string playerID, object data, out string error)
	{
		MonopolyPlayerAction action;

		try
		{
			action = JsonConvert.DeserializeObject<MonopolyPlayerAction>(data?.ToString()!);
		}
		catch (Exception)
		{
			error = "Неправильные данные хода.";
			return false;
		}


		// todo remove cw
		Console.WriteLine("=== New action ===");
		Console.WriteLine(action.ActionType.ToString());
		Console.WriteLine(action.Number);
		Console.WriteLine(action.CellID);



		if (!ValidateActionType(action.ActionType))
		{
			error = "Недопустимое действие в данный момент.";
			return false;
		}

		var expectedActionTypes_BACKUP = expectedActionTypes.ToArray();
		expectedActionTypes.Clear();

		error = string.Empty;
		//return false;

		bool actionResult = action.ActionType switch
		{
			//MonopolyPlayerAction.Type.Yes => Yes(),
			MonopolyPlayerAction.Type.No => No(expectedActionTypes_BACKUP),
			MonopolyPlayerAction.Type.Pay => Pay(),
			MonopolyPlayerAction.Type.PayToPlayer => PayToPlayer(),
			MonopolyPlayerAction.Type.DiceToMove => DiceToMove(),
			MonopolyPlayerAction.Type.DiceToExitPrison => DiceToExitPrison(),
			MonopolyPlayerAction.Type.BuyCell => BuyCell(),
			//MonopolyPlayerAction.Type.UpgradeCell => UpgradeCell(action),
			//MonopolyPlayerAction.Type.DowngradeCell => DowngradeCell(action),
			//MonopolyPlayerAction.Type.CreateContract => CreateContract(action, out error),
			_ => ReturnInvalidActionType(out error)
		};

		if (actionResult)
			UpdateGameState();
		else
		{
			expectedActionTypes.Clear();
			expectedActionTypes.AddRange(expectedActionTypes_BACKUP);
		}

		return actionResult;
	}

	private bool ValidateActionType(MonopolyPlayerAction.Type actionType)
	{
		if (expectedActionTypes.Contains(actionType))
			return true;

		if ((
				actionType == MonopolyPlayerAction.Type.UpgradeCell ||
				actionType == MonopolyPlayerAction.Type.DowngradeCell
			) && (
				expectedActionTypes.Contains(MonopolyPlayerAction.Type.DiceToMove) ||
				expectedActionTypes.Contains(MonopolyPlayerAction.Type.Pay))
			)
			return true;

		return false;
	}

	//private bool Yes()
	//{

	//}

	private bool No(IReadOnlyList<MonopolyPlayerAction.Type> expectedActionTypes)
	{
		int playerIndex = actingPlayerIndex;
		// todo: if no active contracts

		// don't buy cell
		if (expectedActionTypes.Contains(MonopolyPlayerAction.Type.BuyCell))
		{
			chatLogger.PlayerRefusesToBuyCell(PlayerIDs[playerIndex], layoutWithCornerCells[playerPositions[playerIndex]]);
			MakeNextPlayerActing();
			return true;
		}

		// todo No ???

		return false;
	}

	private bool Pay()
	{
		int playerIndex = actingPlayerIndex;

		// prison
		if (playersInPrison[playerIndex].state)
		{
			if (playersMoney[playerIndex] < PRISON_EXIT_PRICE)
				return false;

			playersMoney[playerIndex] -= PRISON_EXIT_PRICE;
			playersInPrison[playerIndex] = (false, 0);

			chatLogger.PlayerPaysToExitPrison(PlayerIDs[playerIndex], PRISON_EXIT_PRICE);

			// move player to safe prison zone to make move
			MovePlayerToCell(playerIndex, PRISON_CELL_INDEX);
			expectedActionTypes.Add(MonopolyPlayerAction.Type.DiceToMove);
			offerManager.OfferDiceRoll(playerIndex);

			return true;
		}

		// event pay
		int cellIndex = playerPositions[playerIndex];

		// todo: event pay (store last event pay amount ?)

		return true;
	}

	private bool PayToPlayer()
	{
		int playerIndex = actingPlayerIndex;

		int cellIndex = playerPositions[playerIndex];
		int amountToPay = GetPayToPlayerAmountFor(cellIndex);
		if (amountToPay == 0)
			return false;

		if (playersMoney[playerIndex] < amountToPay)
			return false;

		int cellOwnerIndex = cellStates[cellIndex]!.Value.OwnerIndex;
		if (cellOwnerIndex == -1)
			return false;

		playersMoney[playerIndex] -= amountToPay;
		playersMoney[cellOwnerIndex] += amountToPay * payToPlayerMultiplier;

		chatLogger.PlayerPaysRent(PlayerIDs[playerIndex], amountToPay);

		return true;
	}

	private bool DiceToMove()
	{
		GetRandomDiceRoll();

		if (lastDiceIsDouble)
			doublesInRow++;

		if (doublesInRow > 2)
		{
			lastDiceIsDouble = false;
			doublesInRow = 0;
			chatLogger.PlayerDidTooManyDoublesAndEnteredPrison(PlayerIDs[actingPlayerIndex]);
			MovePlayerToPrison(actingPlayerIndex);
			return true;
		}

		MovePlayerForward(actingPlayerIndex, lastDiceSum);
		return true;
	}

	private bool DiceToExitPrison()
	{
		GetRandomDiceRoll();

		var (inPrison, freeExitTries) = playersInPrison[actingPlayerIndex];
		if (!inPrison)
			return false;

		if (freeExitTries >= MAX_PRISON_FREE_TRIES)
			return false; // should be unreachable

		if (lastDiceIsDouble)
		{
			playersInPrison[actingPlayerIndex] = new(false, 0);
			MovePlayerForward(actingPlayerIndex, lastDiceSum);
			chatLogger.PlayerExitedPrisonForFree(PlayerIDs[actingPlayerIndex]);
		}
		else
		{
			playersInPrison[actingPlayerIndex] = new(true, freeExitTries + 1);
			int triesLeft = MAX_PRISON_FREE_TRIES - (freeExitTries + 1);
			MakeNextPlayerActing();
			chatLogger.PlayerCouldNotExitPrison(PlayerIDs[actingPlayerIndex], triesLeft);
		}
		return true;
	}

	private bool BuyCell()
	{
		var cellIndex = playerPositions[actingPlayerIndex];
		var cellState = cellStates[cellIndex];

		if (!cellState.HasValue || cellState.Value.OwnerIndex != -1)
			return false;

		int buyCost = cellState.Value.Info.BuyCost;

		if (playersMoney[actingPlayerIndex] < buyCost)
			return false;

		playersMoney[actingPlayerIndex] -= buyCost;

		cellStates[cellIndex] = cellState.Value with
		{
			OwnerIndex = actingPlayerIndex,
			Cost = (
				cellState.Value.Info.Rent?.First()
				?? cellState.Value.Multipliers?[GetPlayerCellsCountOfType(actingPlayerIndex, cellState.Value.Type)]
				?? 0
			)
		};

		chatLogger.PlayerBuysCell(PlayerIDs[actingPlayerIndex], layoutWithCornerCells[cellIndex], buyCost);

		MakeNextPlayerActing();
		return true;
	}

	private int GetPlayerCellsCountOfType(int playerIndex, string cellType)
	{
		return cellStates.Count(x =>
			x.HasValue &&
			x.Value.OwnerIndex == playerIndex &&
			x.Value.Type == cellType
		);
	}

	//private bool UpgradeCell(in MonopolyPlayerAction action)
	//{

	//}

	//private bool DowngradeCell(in MonopolyPlayerAction action)
	//{

	//}

	//private bool CreateContract(in MonopolyPlayerAction action, out string error)
	//{

	//}

	private static bool ReturnInvalidActionType(out string error)
	{
		error = "Неверный тип действия или действие еще не реализовано.";
		return false;
	}





	private (int, int) GetRandomDiceRoll()
	{
		var dice = (
			random.Next(1, 7),
			random.Next(1, 7)
		);

		lastDiceSum = dice.Item1 + dice.Item2;
		lastDiceIsDouble = dice.Item1 == dice.Item2;

		ShowDiceRoll(dice);
		chatLogger.PlayerDiceRolled(PlayerIDs[actingPlayerIndex], dice);

		return dice;
	}



	private void ApplyCellEffect(int playerIndex, int cellIndex)
	{
		var cellID = layoutWithCornerCells[cellIndex];

		if (cellIndex % (map.CardsInLine + 1) == 0)
		{
			if (ApplyCornerCellEffect(playerIndex, cellID))
				MakeNextPlayerActing();

			return;
		}

		bool turnEnded = false;

		if (cellID.StartsWith("g_"))
		{
			turnEnded = ApplyNormalCellEffect(playerIndex, cellIndex);
		}
		else if (cellID.StartsWith("event_"))
		{
			turnEnded = ApplyEventCellEffect(playerIndex, cellID[(cellID.IndexOf('_') + 1)..]);
		}

		if (turnEnded)
			MakeNextPlayerActing();
		else if (expectedActionTypes.Count == 0)
		{
			// should not be happening
			expectedActionTypes.Add(MonopolyPlayerAction.Type.DiceToMove);
		}
	}

	/// <returns>
	/// Effect is completely applied. Turn ended.
	/// </returns>
	private bool ApplyNormalCellEffect(int playerIndex, int cellIndex)
	{
		if (!cellStates[cellIndex].HasValue)
			return true;

		var cell = cellStates[cellIndex]!.Value;
		int cellOwnerIndex = cell.OwnerIndex;

		if (cellOwnerIndex == -1)
		{
			expectedActionTypes.Add(MonopolyPlayerAction.Type.BuyCell);
			expectedActionTypes.Add(MonopolyPlayerAction.Type.No);
			offerManager.OfferBuyCell(playerIndex, layoutWithCornerCells[cellIndex]);

			chatLogger.PlayerThinksAboutBuyingCell(PlayerIDs[playerIndex], layoutWithCornerCells[cellIndex]);
			return false;
		}

		if (cell.IsSold)
		{
			chatLogger.PlayerStepsOnSoldCellAndShouldNotPay(PlayerIDs[playerIndex], layoutWithCornerCells[cellIndex]);
			return true;
		}

		if (cell.OwnerIndex == playerIndex)
		{
			chatLogger.PlayerStepsOnHisCell(PlayerIDs[playerIndex]);
			return true;
		}

		int moneyToPay = GetPayToPlayerAmountFor(cellIndex);

		expectedActionTypes.Add(MonopolyPlayerAction.Type.PayToPlayer);
		offerManager.OfferPayToPlayer(playerIndex, cellOwnerIndex, moneyToPay);

		chatLogger.PlayerShouldPayRent(PlayerIDs[playerIndex], PlayerIDs[cellOwnerIndex], layoutWithCornerCells[cellIndex], moneyToPay);
		return false;
	}

	/// <returns>
	/// Effect is completely applied. Turn ended.
	/// </returns>
	private bool ApplyEventCellEffect(int playerIndex, string eventName)
	{
		// todo event cells

		switch (eventName)
		{
			case "random":
				break;

			case "money":
				break;

			case "star":
				break;

			default: break;
		}

		return true;
	}

	/// <returns>
	/// Effect is completely applied. Turn ended.
	/// </returns>
	private bool ApplyCornerCellEffect(int playerIndex, string cornerName)
	{
		switch (cornerName)
		{
			case "start":
				if (startBonus > 0)
				{
					playersMoney[playerIndex] += startBonus;
					chatLogger.PlayerGotStartBonus(PlayerIDs[playerIndex], startBonus);
				}

				return true;

			case "prison":
				return true;

			case "portal":
				MovePlayerForward(playerIndex, random.Next(1, 7));
				return false;

			case "prisonEnter":
				chatLogger.PlayerWentToPrison(PlayerIDs[playerIndex]);
				MovePlayerToPrison(playerIndex);
				return true;

			default: break;
		}

		return true;
	}

	private int GetPayToPlayerAmountFor(int cellIndex)
	{
		if (!cellStates[cellIndex].HasValue)
			return 0;

		var cell = cellStates[cellIndex]!.Value;

		int moneyToPay = 0;
		switch (cell.Type)
		{
			case "upgrade":
				moneyToPay = cell.Cost;
				break;

			case "count":
				if (cell.Multipliers is not null)
					moneyToPay = cell.Multipliers[cellStates.Count(x => x.HasValue && x.Value.OwnerIndex == cell.OwnerIndex)];
				break;

			case "dice":
				if (cell.Multipliers is not null)
					moneyToPay = cell.Multipliers[cellStates.Count(x => x.HasValue && x.Value.OwnerIndex == cell.OwnerIndex)] * lastDiceSum;
				break;

			default: break;
		}

		return moneyToPay;
	}



	private void MovePlayerForward(int playerIndex, int cellsCount)
	{
		int oldPos = playerPositions[playerIndex];
		int pos = oldPos + cellsCount;

		if (pos >= TOTAL_CELLS_COUNT)
			pos -= TOTAL_CELLS_COUNT;

		// if new lap
		if (oldPos < START_CELL_INDEX && pos >= START_CELL_INDEX)
			playersMoney[playerIndex] += lapMoney;

		if (layoutWithCornerCells[pos] == "prison")
			chatLogger.PlayerGotPrisonExcursion(PlayerIDs[playerIndex]);

		MovePlayerToCell(playerIndex, pos);
	}

	private void MovePlayerToPrison(int playerIndex)
	{
		playersInPrison[playerIndex] = (true, 0);
		MovePlayerToCell(playerIndex, PRISON_CELL_INDEX);
	}

	private void MovePlayerToCell(int playerIndex, int cellIndex)
	{
		playerPositions[playerIndex] = cellIndex;
		ApplyCellEffect(playerIndex, cellIndex);
	}

	private void MakeNextPlayerActing()
	{
		if (playersDead.All(x => x))
			return;

		if (lastDiceIsDouble && !playersDead[actingPlayerIndex])
		{
			lastDiceIsDouble = false;
			expectedActionTypes.Add(MonopolyPlayerAction.Type.DiceToMove);
			offerManager.OfferDiceRoll(actingPlayerIndex);

			return; // move one more time
		}

		var playerIndex = actingPlayerIndex;
		do
		{
			playerIndex++;
			if (playerIndex == PlayerIDs.Count)
				playerIndex = 0;
		}
		while (playersDead[playerIndex]);

		actingPlayerIndex = playerIndex;

		// deside what should next player do

		// if in prison
		if (playersInPrison[playerIndex].state)
		{
			if (playersInPrison[playerIndex].freeExitTries < MAX_PRISON_FREE_TRIES)
			{
				expectedActionTypes.Add(MonopolyPlayerAction.Type.Pay);
				expectedActionTypes.Add(MonopolyPlayerAction.Type.DiceToExitPrison);
				offerManager.OfferExitPrison(playerIndex, PRISON_EXIT_PRICE, MAX_PRISON_FREE_TRIES - playersInPrison[playerIndex].freeExitTries);
			}
			else
			{
				expectedActionTypes.Add(MonopolyPlayerAction.Type.Pay);
				offerManager.OfferPay(playerIndex, PRISON_EXIT_PRICE, MonopolyOfferManager.ExitPrison);
			}

			return;
		}

		// if on normal cell
		expectedActionTypes.Add(MonopolyPlayerAction.Type.DiceToMove);
		offerManager.OfferDiceRoll(playerIndex);


		// todo?
	}

	private void ShowDiceRoll((int, int) dice)
	{
		SendHubMessage(MonopolyHubPaths.ShowDiceRoll, null, new
		{
			dice1 = dice.Item1,
			dice2 = dice.Item2,
		});
	}

	private void UpdateGameState()
	{
		// todo
		// update lapMoney, startBonus depending on GameStarted
	}

	protected override void SendChatMessage_Internal(string message)
	{
		// todo
	}

	protected override object? GetRelativeState_Internal(string playerID)
	{
		int playerIndex = PlayerIDs.IndexOf(playerID);
		if (playerIndex == -1)
			return emptyObject;

		var playerStates = new Dictionary<string, MonopolyPlayerStateDto>(PlayerIDs.Count);
		for (int i = 0; i < PlayerIDs.Count; i++)
		{
			var position = layoutWithCornerCells[playerPositions[i]];
			if (position == "prison")
			{
				position += playersInPrison[i].state
					? "_2"
					: "_1";
			}

			playerStates[PlayerIDs[i]] = new()
			{
				Color = playerColors[i],
				IsOnline = IsPlayerConnected(i),
				IsDead = playersDead[i],
				Money = playersMoney[i],
				Position = position,
			};
		}

		var cellStates = new Dictionary<string, MonopolyCellStateDto>(this.cellStates.Length);
		for (int i = 0; i < this.cellStates.Length; i++)
			if (this.cellStates[i].HasValue)
			{
				cellStates.Add(layoutWithCornerCells[i], this.cellStates[i]!.Value.ToDto(playerIdx => playerIdx == -1 ? null : PlayerIDs[playerIdx]));
			}

		return new MonopolyGameStateDto
		{
			MyID = playerID,
			Players = playerStates,
			CellStates = cellStates,
			ChatMessages = chatLogger.Messages,
		};
	}

	private static MonopolyMap.CardGroup? GetCellGroup(string cellID)
	{
		var groupID = cellID[..cellID.LastIndexOf('_')];
		return map.CardGroups.Find(x => x.ID == groupID);
	}
}
