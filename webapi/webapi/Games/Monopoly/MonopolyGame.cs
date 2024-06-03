using Microsoft.AspNetCore.SignalR;
using webapi.Extensions;
using webapi.Models;

namespace webapi.Games.Monopoly;

public class MonopolyGame : PlayableGame
{
	private const int START_MONEY = 16_000;
	private const int PRISON_EXIT_PRICE = 500;
	private const int MAX_PRISON_FREE_TRIES = 3;
	private const int GAME_START_LAP_BONUS = 2000;
	private const int GAME_START_START_BONUS = 1000;
	private const int MOVES_TO_LOOSE_CELL = 16;
	private static readonly IReadOnlyList<(string message, int amount)> randomEvents_Money = [
		($"находит в зимней куртке {MonopolyLogger.FormatMoney(500)}", 500),
		($"находит на дороге {MonopolyLogger.FormatMoney(250)} мелочью", 250),
		($"участвует в лотерее и выигрывает {MonopolyLogger.FormatMoney(1500)}", 1500),
		($"участвует в лотерее и выигрывает {MonopolyLogger.FormatMoney(1000)}", 1000),
		($"занимает второе место в конкурсе красоты и получает {MonopolyLogger.FormatMoney(750)}", 750),
		($"занимает третье место в конкурсе красоты и получает {MonopolyLogger.FormatMoney(500)}", 500),

		($"играет в казино и проигрывает {MonopolyLogger.FormatMoney(1000)}", -1000),
		($"тратит в парке развлечений {MonopolyLogger.FormatMoney(250)}", -250),
		($"забывает выключить утюг и платит {MonopolyLogger.FormatMoney(750)} на ремонт", -750),
		($"попадает на распродажу и тратит там {MonopolyLogger.FormatMoney(750)}", -750),
		($"понимает, что любимое авто больше не заводится и платит за ремонт {MonopolyLogger.FormatMoney(500)}", -500),
	];

	private static readonly MonopolyMap map;
	private static readonly IReadOnlyList<string> availablePlayerColors = ["#d98381", "#add884", "#dabc6a", "#44a7df", "#a281b6"];
	private static readonly IReadOnlyList<string> layoutWithCornerCells;
	private static readonly object emptyObject = new();
	private static readonly int TOTAL_CELLS_COUNT;
	private static readonly int START_CELL_INDEX;
	private static readonly int PRISON_CELL_INDEX;
	private readonly Random random = new();

	private readonly int[] playersMoney;
	private readonly int[] playerPositions;
	private readonly bool[] playersDead;
	private readonly string[] playerColors;
	private readonly (bool state, int freeExitTries)[] playersInPrison;
	private readonly MonopolyCellState?[] cellStates;
	private readonly MonopolyOfferManager offerManager;
	private readonly MonopolyLogger chatLogger;

	private readonly List<MonopolyPlayerAction.Type> expectedActionTypes = new(2);
	private int actingPlayerIndex;
	private int lapBonus = GAME_START_LAP_BONUS;
	private int startBonus = GAME_START_START_BONUS;
	private int payToPlayerMultiplier = 1;
	private float positiveMoneyEventChance = 0.5f;
	private int lastEventPaySum;
	private int lastDiceSum;
	private int upgradedCellIndexThisTurn;
	private int doublesInRow;
	private bool lastDiceIsDouble;

	static MonopolyGame()
	{
		var readMap = Utils.ReadAsJson<MonopolyMap>("Games/Monopoly/monopoly_map.json")
			?? throw new Exception("Could not read and parse monopoly_map.json file.");

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
		chatLogger = new MonopolyLogger(256);

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
		chatLogger.PlayerSurrenders(playerID);

		// clear all user's cells
		for (int i = 0; i < cellStates.Length; i++)
		{
			if (!cellStates[i].HasValue || cellStates[i]!.Value.OwnerIndex != playerIndex)
				continue;

			cellStates[i] = cellStates[i]!.Value with
			{
				OwnerIndex = -1,
				Cost = cellStates[i]!.Value.Info.BuyCost,
				UpgradeLevel = 0,
				MovesLeftToLooseThisCell = 0,
				IsSold = false,
			};
		}

		MakeNextPlayerActing();

		if (playersDead.Count(x => !x) == 1)
			WinnerID = PlayerIDs[playersDead.IndexOf(x => !x)];
		else
			SendHubMessage(MonopolyHubPaths.GameStateChanged, null);

		return true;
	}

