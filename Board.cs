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
	//overloading for bfs usage. this should be able to be run without the use of the animations.
	public bool CanMove(byte[,] grid, Vector2I currentPos, Vector2I size, Vector2I direction)
	{
		Vector2I nextPos = currentPos + direction;

		for (int x = 0; x < size.X; x++)
		{
			for (int y = 0; y < size.Y; y++)
			{
				int targetX = nextPos.X + x;
				int targetY = nextPos.Y + y;

				// 1. Check Board Boundaries
				if (targetX < 0 || targetX >= 4 || targetY < 0 || targetY >= 5) 
					return false;

				// 2. Check if cell is occupied by ANOTHER block
				// It's okay if targetX/Y is inside the block's current area
				bool isInsideOwnSelf = (targetX >= currentPos.X && targetX < currentPos.X + size.X &&
										targetY >= currentPos.Y && targetY < currentPos.Y + size.Y);
				
				if (!isInsideOwnSelf && grid[targetX, targetY] != (byte)'.')
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
	//put 
	public byte[,] GetState2D()
	{
		byte[,] grid = new byte[4, 5];
		// empty the grid first. then fill.
		for (int y = 0; y < 5; y++)
			for (int x = 0; x < 4; x++)
				grid[x, y] = (byte)'.';

		foreach (var block in _blocks)
		{
			byte symbol = GetSymbolForBlock(block);
			for (int x = 0; x < block.BlockSize.X; x++)
			{
				for (int y = 0; y < block.BlockSize.Y; y++)
				{
					grid[block.GridPos.X + x, block.GridPos.Y + y] = symbol;
				}
			}
		}
		return grid;
	}
	
	public serializeState(byte[,] grid){
		char[] flattenedState = new char[20];
		int ind = 0;
		for(int i = 0; i < 5; i++){
			for(int i = 0; i < 5; i++){
			 	flattendState = (char)grid[x,y];
			}
		}
		return new string(flattenedState);
	}
}
