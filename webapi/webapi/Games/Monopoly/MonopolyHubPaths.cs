namespace webapi.Games.Monopoly;

internal static class MonopolyHubPaths
{
	public const string GameStateChanged = "GameStateChanged";
	public const string ShowDiceRoll = "ShowDiceRoll";
	public const string ChatMessage = "ChatMessage";

	// incoming
	public const string RepeatLastOffer = "RepeatLastOffer";
	public const string SendChatMessage = "SendChatMessage";
}
