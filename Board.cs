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
			for(int j = 0; j < 5; j++){
			 	flattenedState[ind++] = (char)grid[i,j];
			}
		}
		return new string(flattenedState);
	}
	
	private bool IsWinState(byte[,] grid)
	{
		// Common Klotski win: 2x2 Hero block at bottom middle
		return grid[1, 3] == (byte)'H' && grid[1, 4] == (byte)'H' && 
			   grid[2, 3] == (byte)'H' && grid[2, 4] == (byte)'H';
	}
	
	private byte GetSymbolForBlock(KlotskiBlock block)
	{
		if (block.BlockSize.X == 2 && block.BlockSize.Y == 2) return (byte)'H';
		if (block.BlockSize.X == 1 && block.BlockSize.Y == 2) return (byte)'V';
		if (block.BlockSize.X == 2 && block.BlockSize.Y == 1) return (byte)'W';
		return (byte)'S'; // Small 1x1
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
		string startHash = serializeState(startState);
		
		queue.Enqueue(startState);
		visited.Add(startHash, null); // Null means it's the root/starting node

		// 3. THE BFS LOOP
		while (queue.Count > 0)
		{
			byte[,] current = queue.Dequeue();
			string currentHash = serializeState(current);

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
				string nextHash = serializeState(next);

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
	
	private void DisplaySolution(Dictionary<string, string> visited, string finalHash)
	{
		GD.Print("Found a path! Backtracking...");
		// Logic to highlight the path in your graph goes here later
	}

	private void CreateVisualNode(string nextHash, string currentHash)
	{
		// Logic to spawn your GraphNode and draw a line goes here
		// For now, just a print statement to verify it's working:
		// GD.Print($"New Node: {nextHash} from {currentHash}");
	}
	
	// Helper class to store temporary block data for the solver
	public struct BlockData {
		public Vector2I Pos;
		public Vector2I Size;
		public byte Type;
	}

	private List<BlockData> FindBlocksInGrid(byte[,] grid) {
		List<BlockData> blocks = new List<BlockData>();
		bool[,] visited = new bool[4, 5];

		for (int y = 0; y < 5; y++) {
			for (int x = 0; x < 4; x++) {
				if (grid[x, y] != (byte)'.' && !visited[x, y]) {
					byte type = grid[x, y];
					Vector2I size = GetSizeFromType(type, grid, x, y);
					blocks.Add(new BlockData { Pos = new Vector2I(x, y), Size = size, Type = type });

					// Mark all cells of this block as visited
					for (int sy = 0; sy < size.Y; sy++)
						for (int sx = 0; sx < size.X; sx++)
							visited[x + sx, y + sy] = true;
				}
			}
		}
		return blocks;
	}

	private Vector2I GetSizeFromType(byte type, byte[,] grid, int x, int y) {
		if (type == (byte)'H') return new Vector2I(2, 2);
		if (type == (byte)'V') return new Vector2I(1, 2);
		if (type == (byte)'W') return new Vector2I(2, 1);
		return new Vector2I(1, 1); // 'S'
	}
}
