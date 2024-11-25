using Godot;
using System;
using System.Linq;

public partial class Player : CharacterBody2D, IStateOwner<Player.State>
{
	#region State enum

	public enum State
	{
		NonGroundIdle,
		Idle,
		Running,
		Jump,
		Fall,
		Landing,
		WallSliding,
		WallJump,
	}

	private readonly State[] groundStates = 
	{
		State.Idle, State.Running, State.Landing,
	};
	#endregion

	#region  Const
	public const float RunSpeed = 200.0f;
	public const float JumpVelocity = -300.0f;
	public const float FloorAcceleration = RunSpeed / 0.2f;
	public const float JumpAcceleration = RunSpeed / 0.1f;

	public Vector2 WallJumpVelocity = new Vector2(500, -300);
	#endregion
	
	#region Child

	public AnimationPlayer AnimationPlayer = new AnimationPlayer();
	public Timer CoyoteTimer = new Timer();
	public Timer JumpRequestTimer = new Timer();
	public StateMachine<State> stateMachine;

	public Node2D Graphics = new Node2D();
	public RayCast2D HandChecker = new RayCast2D();
    public RayCast2D FootChecker = new RayCast2D();

	public Sprite2D Sprite2D = new Sprite2D();
	#endregion
	private bool isFirstTick;

	private float gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

    private Player()
	{
		stateMachine = StateMachine<State>.Create(this);
	}
	public override void _Ready()
    {
		#region onready
		Sprite2D = GetNode<Sprite2D>("Graphics/Sprite2D");
		AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		CoyoteTimer = GetNode<Timer>("CoyoteTimer");
		JumpRequestTimer = GetNode<Timer>("JumpRequestTimer");
		Graphics = GetNode<Node2D>("Graphics");
		HandChecker = GetNode<RayCast2D>("Graphics/HandChecker");
		FootChecker = GetNode<RayCast2D>("Graphics/FootChecker");
		#endregion
	}

    public override void _UnhandledInput(InputEvent @event)
    {
        if(@event.IsActionPressed("jump"))
		{
			JumpRequestTimer.Start();
		}
		if(@event.IsActionReleased("jump"))
		{
			JumpRequestTimer.Stop();
			if(Velocity.Y < JumpVelocity / 2.0f)
				Velocity = Velocity with { Y = JumpVelocity / 2.0f };
		}
    }

    public void TransitionState(State fromState, State toState)
    {   
		//GD.Print($"[{nameof(Player)}][{Engine.GetPhysicsFrames()}] {fromState} => {toState}");

		if (!groundStates.Contains(fromState) && groundStates.Contains(toState))
			CoyoteTimer.Stop();
		
		switch(toState)
		{
			case State.Idle or State.NonGroundIdle:
                AnimationPlayer.Play("idle");
                break;
            case State.Running:
                AnimationPlayer.Play("running");
                break;
			case State.Jump:
				AnimationPlayer.Play("jump");
				Velocity = Velocity with { Y = JumpVelocity };
				CoyoteTimer.Stop();
				JumpRequestTimer.Stop();
				break;
			case State.Fall:
				AnimationPlayer.Play("fall");
				if (groundStates.Contains(fromState))
				{
					CoyoteTimer.Start();
				}
				break;
			case State.Landing:
			    AnimationPlayer.Play("landing");
				break;
			case State.WallSliding:
				AnimationPlayer.Play("wall_sliding");
				break;
			case State.WallJump:
			    AnimationPlayer.Play("jump");
				Velocity = WallJumpVelocity with { X = WallJumpVelocity.X * GetWallNormal().X };
                JumpRequestTimer.Stop();
				break;
		}

		isFirstTick = true;
    }

    public State GetNextState(State currentState, out bool keepCurrent)
    {
		keepCurrent = false;
        bool canJump = IsOnFloor() || CoyoteTimer.TimeLeft > 0;
		bool shouldJump = canJump && JumpRequestTimer.TimeLeft > 0;
		if (shouldJump)
		{
			return State.Jump;
		}

		if (groundStates.Contains(currentState) && !IsOnFloor())
            return State.Fall;

		var movement = Input.GetAxis("move_left", "move_right");
        var isStill = Mathf.IsZeroApprox(movement) && Mathf.IsZeroApprox(Velocity.X);
		
		switch(currentState)
		{
			case State.NonGroundIdle:
			    return IsOnFloor() ? State.Idle : State.Fall;
			case State.Idle:
				if (!isStill)
					return State.Running;
				break;
			case State.Running:
				if(isStill)
				    return State.Idle;
				break;
			case State.Jump:
				if(Velocity.Y >= 0)
					return State.Fall;
				break;
			case State.Fall:
				if (IsOnFloor())
					return isStill ? State.Landing : State.Running;
				if(CanWallSlide())
					return State.WallSliding;
				break;
			case State.Landing:
				if (!AnimationPlayer.IsPlaying())
					return State.Idle;
				break;	
			case State.WallSliding:
				if (JumpRequestTimer.TimeLeft > 0)
				    return State.WallJump;
				if (IsOnFloor())
					return State.Idle;
				if (!IsOnWall())
					return State.Fall;
				break;	
			case State.WallJump:
				if(CanWallSlide() && !isFirstTick)
					return State.WallSliding;
				if(Velocity.Y >= 0)
					return State.Fall;
				break;				
		}

		keepCurrent = true;
		return currentState;
    }

    public void TickPhysics(State currentState, double delta)
    {
        switch (currentState)
		{
			case State.Idle or State.NonGroundIdle:
				Move(gravity, delta);
				break;
			case State.Running:
                Move(gravity, delta);
                break;
            case State.Jump:
                Move(isFirstTick ? 0 : gravity, delta);
                break;
            case State.Fall:
                Move(gravity, delta);
                break;
            case State.Landing:
                Stand(gravity, delta);
                break;	
			case State.WallSliding:
                Move(gravity / 3, delta);
				Graphics.Scale = Graphics.Scale with { X = GetWallNormal().X };
                break;	
			case State.WallJump:
				if (stateMachine.startTime < 0.1)
					Stand(isFirstTick ? 0 : gravity, delta);
				else
			    	Move(gravity, delta);
				break;			
		}
		isFirstTick = false;
    }

	private void Stand(float gravity, double delta)
    {
        var acceleration = IsOnFloor() ? FloorAcceleration : JumpAcceleration;
        Velocity = Velocity with
        {
            Y = Velocity.Y + gravity * (float)delta,
            X = Mathf.MoveToward(Velocity.X, 0, acceleration * (float)delta)
        };

        MoveAndSlide();
    }

	private void Move(float gravity, double delta)
    {
        var direction = Input.GetAxis("move_left", "move_right");
		var acceleration = IsOnFloor() ? FloorAcceleration : JumpAcceleration;
        Velocity = Velocity with
        {
            Y = Velocity.Y + gravity * (float)delta,
            X = Mathf.MoveToward(Velocity.X, direction * RunSpeed, acceleration * (float)delta)
        };

        if (!Mathf.IsZeroApprox(direction))
		{
			Graphics.Scale = Graphics.Scale with { X = direction < 0 ? -1 : 1 };
		}

        MoveAndSlide();
    }

	private bool CanWallSlide()
	{
		return IsOnWall() && HandChecker.IsColliding() && FootChecker.IsColliding();
	}
}
