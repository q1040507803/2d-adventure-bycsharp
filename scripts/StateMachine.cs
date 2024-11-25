using Godot;
using System;

[Tool]
public partial class StateMachine<TState> : Node
	where TState : struct, Enum
{
	public double startTime;

	private TState currentState;

	public TState CurrentState
	{
		get => currentState;
		set
		{
			if (Owner is not IStateOwner<TState> owner)
				return;
			owner.TransitionState(CurrentState, value);
			currentState = value;
			startTime = 0;
		}
	}

	public static StateMachine<TState> Create(Node owner)
	{
		var machine = new StateMachine<TState>();
		machine.Name = "StateMachine";
		owner.AddChild(machine);
		machine.Owner = owner;
		return machine;
	}

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		await ToSignal(Owner, Node.SignalName.Ready);
		CurrentState = Enum.GetValues<TState>()[0];
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (Owner is not IStateOwner<TState> owner)
			return;
		
		while (true)
		{
			var nextState = owner.GetNextState(currentState, out var keepCurrent);
			if (keepCurrent)
				break;
			CurrentState = nextState;
		}
		
		owner.TickPhysics(currentState, delta);	
		startTime += delta;	
	}
}
