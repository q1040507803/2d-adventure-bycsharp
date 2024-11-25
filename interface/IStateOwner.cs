using Godot;
using System;

public interface IStateOwner<TState>
{
	public void TransitionState(TState fromState, TState toState);

	public TState GetNextState(TState currentState, out bool keepCurrent);

	public void TickPhysics(TState currentState, double delta);

}