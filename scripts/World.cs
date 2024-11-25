using Godot;
using System;

public partial class World : Node2D
{

	TileMapLayer tileMapLayer = new TileMapLayer();

	Camera2D camera2D = new Camera2D();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		#region onready
		tileMapLayer = GetNode<TileMapLayer>("TileMap/Geometry");
		camera2D = GetNode<Camera2D>("Player/Camera2D");
		#endregion

		var used = tileMapLayer.GetUsedRect();
		var tileSize = tileMapLayer.TileSet.TileSize;

		camera2D.LimitTop = used.Position.Y * tileSize.Y;
		camera2D.LimitBottom = used.End.Y * tileSize.Y;
		camera2D.LimitLeft = used.Position.X * tileSize.X;
		camera2D.LimitRight = used.End.X * tileSize.X;

		camera2D.ResetSmoothing();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
