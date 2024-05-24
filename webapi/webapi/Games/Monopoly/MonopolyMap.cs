namespace webapi.Games.Monopoly;

public class MonopolyMap
{
	public required int CardsInLine { get; init; }
	public required List<CornerCard> CornerCards { get; init; }
	public required List<CardGroup> CardGroups { get; init; }
	public required List<string> Layout { get; init; }

	public class CornerCard
	{
		public required string ID { get; init; }
	}

	public class CardGroup
	{
		public required string ID { get; init; }
		public required string Type { get; init; }
		public required List<Card> Cards { get; init; }
		public required List<int>? Multipliers { get; init; }
	}

	public class Card
	{
		public required List<int>? Rent { get; init; }
		public required int BuyCost { get; init; }
		public required int SellCost { get; init; }
		public required int RebuyCost { get; init; }
		public required int? UpgradeCost { get; init; }
	}
}
