namespace Mammon.Actors;

public abstract class ActorBase<TActorState>(ActorHost host, StateManagerService stateManagerService) : Actor(host) where TActorState : new()
{
	public static string GetActorId(string reportId, string name, string subId) => $"{reportId}_{subId}_{name}";

	public async Task<TActorState> GetStateAsync(string stateName)
	{
		var stateAttempt = await stateManagerService.TryGetStateAsync<TActorState>(StateManager, stateName);
		return (!stateAttempt.HasValue) ? new TActorState() : stateAttempt.Value;
	}

	public async Task SaveStateAsync(string stateName, TActorState state)
	{
		await stateManagerService.SetStateAsync(StateManager, stateName, state);
	}
}

public class StateManagerService
{
	public virtual Task<ConditionalValue<T>> TryGetStateAsync<T>(IActorStateManager stateManager, string stateName) where T : new()
	{
		return stateManager.TryGetStateAsync<T>(stateName);
	}
	public virtual Task SetStateAsync<T>(IActorStateManager stateManager, string stateName, T state) where T : new()
	{
		return stateManager.SetStateAsync(stateName, state);
    }
}
