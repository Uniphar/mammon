namespace Mammon.Actors;

public abstract class ActorBase<TActorState>(ActorHost host) : Actor(host) where TActorState : new()
{
	public async Task<TActorState> GetStateAsync(string stateName)
	{
		var stateAttempt = await StateManager.TryGetStateAsync<TActorState>(stateName);
		return (!stateAttempt.HasValue) ? new TActorState() : stateAttempt.Value;
	}

	public async Task SaveStateAsync(string stateName, TActorState state)
	{
		await StateManager.SetStateAsync(stateName, state);
	}
}