	protected override bool Request_Internal(string playerID, string request, object? data)
	{
		int playerIndex = PlayerIDs.IndexOf(playerID);
		if (playerIndex == -1)
			return false;

		if (request == MonopolyHubPaths.RepeatLastOffer)
		{
			offerManager.RepeatLastOffer(playerIndex);
			return true;
		}

		if (request == MonopolyHubPaths.SendChatMessage)
		{
			if (data?.ToString() is not string message)
				return false;

			if (message.Length > 512)
				return false;

			message = chatLogger.SendChatMessage(playerID, message);
			SendHubMessage(MonopolyHubPaths.ChatMessage, null, message);
			return true;
		}

		return false;
	}

	protected override bool TryUpdateState_Internal(string playerID, object data, out string error)
	{
		if (!TryDeserializeData(data, out MonopolyPlayerAction action))
		{
			error = "Неправильные данные хода.";
			return false;
		}



		// todo remove cw
		Console.WriteLine("=== New action ===");
		Console.WriteLine(action.ActionType.ToString());
		Console.WriteLine(action.CellID);



		if (!ValidateActionType(action.ActionType))
		{
			error = "Недопустимое действие в данный момент.";
			return false;
		}

		var expectedActionTypes_BACKUP = expectedActionTypes.ToArray();
		expectedActionTypes.Clear();

		bool actionResult = action.ActionType switch
		{
			//MonopolyPlayerAction.Type.Yes => Yes(),
			MonopolyPlayerAction.Type.No => No(expectedActionTypes_BACKUP),
			MonopolyPlayerAction.Type.Pay => Pay(),
			MonopolyPlayerAction.Type.PayToPlayer => PayToPlayer(),
			MonopolyPlayerAction.Type.DiceToMove => DiceToMove(),
			MonopolyPlayerAction.Type.DiceToExitPrison => DiceToExitPrison(),
			MonopolyPlayerAction.Type.BuyCell => BuyCell(),
			MonopolyPlayerAction.Type.UpgradeCell => UpgradeCell(action.CellID, expectedActionTypes_BACKUP),
			MonopolyPlayerAction.Type.DowngradeCell => DowngradeCell(action.CellID, expectedActionTypes_BACKUP),
			//MonopolyPlayerAction.Type.CreateContract => CreateContract(action, out error),
			_ => ReturnInvalidActionType(out error)
		};

		if (actionResult)
		{
			error = string.Empty;
			UpdateGameState();
		}
		else
		{
			error = "Так походить нельзя.";
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
				expectedActionTypes.Contains(MonopolyPlayerAction.Type.Pay) ||
				expectedActionTypes.Contains(MonopolyPlayerAction.Type.BuyCell))
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
			playerPositions[playerIndex] = PRISON_CELL_INDEX;
			expectedActionTypes.Add(MonopolyPlayerAction.Type.DiceToMove);
			offerManager.OfferDiceRoll(playerIndex);

			return true;
		}

		// event pay
		if (playersMoney[playerIndex] < lastEventPaySum)
			return false;

		playersMoney[playerIndex] -= lastEventPaySum;
		lastEventPaySum = 0;

		expectedActionTypes.Add(MonopolyPlayerAction.Type.DiceToMove);
		offerManager.OfferDiceRoll(playerIndex);

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

