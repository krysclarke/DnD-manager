using Microsoft.AspNetCore.SignalR;

namespace DnDManager.Web;

public class EncounterHub : Hub {
    private readonly Func<WebEncounterState> _stateProvider;

    public EncounterHub(Func<WebEncounterState> stateProvider) {
        _stateProvider = stateProvider;
    }

    public override async Task OnConnectedAsync() {
        var state = _stateProvider();
        await Clients.Caller.SendAsync("ReceiveFullState", state);
        await base.OnConnectedAsync();
    }
}
