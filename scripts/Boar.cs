using Godot;
using System;
using System.Diagnostics;

public partial class Boar : Enemy, IStateOwner<Boar.State>
{
	#region State enum

    public enum State
    {
        Idle,
        Run,
        Walk
    }

    #endregion

    #region Child
    public RayCast2D wallChecker = new RayCast2D();
    
    public RayCast2D floorChecker = new RayCast2D();
    
    public RayCast2D playerChecker = new RayCast2D();

    public Timer calmDownTimer = new Timer();

    public StateMachine<State> stateMachine;
    #endregion

	public Boar()
    {
        stateMachine = StateMachine<State>.Create(this);
    }

    public override void _Ready()
    {
        #region OnReady
        wallChecker = GetNode<RayCast2D>("Graphics/WallChecker");
        floorChecker = GetNode<RayCast2D>("Graphics/FloorChecker");
        playerChecker = GetNode<RayCast2D>("Graphics/PlayerChecker");
        calmDownTimer = GetNode<Timer>("CalmDownTimer");
        AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        graphics = GetNode<Node2D>("Graphics");
        #endregion
    }

    public void TransitionState(State fromState, State toState)
    {
        GD.Print($"[{nameof(Boar)}][{Engine.GetPhysicsFrames()}] {fromState} => {toState}");

        switch(toState)
        {
            case State.Idle:
                AnimationPlayer.Play("idle");
                if (wallChecker.IsColliding())
                    Direction = (DirectionEnum)((int)Direction * -1);
                break;
            
            case State.Walk:
                AnimationPlayer.Play("walk");
                if (!floorChecker.IsColliding())
                {
                    Direction = (DirectionEnum)((int)Direction * -1);
                    floorChecker.ForceRaycastUpdate();
                }
                break;

            case State.Run:
                AnimationPlayer.Play("run");
                break;
        }
    }

    public State GetNextState(State currentState, out bool keepCurrent)
    {
        keepCurrent = false;
        
        switch(currentState)
        {
            case State.Idle:
                if (CanSeePlayer())
                    return State.Run;
                if (stateMachine.startTime > 2)
                    return State.Walk;
                break;

            case State.Walk:
                if (CanSeePlayer())
                    return State.Run;
                if (wallChecker.IsColliding() || !floorChecker.IsColliding())
                    return State.Idle;
                break;

            case State.Run:
                if (!CanSeePlayer() && calmDownTimer.IsStopped())
                    return State.Walk;
                break;
        }

        keepCurrent = true;
        return currentState;
    }

    public void TickPhysics(State currentState, double delta)
    {
        switch(currentState)
        {
            case State.Idle:
                Move(0, delta);
                break;

            case State.Walk:
                Move(MaxSpeed / 3, delta);
                break;

            case State.Run:
                if (wallChecker.IsColliding() || !floorChecker.IsColliding())
                    Direction = (DirectionEnum)((int)Direction * -1);
                Move(MaxSpeed, delta);
                if (CanSeePlayer())
                    calmDownTimer.Start();
                break;
        }
    }

    private bool CanSeePlayer()
    {
        if (!playerChecker.IsColliding())
            return false;
        return playerChecker.GetCollider() is Player;
    }

}