		MakeNextPlayerActing();
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
			lastDiceIsDouble = false;
			doublesInRow = 0;
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
			UpgradeLevel = 0,
			IsSold = false,
			MovesLeftToLooseThisCell = 0,
			Cost = (
				cellState.Value.Info.Rent?.First()
				?? cellState.Value.Multipliers?[GetPlayerCellsCountOfType(actingPlayerIndex, cellState.Value.Type)]
				?? 0 // should be unreachable
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

	private bool UpgradeCell(string? cellID, IReadOnlyList<MonopolyPlayerAction.Type> expectedActionTypes)
	{
		if (cellID is null)
			return false;

		int cellIndex = layoutWithCornerCells.IndexOf(cellID);
		if (cellIndex == -1)
			return false;

		if (!cellStates[cellIndex].HasValue)
			return false;

		int playerIndex = actingPlayerIndex;
		var cell = cellStates[cellIndex]!.Value;

		if (cell.OwnerIndex != playerIndex)
			return false;

		int moneyToPay;

		// rebuy
		if (cell.IsSold)
		{
			moneyToPay = cell.Info.RebuyCost;
			if (playersMoney[playerIndex] < moneyToPay)
				return false;

			playersMoney[playerIndex] -= moneyToPay;
			cellStates[cellIndex] = cell with
			{
				IsSold = false,
				MovesLeftToLooseThisCell = 0,
				UpgradeLevel = 0,
				Cost = (
					cell.Info.Rent?.First()
					?? cell.Multipliers?[GetPlayerCellsCountOfType(actingPlayerIndex, cell.Type) - 1]
					?? 0 // should be unreachable
				)
			};

			chatLogger.PlayerRebuysCell(PlayerIDs[playerIndex], cellID);

			this.expectedActionTypes.AddRange(expectedActionTypes);
			return true;
		}

		// upgrade
		if (cell.Type != "upgrade")
			return false;

		if (upgradedCellIndexThisTurn != -1 || !cell.Info.UpgradeCost.HasValue || cell.UpgradeLevel >= 5)
			return false;

		if (!IsPlayerOwnsAllCellsOfThisGroup(playerIndex, cellIndex))
			return false;

		var groupID = GetCellGroup(layoutWithCornerCells[cellIndex])?.ID;
		if (groupID is null)
			return false;

		if (cellStates.Any(
			x => x.HasValue &&
			x.Value.OwnerIndex == playerIndex &&
			GetCellGroup(layoutWithCornerCells[cellStates.IndexOf(x)])?.ID == groupID &&
			(
				x.Value.IsSold ||
				x.Value.UpgradeLevel < cell.UpgradeLevel
			))
		)
		{
			// can't upgrade if there are cells of the same group with lower upgradeLevel (or sold)
			return false;
		}

		moneyToPay = cell.Info.UpgradeCost.Value;
		if (playersMoney[playerIndex] < moneyToPay)
			return false;

		playersMoney[playerIndex] -= moneyToPay;
		cellStates[cellIndex] = cell with
		{
			UpgradeLevel = cell.UpgradeLevel + 1,
			Cost = (
				cell.Info.Rent?[cell.UpgradeLevel + 1]
				?? 0 // should be unreachable
			)
		};

		upgradedCellIndexThisTurn = cellIndex;
		chatLogger.PlayerUpgradesCell(PlayerIDs[playerIndex], cellID);

		this.expectedActionTypes.AddRange(expectedActionTypes);
		return true;
	}

	private bool DowngradeCell(string? cellID, IReadOnlyList<MonopolyPlayerAction.Type> expectedActionTypes)
	{
		if (cellID is null)
			return false;

		int cellIndex = layoutWithCornerCells.IndexOf(cellID);
		if (cellIndex == -1)
			return false;

		if (!cellStates[cellIndex].HasValue)
			return false;

		int playerIndex = actingPlayerIndex;
		var cell = cellStates[cellIndex]!.Value;

		if (cell.OwnerIndex != playerIndex)
			return false;

		if (cell.IsSold)
			return false;

		var groupID = GetCellGroup(layoutWithCornerCells[cellIndex])?.ID;
		if (groupID is null)
			return false;

		// sell
		if (cell.UpgradeLevel == 0 && !cellStates.Any(
			x => x.HasValue &&
			x.Value.OwnerIndex == playerIndex &&
			GetCellGroup(layoutWithCornerCells[cellStates.IndexOf(x)])?.ID == groupID &&
			x.Value.UpgradeLevel > 0))
		{
			playersMoney[playerIndex] += cell.Info.SellCost;
			cellStates[cellIndex] = cell with
			{
				IsSold = true,
				MovesLeftToLooseThisCell = MOVES_TO_LOOSE_CELL,
				Cost = 0
			};

			chatLogger.PlayerSellsCell(PlayerIDs[playerIndex], cellID);

			this.expectedActionTypes.AddRange(expectedActionTypes);
			return true;
		}

		// downgrade
		if (cell.Type != "upgrade" || cell.UpgradeLevel < 1)
			return false;

		if (cellStates.Any(
			x => x.HasValue &&
			x.Value.OwnerIndex == playerIndex &&
			GetCellGroup(layoutWithCornerCells[cellStates.IndexOf(x)])?.ID == groupID &&
			x.Value.UpgradeLevel > cell.UpgradeLevel)
		)
		{
			// can't downgrade if there are cells of the same group with higher upgradeLevel
			return false;
		}

		playersMoney[playerIndex] += cell.Info.SellCost;
		cellStates[cellIndex] = cell with
		{
			UpgradeLevel = cell.UpgradeLevel - 1,
			Cost = (
				cell.Info.Rent?[cell.UpgradeLevel - 1]
				?? 0 // should be unreachable
			)
		};

		// allow upgrade-downgrade of the same cell multiple times
		if (upgradedCellIndexThisTurn == cellIndex)
			upgradedCellIndexThisTurn = -1;

		chatLogger.PlayerDowngradesCell(PlayerIDs[playerIndex], cellID);

		this.expectedActionTypes.AddRange(expectedActionTypes);
		return true;
	}

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

		if (lastDiceIsDouble)
			chatLogger.PlayerDiceRolledDouble(PlayerIDs[actingPlayerIndex], dice);
		else
			chatLogger.PlayerDiceRolled(PlayerIDs[actingPlayerIndex], dice);

		return dice;
	}

