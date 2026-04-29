using Godot;
using System;
using System.Collections.Generic;

public partial class Board : Control
{
	[Export] public PackedScene BlockTemplate; // Drag KlotskiBlock.tscn here in Inspector
	[Export] public int CellSize = 100;
	
	private Vector2I _gridSize = new Vector2I(4, 5);
	private List<KlotskiBlock> _blocks = new();
	private KlotskiBlock _selectedBlock = null;

	public override void _Ready()
	{
		// For debugging, let's spawn a test layout
		SpawnInitialLayout();
	}

	private void SpawnInitialLayout()
	{
		// Format: ID, Position(x,y), Size(w,h), Color
		CreateBlock("Hero", new Vector2I(1, 0), new Vector2I(2, 2), Colors.Crimson);
		CreateBlock("Vert1", new Vector2I(0, 0), new Vector2I(1, 2), Colors.RoyalBlue);
		CreateBlock("Vert2", new Vector2I(3, 0), new Vector2I(1, 2), Colors.RoyalBlue);
		CreateBlock("Small1", new Vector2I(1, 2), new Vector2I(1, 1), Colors.ForestGreen);
	}
	
	// Inside KlotskiBlock.cs
	public void SetHighlight(bool active)
	{
		var style = GetThemeStylebox("panel") as StyleBoxFlat;
		if (style != null)
		{
			style.BorderColor = active ? Colors.White : new Color(0, 0, 0, 0.3f);
			style.SetBorderWidthAll(active ? 4 : 2);
		}
	}

	private void CreateBlock(string id, Vector2I pos, Vector2I size, Color color)
	{
		var block = BlockTemplate.Instantiate<KlotskiBlock>();
		AddChild(block);
		block.Setup(id, pos, size, CellSize, color);
		_blocks.Add(block);
	}

	// This is the core logic for your Data Viz / Solver
	public bool CanMove(KlotskiBlock block, Vector2I direction)
	{
		Vector2I newPos = block.GridPos + direction;

		// 1. Boundary Check
		if (newPos.X < 0 || newPos.Y < 0 || 
			newPos.X + block.BlockSize.X > _gridSize.X || 
			newPos.Y + block.BlockSize.Y > _gridSize.Y)
			return false;

		// 2. Collision Check against other blocks
		foreach (var other in _blocks)
		{
			if (other == block) continue;

			// Simple AABB (Axis-Aligned Bounding Box) check for grid cells
			if (newPos.X < other.GridPos.X + other.BlockSize.X &&
				newPos.X + block.BlockSize.X > other.GridPos.X &&
				newPos.Y < other.GridPos.Y + other.BlockSize.Y &&
				newPos.Y + block.BlockSize.Y > other.GridPos.Y)
			{
				return false;
			}
		}
		return true;
	}

	public void SelectBlock(KlotskiBlock block)
	{
		_selectedBlock = block;
		GD.Print($"Selected: {block.ID}");
	}

	public override void _Input(InputEvent @event)
	{
		if (_selectedBlock == null) return;

		Vector2I dir = Vector2I.Zero;

		if (@event.IsActionPressed("ui_up")) dir = Vector2I.Up;
		if (@event.IsActionPressed("ui_down")) dir = Vector2I.Down;
		if (@event.IsActionPressed("ui_left")) dir = Vector2I.Left;
		if (@event.IsActionPressed("ui_right")) dir = Vector2I.Right;

		if (dir != Vector2I.Zero && CanMove(_selectedBlock, dir))
		{
			_selectedBlock.SlideTo(_selectedBlock.GridPos + dir);
		}
	}
}
