namespace webapi.Games.Monopoly;

// todo: use string.Format ???

internal class MonopolyLogger(Action<string> logDelegate)
{
	private const string PLAYER_ID = "playerID";
	private const string TRIES_LEFT = "triesLeft";
	private const string AMOUNT = "amount";

	private readonly Action<string> log = logDelegate;

	public void PlayerDidTooManyDoublesAndEnteredPrison(string playerID)
	{
		log($"{{{PLAYER_ID}:{playerID}}} выкинул слишком много дублей и попал в тюрьму за мошенничество.");
	}

	public void PlayerCouldNotExitPrison(string playerID, int triesLeft)
	{
		log($"У {{{PLAYER_ID}:{playerID}}} не получилось выкинуть дубль, игрок остается в тюрьме. Осталось попыток: {{{TRIES_LEFT}:{triesLeft}}}.");
	}

	public void PlayerExitedPrison(string playerID)
	{
		log($"{{{PLAYER_ID}:{playerID}}} выкинул дубль и вышел и тюрьмы.");
	}

	public void PlayerGotStartBonus(string playerID, int amount)
	{
		log($"{{{PLAYER_ID}:{playerID}}} попал точно на старт и получил за это {{{AMOUNT}:{amount}}}k.");
	}

	public void PlayerWentToPrison(string playerID)
	{
		log($"{{{PLAYER_ID}:{playerID}}} попал в тюрьму.");
	}

	public void PlayerGotPrisonExcursion(string playerID)
	{
		log($"{{{PLAYER_ID}:{playerID}}} пришел в тюрьму с экскурсией. Ничего интересного не произошло.");
	}
}