	private bool IsPlayerOwnsAllCellsOfThisGroup(int playerIndex, int cellIndex)
	{
		var cellID = layoutWithCornerCells[cellIndex];
		if (!cellID.StartsWith("g_"))
			return false;

		var targetGroup = GetCellGroup(cellID);
		if (targetGroup is null) return false;

		int cellsCountInGroup = targetGroup.Cards.Count;

		var ownedCells = cellStates.Where(x => x.HasValue && x.Value.OwnerIndex == playerIndex).ToArray();
		if (ownedCells.Length < cellsCountInGroup)
			return false;

		int cellsCountOwned = ownedCells.Count(cell =>
		{
			int cellIndex = cellStates.IndexOf(cell);
			var cellID = layoutWithCornerCells[cellIndex];
			var group = GetCellGroup(cellID);
			if (group is null)
				return false;

			return group.ID == targetGroup.ID;
		});

		return cellsCountOwned == cellsCountInGroup;
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
			turnEnded = ApplyEventCellEffect(playerIndex, cellID[(cellID.IndexOf('_') + 1)..cellID.LastIndexOf('_')]);
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
		string playerID = PlayerIDs[playerIndex];
		float rng = random.NextSingle();

		switch (eventName)
		{
			case "random":
				const float step = 1f / 12;

				switch (rng)
				{
					case < step: // happy birthday
						const int happyB_cost = 500;
						int moneyGathered = 0;

						for (int i = 0; i < playersMoney.Length; i++)
						{
							if (i == playerIndex) continue;
							int paid = Math.Min(playersMoney[i], happyB_cost);
							playersMoney[i] -= paid;
							moneyGathered += paid;
						}
						playersMoney[playerIndex] += moneyGathered;

						chatLogger.Event_PlayerEntersRandomEvent(playerID, $"празднует день рождения! Все остальные скидываются на подарок по {MonopolyLogger.FormatMoney(happyB_cost)}");
						break;

					case < step * 2: // star dependent repair
						const int smallStarCost = 250;
						const int bigStarCost = 1000;

						int total = cellStates
							.Where(x => x.HasValue && x.Value.OwnerIndex == playerIndex && !x.Value.IsSold && x.Value.Type == "upgrade")
							.Select(x => x!.Value.UpgradeLevel)
							.Select(x => x == 5 ? bigStarCost : x * smallStarCost)
							.Sum();

						chatLogger.Event_PlayerEntersRandomEvent(playerID, $"проводит капитальный ремонт своих зданий и должен заплатить по {MonopolyLogger.FormatMoney(smallStarCost)} за каждую маленькую звезду и по {MonopolyLogger.FormatMoney(bigStarCost)} за каждую большую. Всего: {MonopolyLogger.FormatMoney(total)}");

						if (total > 0)
						{
							lastEventPaySum = total;
							expectedActionTypes.Add(MonopolyPlayerAction.Type.Pay);
							offerManager.OfferPay(playerIndex, total, MonopolyOfferManager.EventRandom);
							return false;
						}
						break;

					case < step * 3: // move forward
						chatLogger.Event_PlayerEntersRandomEvent(playerID, "отправляется в путешествие");
						MovePlayerForward(playerIndex, random.Next(1, 7));
						return false;

					case < step * 4: // go to prison
						chatLogger.Event_PlayerEntersRandomEvent(playerID, "отправляется в тюрьму за отмывание денег");
						MovePlayerToPrison(playerIndex);
						return false;

					default: // money +/-
						int index = random.Next(0, randomEvents_Money.Count);
						var (message, amount) = randomEvents_Money[index];

						chatLogger.Event_PlayerEntersRandomEvent(playerID, message);

						if (amount > 0)
						{
							playersMoney[playerIndex] += amount;
							return true;
						}
						else
						{
							lastEventPaySum = amount;
							expectedActionTypes.Add(MonopolyPlayerAction.Type.Pay);
							offerManager.OfferPay(playerIndex, -amount, MonopolyOfferManager.EventRandom);
							return false;
						}
				}
				break;

			case "money":
				if (rng < positiveMoneyEventChance)
				{
					int amount = 250 * random.Next(1, 4);

					playersMoney[playerIndex] += amount;
					chatLogger.EventMoney_PlayerGetsDividendsFromBanK(playerID, amount);
					return true;
				}
				else
				{
					int amount = 1000 + 500 * random.Next(1, 3);

					lastEventPaySum = amount;
					chatLogger.EventMoney_PlayerShouldPayToBank(playerID, amount);

					expectedActionTypes.Add(MonopolyPlayerAction.Type.Pay);
					offerManager.OfferPay(playerIndex, amount, MonopolyOfferManager.EventMoney);
					return false;
				}

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

		if (cell.IsSold || cell.OwnerIndex == -1)
			return 0;

		int moneyToPay = 0;
		switch (cell.Type)
		{
			case "upgrade":
				moneyToPay = cell.Cost;
				break;

			case "count":
				if (cell.Multipliers is not null)
					moneyToPay = cell.Multipliers[GetPlayerCellsCountOfType(cell.OwnerIndex, cell.Type) - 1];
				break;

			case "dice":
				if (cell.Multipliers is not null)
					moneyToPay = cell.Multipliers[GetPlayerCellsCountOfType(cell.OwnerIndex, cell.Type) - 1] * lastDiceSum;
				break;

			default: break;
		}

		return moneyToPay;
	}



	private void MovePlayerForward(int playerIndex, int cellsCount)
	{
		int pos = playerPositions[playerIndex] + cellsCount;
		if (pos >= TOTAL_CELLS_COUNT)
			pos -= TOTAL_CELLS_COUNT;

		// if new lap
		if (lapBonus > 0 && (pos - cellsCount) < START_CELL_INDEX && pos >= START_CELL_INDEX)
		{
			playersMoney[playerIndex] += lapBonus;
			chatLogger.PlayerGotLapBonus(PlayerIDs[playerIndex], lapBonus);
		}

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

		doublesInRow = 0;
		upgradedCellIndexThisTurn = -1;
		IncrementMovesLeftToLooseCellForAllSold();

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
	}

	private void ShowDiceRoll((int, int) dice)
	{
		SendHubMessage(MonopolyHubPaths.ShowDiceRoll, null, new
		{
			dice1 = dice.Item1,
			dice2 = dice.Item2,
		});
	}



	private void IncrementMovesLeftToLooseCellForAllSold()
	{
		for (int i = 0; i < cellStates.Length; i++)
		{
			if (!cellStates[i].HasValue) continue;
			var cell = cellStates[i]!.Value;

			if (!cell.IsSold)
				continue;

			int movesLeftToLooseThisCell = cell.MovesLeftToLooseThisCell - 1;
			bool isSold = movesLeftToLooseThisCell > 0;

			if (!isSold && cell.OwnerIndex != -1)
				chatLogger.PlayerLostSoldCell(PlayerIDs[cell.OwnerIndex], layoutWithCornerCells[i]);

			cellStates[i] = cell with
			{
				IsSold = isSold,
				MovesLeftToLooseThisCell = movesLeftToLooseThisCell,
				OwnerIndex = isSold ? cell.OwnerIndex : -1,
				Cost = isSold ? cell.Cost : cell.Info.BuyCost,
			};
		}
	}

	private void UpdateCountAndDiceCellsCost()
	{
		for (int i = 0; i < cellStates.Length; i++)
		{
			if (!cellStates[i].HasValue) continue;
			var cell = cellStates[i]!.Value;

			if (cell.OwnerIndex == -1 || cell.Type != "count" && cell.Type != "dice")
				continue;

			cellStates[i] = cell with
			{
				Cost = cell.IsSold ? 0
					: cell.Multipliers?[GetPlayerCellsCountOfType(cell.OwnerIndex, cell.Type) - 1]
					?? 0 // should be unreachable
			};
		}
	}

	private void UpdateGameState()
	{
		UpdateCountAndDiceCellsCost();

		// todo
		// update lapMoney, startBonus, payToPlayer multiplier depending on GameStarted
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
			IsMyTurn = IsPlayerTurn(playerID),
			IsAbleToUpgrade = upgradedCellIndexThisTurn == -1,
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
