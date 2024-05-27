namespace webapi.Games.Monopoly;

internal readonly struct MonopolyPlayerAction
{
	public Type ActionType { get; init; }
	public string? CellID { get; init; }
	public MonopolyContractInfo? ContractInfo { get; init; }



	public enum Type
	{
		Yes,
		No,
		Pay,
		PayToPlayer,
		DiceToMove,
		DiceToExitPrison,
		BuyCell,
		UpgradeCell,
		DowngradeCell,
		CreateContract,
	}
}

internal readonly struct MonopolyContractInfo
{
	public string Player1_ID { get; init; }
	public string Player2_ID { get; init; }

	public int Player1_Money { get; init; }
	public int Player2_Money { get; init; }

	public string[] Player1_cells { get; init; }
	public string[] Player2_cells { get; init; }
}

internal readonly struct MonopolyCellState
{
	/// <summary>
	/// -1 if none.
	/// </summary>
	public int OwnerIndex { get; init; }
	public int Cost { get; init; }
	public int UpgradeLevel { get; init; }
	public bool IsSold { get; init; }
	public int MovesLeftToLooseThisCell { get; init; }
	public string Type { get; init; }
	public MonopolyMap.Card Info { get; init; }
	public List<int>? Multipliers { get; init; }

	public MonopolyCellState(int ownerIndex,
						  int cost,
						  int upgradeLevel,
						  bool isSold,
						  int movesLeftToLooseThisCell,
						  string type,
						  MonopolyMap.Card info,
						  List<int>? multipliers)
	{
		OwnerIndex = ownerIndex;
		Cost = cost;
		UpgradeLevel = upgradeLevel;
		IsSold = isSold;
		MovesLeftToLooseThisCell = movesLeftToLooseThisCell;
		Type = type;
		Info = info;
		Multipliers = multipliers;
	}

	public MonopolyCellStateDto ToDto(Func<int, string?> ownerIndexToIDFunc) => new()
	{
		OwnerID = ownerIndexToIDFunc(OwnerIndex),
		Cost = Cost,
		UpgradeLevel = UpgradeLevel,
		IsSold = IsSold,
		MovesLeftToLooseThisCell = MovesLeftToLooseThisCell,
		Type = Type,
		Info = Info,
		Multipliers = Multipliers,
	};
}



internal readonly struct MonopolyGameStateDto
{
	public required string MyID { get; init; }
	public required bool IsMyTurn { get; init; }
	public required bool IsAbleToUpgrade { get; init; }
	public required Dictionary<string, MonopolyPlayerStateDto> Players { get; init; }
	public required Dictionary<string, MonopolyCellStateDto> CellStates { get; init; }
	public required IReadOnlyList<string> ChatMessages { get; init; }
}

internal readonly struct MonopolyPlayerStateDto
{
	public required string Color { get; init; }
	public required bool IsOnline { get; init; }
	public required bool IsDead { get; init; }
	public required int Money { get; init; }
	public required string Position { get; init; }
}

internal readonly struct MonopolyCellStateDto
{
	public required string? OwnerID { get; init; }
	public required int Cost { get; init; }
	public required int UpgradeLevel { get; init; }
	public required bool IsSold { get; init; }
	public required int MovesLeftToLooseThisCell { get; init; }
	public required string Type { get; init; }
	public required MonopolyMap.Card Info { get; init; }
	public required List<int>? Multipliers { get; init; }
}
