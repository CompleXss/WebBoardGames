namespace webapi.Games.Monopoly;

internal class MonopolyOfferManager
{
	// offer consts
	private const string OFFER_DICE_ROLL = "OfferDiceRoll";
	private const string OFFER_PAY = "OfferPay";
	private const string OFFER_PAY_TO_PLAYER = "OfferPayToPlayer";
	private const string OFFER_EXIT_PRISON = "OfferExitPrison";
	private const string OFFER_BUY_CELL = "OfferBuyCell";

	// pay reasons
	public const string ExitPrison = "ExitPrison";



	private readonly Action<string, int?, object?> sendHubMessage;
	private int lastOfferPlayerIndex;
	private Action? lastOffer;

	public MonopolyOfferManager(Action<string, int?, object?> sendHubMessage)
	{
		this.sendHubMessage = sendHubMessage;
	}



	private void Offer(string hubPath, int playerIndex, object? arg = null)
	{
		lastOffer =
			() => sendHubMessage(hubPath, playerIndex, arg);

		lastOfferPlayerIndex = playerIndex;
		//lastOffer.Invoke();
	}

	public void RepeatLastOffer(int playerIndex)
	{
		if (lastOfferPlayerIndex == playerIndex)
			lastOffer?.Invoke();
	}

	public void SetLastOfferIfNull(Action? lastOffer)
	{
		this.lastOffer ??= lastOffer;
	}



	public void OfferDiceRoll(int playerIndex)
	{
		Offer(OFFER_DICE_ROLL, playerIndex);
	}

	public void OfferPay(int playerIndex, int amount, string reason)
	{
		Offer(OFFER_PAY, playerIndex, new
		{
			amount,
			reason,
		});
	}

	public void OfferPayToPlayer(int playerIndex, int payToPlayerIndex, int amount)
	{
		Offer(OFFER_PAY_TO_PLAYER, playerIndex, new
		{
			payToPlayerIndex,
			amount,
		});
	}

	public void OfferExitPrison(int playerIndex, int payAmount, int triesLeft)
	{
		Offer(OFFER_EXIT_PRISON, playerIndex, new
		{
			amount = payAmount,
			triesLeft,
		});
	}

	public void OfferBuyCell(int playerIndex, string cellID)
	{
		Offer(OFFER_BUY_CELL, playerIndex, new
		{
			cellID,
		});
	}
}
