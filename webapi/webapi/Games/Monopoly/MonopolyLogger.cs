using webapi.Extensions;

namespace webapi.Games.Monopoly;

internal class MonopolyLogger(int capacity = 128)
{
	private const string PLAYER_ID = "playerID";
	private const string CELL_ID = "cellID";

	public IReadOnlyList<string> Messages => messages;
	private readonly List<string> messages = new(capacity);

	private void Log(string message) => messages.Add(message);



	public void PlayerDiceRolled(string playerID, (int, int) dice)
	{
		Log(GetPlayerTemplate(playerID) + $" выбрасывает {dice.Item1}:{dice.Item2}");
	}

	public void PlayerGotLapBonus(string playerID, int amount)
	{
		Log(GetPlayerTemplate(playerID) + " проходит очередной круг и получает бонус " + FormatMoney(amount));
	}

	public void PlayerGotStartBonus(string playerID, int amount)
	{
		Log(GetPlayerTemplate(playerID) + " попадает точно на Старт и получает бонус " + FormatMoney(amount));
	}


	#region Pay to player
	public void PlayerShouldPayRent(string playerID, string payToPlayerID, string cellID, int amount)
	{
		Log(GetPlayerTemplate(playerID) + $" попадает на {GetCellIDTemplate(cellID)} и должен заплатить {GetPlayerTemplate(payToPlayerID)} {FormatMoney(amount)}");
	}

	public void PlayerPaysRent(string playerID, int amount)
	{
		Log(GetPlayerTemplate(playerID) + " платит аренду " + FormatMoney(amount));
	}

	public void PlayerStepsOnHisCell(string playerID)
	{
		Log(GetPlayerTemplate(playerID) + " попадает на свое поле");
	}

	public void PlayerStepsOnSoldCellAndShouldNotPay(string playerID, string cellID)
	{
		Log(GetPlayerTemplate(playerID) + $" попадает на заложенное поле {GetCellIDTemplate(cellID)} и ничего не платит");
	}
	#endregion


	#region Buy cell
	public void PlayerThinksAboutBuyingCell(string playerID, string cellID)
	{
		Log(GetPlayerTemplate(playerID) + $" попадает на {GetCellIDTemplate(cellID)} и задумывается о покупке");
	}

	public void PlayerBuysCell(string playerID, string cellID, int cost)
	{
		Log(GetPlayerTemplate(playerID) + $" покупает {GetCellIDTemplate(cellID)} за {FormatMoney(cost)}");
	}

	public void PlayerRefusesToBuyCell(string playerID, string cellID)
	{
		Log(GetPlayerTemplate(playerID) + " отказывается от покупки " + GetCellIDTemplate(cellID));
	}
	#endregion


	#region Events



	#endregion


	#region Prison
	public void PlayerWentToPrison(string playerID)
	{
		Log(GetPlayerTemplate(playerID) + " попадается полиции и отправляется в тюрьму");
	}

	public void PlayerDidTooManyDoublesAndEnteredPrison(string playerID)
	{
		Log(GetPlayerTemplate(playerID) + " выбрасывает слишком много дублей и попадает в тюрьму за мошенничество");
	}

	public void PlayerCouldNotExitPrison(string playerID, int triesLeft)
	{
		Log(GetPlayerTemplate(playerID) + " не смог выкинуть дубль и остается в тюрьме. Осталось попыток: " + triesLeft);
	}

	public void PlayerExitedPrisonForFree(string playerID)
	{
		Log(GetPlayerTemplate(playerID) + " выбрасывает дубль и выходит и тюрьмы абсолютно бесплатно!");
	}

	public void PlayerPaysToExitPrison(string playerID, int paidAmount)
	{
		Log(GetPlayerTemplate(playerID) + $" платит {FormatMoney(paidAmount)} и выходит из тюрьмы");
	}

	public void PlayerGotPrisonExcursion(string playerID)
	{
		Log(GetPlayerTemplate(playerID) + " приходит в тюрьму с экскурсией. Ничего интересного не происходит");
	}
	#endregion



	#region Utils
	private static string GetPlayerTemplate(string playerID)
	{
		return $"{{{PLAYER_ID}:{playerID}}}";
	}

	private static string GetCellIDTemplate(string cellID)
	{
		return $"{{{CELL_ID}:{cellID}}}";
	}

	private static string FormatMoney(int amount)
	{
		return $"{Utils.FormatNumberWithCommas(amount.ToString())}k";
	}
	#endregion
}
