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
		
		GD.Print("Board ready. Launching BFS Solver...");
		SolvePuzzle();
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
	
	public string serializeState(byte[,] grid){
		char[] flattenedState = new char[20];
		int ind = 0;
		for(int i = 0; i < 5; i++){
			for(int i = 0; i < 5; i++){
			 	flattendState = (char)grid[x,y];
			}
		}
		return new string(flattenedState);
	}
	
	public List<byte[,]> GetNextStates(byte[,] currentGrid)
	{
		List<byte[,]> neighbors = new List<byte[,]>();

		// Define the directions: Right, Left, Down, Up
		Vector2I[] directions = { 
			new Vector2I(1, 0), new Vector2I(-1, 0), 
			new Vector2I(0, 1), new Vector2I(0, -1) 
		};

		// Find all unique blocks in this grid
		// (You can pass a list of block data to make this faster, 
		// but scanning the grid works too)
		var blocks = FindBlocksInGrid(currentGrid);

		foreach (var b in blocks)
		{
			foreach (var dir in directions)
			{
				if (CanMove(currentGrid, b.Pos, b.Size, dir))
				{
					neighbors.Add(ApplyMove(currentGrid, b.Pos, b.Size, dir));
				}
			}
		}
		return neighbors;
	}
	
	private byte[,] ApplyMove(byte[,] grid, Vector2I pos, Vector2I size, Vector2I dir)
	{
		byte[,] nextGrid = (byte[,])grid.Clone();
		byte symbol = grid[pos.X, pos.Y];

		// 1. Clear old position
		for (int x = 0; x < size.X; x++)
			for (int y = 0; y < size.Y; y++)
				nextGrid[pos.X + x, pos.Y + y] = (byte)'.';

		// 2. Fill new position
		for (int x = 0; x < size.X; x++)
			for (int y = 0; y < size.Y; y++)
				nextGrid[pos.X + dir.X + x, pos.Y + dir.Y + y] = symbol;

		return nextGrid;
	}
	
	public void SolvePuzzle()
	{
		// 1. DATA STRUCTURES
		Queue<byte[,]> queue = new Queue<byte[,]>();
		// Store the hash of the state as the Key, and the parent's hash as the Value
		Dictionary<string, string> visited = new Dictionary<string, string>();

		// 2. INITIAL STATE
		byte[,] startState = GetState2D();
		string startHash = GetStateString(startState);
		
		queue.Enqueue(startState);
		visited.Add(startHash, null); // Null means it's the root/starting node

		// 3. THE BFS LOOP
		while (queue.Count > 0)
		{
			byte[,] current = queue.Dequeue();
			string currentHash = GetStateString(current);

			// Check if this state is the winner (Hero at exit)
			if (IsWinState(current))
			{
				GD.Print("Solution Found!");
				DisplaySolution(visited, currentHash);
				return; 
			}

			// 4. EXPLORE NEIGHBORS
			foreach (byte[,] next in GetNextStates(current))
			{
				string nextHash = GetStateString(next);

				if (!visited.ContainsKey(nextHash))
				{
					visited.Add(nextHash, currentHash);
					queue.Enqueue(next);

					// --- GRAPH VISUALIZER HOOK ---
					// This is where you call your node-and-line code
					CreateVisualNode(nextHash, currentHash);
				}
			}
		}
	}
}
