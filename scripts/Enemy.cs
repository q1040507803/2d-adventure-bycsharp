using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	private float gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
	
	public const float MaxSpeed = 230;    
	public const float Acceleration = MaxSpeed / 0.1f;

	#region Child
	protected Node2D graphics = new Node2D();
	private CollisionShape2D collisionShape = new CollisionShape2D();
    protected AnimationPlayer AnimationPlayer = new AnimationPlayer();
	
	#endregion
	protected enum DirectionEnum
	{
		Left = -1,
		Right = 1
	}
	private DirectionEnum direction = DirectionEnum.Left;

	protected DirectionEnum Direction
	{
		get => direction;
		set => SetDirection(value);

	}

	private async void SetDirection(DirectionEnum value)
	{
		if (direction == value)
		    return;
		if (!IsNodeReady())
			await ToSignal(this, Node.SignalName.Ready);
		
		direction = value;
		graphics.Scale = graphics.Scale with { X = -(int)direction };
	}

    public override void _Ready()
    {
        graphics = GetNode<Node2D>("Graphics");
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
    }

    protected void Move(float speed, double delta)
    {
        Velocity = Velocity with
        {
            X = Mathf.MoveToward(Velocity.X, speed * (int)Direction, Acceleration * (float)delta),
            Y = Velocity.Y + gravity * (float)delta
        };
        MoveAndSlide();
    }

}
