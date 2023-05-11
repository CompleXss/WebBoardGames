using Microsoft.AspNetCore.SignalR;

namespace webapi.Hubs;

public class CheckersLobbyHub : Hub
{
	public async Task Test(string message)
	{
		string? name = Context.User?.Claims.FirstOrDefault(x => x.Type == "name")!.Value;
		await Clients.All.SendAsync($"Mes from {name}: " + message);
	}
}
