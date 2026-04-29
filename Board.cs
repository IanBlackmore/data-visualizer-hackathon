using Godot;
using System;
using System.Collections.Generic;

public partial class Board : Control
{
	[Export] public PackedScene BlockTemplate;
	[Export] public int CellSize = 100;

	private Vector2I _gridSize = new Vector2I(4, 5);
	private List<KlotskiBlock> _blocks = new();
	private KlotskiBlock _selectedBlock = null;

	// Visual style
	private readonly Color _boardColor = new Color(0.14f, 0.11f, 0.09f);
	private readonly Color _gridLineColor = new Color(1f, 1f, 1f, 0.07f);
	private readonly Color _borderColor = new Color(0.50f, 0.38f, 0.26f);

	private readonly Color _heroColor = new Color(0.80f, 0.25f, 0.22f);
	private readonly Color _neutralBlockColor = new Color(0.64f, 0.63f, 0.58f);

	private int _exitRow = 2;
	private int _exitHeightCells = 1;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(_gridSize.X * CellSize, _gridSize.Y * CellSize);

		// Load the level
		LoadMatrixFromFile("res://Layouts/level1.json");
		QueueRedraw();
		
		GD.Print("Board ready. Launching BFS Solver...");
		SolvePuzzle();
	}

	private void CreateBlock(string id, Vector2I pos, Vector2I size, Color color)
	{
		var block = BlockTemplate.Instantiate<KlotskiBlock>();
		AddChild(block);
		block.Board = this;
		block.Setup(id, pos, size, CellSize, color);
		_blocks.Add(block);
	}

	public void SelectBlock(KlotskiBlock block)
	{
		if (_selectedBlock != null)
			_selectedBlock.SetHighlight(false);

		_selectedBlock = block;
		_selectedBlock.SetHighlight(true);
		GD.Print($"Selected: {block.ID}");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("export_board"))
		{
			SaveMatrixToFile("res://Layouts/exported_level.json");
			GD.Print("Board exported!");
			return; 
		}
		
		if (_selectedBlock == null) return;

		Vector2I dir = Vector2I.Zero;
		if (@event.IsActionPressed("ui_up")) dir = Vector2I.Up;
		if (@event.IsActionPressed("ui_down")) dir = Vector2I.Down;
		if (@event.IsActionPressed("ui_left")) dir = Vector2I.Left;
		if (@event.IsActionPressed("ui_right")) dir = Vector2I.Right;

		if (dir != Vector2I.Zero)
		{
			// FIXED: Pass all 4 required arguments to CanMove
			if (CanMove(GetState2D(), _selectedBlock.GridPos, _selectedBlock.BlockSize, dir))
			{
				_selectedBlock.SlideTo(_selectedBlock.GridPos + dir);
			}
		}
	}

	// --- CORE LOGIC FOR BFS & MOVEMENT ---

	public bool CanMove(byte[,] grid, Vector2I currentPos, Vector2I size, Vector2I direction)
	{
		Vector2I nextPos = currentPos + direction;

		for (int x = 0; x < size.X; x++)
		{
			for (int y = 0; y < size.Y; y++)
			{
				int targetX = nextPos.X + x;
				int targetY = nextPos.Y + y;

				// Boundary check
				if (targetX < 0 || targetX >= 4 || targetY < 0 || targetY >= 5) 
					return false;

				// Collision check
				bool isInsideOwnSelf = (targetX >= currentPos.X && targetX < currentPos.X + size.X &&
										targetY >= currentPos.Y && targetY < currentPos.Y + size.Y);
				
				if (!isInsideOwnSelf && grid[targetX, targetY] != (byte)'.')
					return false;
			}
		}
		return true;
	}

	public byte[,] GetState2D()
	{
		byte[,] grid = new byte[4, 5];
		for (int y = 0; y < 5; y++)
			for (int x = 0; x < 4; x++)
				grid[x, y] = (byte)'.';

		foreach (var block in _blocks)
		{
			byte symbol = GetSymbolForBlock(block);
			for (int x = 0; x < block.BlockSize.X; x++)
				for (int y = 0; y < block.BlockSize.Y; y++)
					grid[block.GridPos.X + x, block.GridPos.Y + y] = symbol;
		}
		return grid;
	}
	
	public string serializeState(byte[,] grid)
	{
		char[] flattenedState = new char[20];
		int ind = 0;
		for (int y = 0; y < 5; y++)
		{
			for (int x = 0; x < 4; x++)
			{
				flattenedState[ind++] = (char)grid[x, y];
			}
		}
		return new string(flattenedState);
	}
	
	private bool IsWinState(byte[,] grid)
	{
		return grid[1, 3] == (byte)'H' && grid[2, 3] == (byte)'H' && 
			   grid[1, 4] == (byte)'H' && grid[2, 4] == (byte)'H';
	}
	
	private byte GetSymbolForBlock(KlotskiBlock block)
	{
		if (block.BlockSize.X == 2 && block.BlockSize.Y == 2) return (byte)'H';
		if (block.BlockSize.X == 1 && block.BlockSize.Y == 2) return (byte)'V';
		if (block.BlockSize.X == 2 && block.BlockSize.Y == 1) return (byte)'W';
		return (byte)'S'; 
	}

	public List<byte[,]> GetNextStates(byte[,] currentGrid)
	{
		List<byte[,]> neighbors = new();
		Vector2I[] directions = { Vector2I.Right, Vector2I.Left, Vector2I.Down, Vector2I.Up };

		var blocks = FindBlocksInGrid(currentGrid);
		foreach (var b in blocks)
		{
			foreach (var dir in directions)
			{
				// Ensure all parameters are passed here as well
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

		for (int x = 0; x < size.X; x++)
			for (int y = 0; y < size.Y; y++)
				nextGrid[pos.X + x, pos.Y + y] = (byte)'.';

		for (int x = 0; x < size.X; x++)
			for (int y = 0; y < size.Y; y++)
				nextGrid[pos.X + dir.X + x, pos.Y + dir.Y + y] = symbol;

		return nextGrid;
	}
	
	public void SolvePuzzle()
	{
		Queue<byte[,]> queue = new();
		Dictionary<string, string> visited = new();

		byte[,] startState = GetState2D();
		string startHash = serializeState(startState);
		
		queue.Enqueue(startState);
		visited.Add(startHash, null);

		int safetyCounter = 0;
		while (queue.Count > 0)
		{
			safetyCounter++;
			byte[,] current = queue.Dequeue();
			string currentHash = serializeState(current);

			if (IsWinState(current))
			{
				GD.Print($"Solution Found in {safetyCounter} iterations!");
				return; 
			}

			foreach (byte[,] next in GetNextStates(current))
			{
				string nextHash = serializeState(next);
				if (!visited.ContainsKey(nextHash))
				{
					visited.Add(nextHash, currentHash);
					queue.Enqueue(next);
				}
			}
		}
		GD.Print("No solution possible.");
	}

	// --- GRID UTILITIES ---

	public struct BlockData {
		public Vector2I Pos;
		public Vector2I Size;
		public byte Type;
	}

	private List<BlockData> FindBlocksInGrid(byte[,] grid) {
		List<BlockData> blocks = new();
		bool[,] visitedCells = new bool[4, 5];

		for (int y = 0; y < 5; y++) {
			for (int x = 0; x < 4; x++) {
				if (grid[x, y] != (byte)'.' && !visitedCells[x, y]) {
					byte type = grid[x, y];
					Vector2I size = GetSizeFromType(type);
					blocks.Add(new BlockData { Pos = new Vector2I(x, y), Size = size, Type = type });

					for (int sy = 0; sy < size.Y; sy++)
						for (int sx = 0; sx < size.X; sx++)
							visitedCells[x + sx, y + sy] = true;
				}
			}
		}
		return blocks;
	}

	private Vector2I GetSizeFromType(byte type) {
		return (char)type switch {
			'H' => new Vector2I(2, 2),
			'V' => new Vector2I(1, 2),
			'W' => new Vector2I(2, 1),
			_ => new Vector2I(1, 1)
		};
	}

	// --- DRAWING CODE ---

	public override void _Draw()
	{
		Vector2 boardSize = new Vector2(_gridSize.X * CellSize, _gridSize.Y * CellSize);
		DrawRect(new Rect2(Vector2.Zero, boardSize), _boardColor);

		for (int c = 1; c < _gridSize.X; c++)
			DrawLine(new Vector2(c * CellSize, 0), new Vector2(c * CellSize, boardSize.Y), _gridLineColor, 1f);
		for (int r = 1; r < _gridSize.Y; r++)
			DrawLine(new Vector2(0, r * CellSize), new Vector2(boardSize.X, r * CellSize), _gridLineColor, 1f);

		DrawBoardOutline(boardSize.X, boardSize.Y);
	}

	private void DrawBoardOutline(float boardW, float boardH)
	{
		float thick = 4f;
		float exitY = _exitRow * CellSize;
		float exitH = _exitHeightCells * CellSize;

		DrawLine(Vector2.Zero, new Vector2(boardW, 0), _borderColor, thick);
		DrawLine(new Vector2(0, boardH), new Vector2(boardW, boardH), _borderColor, thick);
		DrawLine(Vector2.Zero, new Vector2(0, boardH), _borderColor, thick);
		DrawLine(new Vector2(boardW, 0), new Vector2(boardW, exitY), _borderColor, thick);
		DrawLine(new Vector2(boardW, exitY + exitH), new Vector2(boardW, boardH), _borderColor, thick);
	}

	// --- JSON SYSTEM ---

	public void LoadMatrixFromFile(string path)
	{
		if (!FileAccess.FileExists(path)) return;
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		if (json.Parse(file.GetAsText()) != Error.Ok) return;

		var gridList = json.Data.AsGodotDictionary()["grid"].AsGodotArray();
		int[][] matrix = new int[gridList.Count][];
		for (int y = 0; y < gridList.Count; y++)
		{
			var row = gridList[y].AsGodotArray();
			matrix[y] = new int[row.Count];
			for (int x = 0; x < row.Count; x++) matrix[y][x] = (int)row[x];
		}
		LoadMatrixLayout(matrix);
	}

	public void LoadMatrixLayout(int[][] matrix)
	{
		if (IsInstanceValid(_selectedBlock)) _selectedBlock.SetHighlight(false);
		_selectedBlock = null;

		foreach (var b in _blocks) if (IsInstanceValid(b)) b.QueueFree();
		_blocks.Clear();

		_gridSize = new Vector2I(matrix[0].Length, matrix.Length);
		bool[,] visited = new bool[_gridSize.X, _gridSize.Y];

		for (int y = 0; y < _gridSize.Y; y++)
		{
			for (int x = 0; x < _gridSize.X; x++)
			{
				int id = matrix[y][x];
				if (id == 0 || visited[x, y]) continue;

				var bInfo = ExtractBlockFromMatrix(matrix, id, x, y, visited);
				Color color = (bInfo.id == "1") ? _heroColor : _neutralBlockColor;
				CreateBlock(bInfo.id, bInfo.pos, bInfo.size, color);
			}
		}
		QueueRedraw();
	}

	private (string id, Vector2I pos, Vector2I size) ExtractBlockFromMatrix(int[][] matrix, int id, int startX, int startY, bool[,] visited)
	{
		int minX = startX, maxX = startX, minY = startY, maxY = startY;
		Queue<Vector2I> q = new();
		q.Enqueue(new Vector2I(startX, startY));
		visited[startX, startY] = true;

		while (q.Count > 0)
		{
			var p = q.Dequeue();
			minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
			minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);

			foreach (var dir in new Vector2I[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
			{
				int nx = p.X + dir.X, ny = p.Y + dir.Y;
				if (nx >= 0 && ny >= 0 && nx < matrix[0].Length && ny < matrix.Length && !visited[nx, ny] && matrix[ny][nx] == id)
				{
					visited[nx, ny] = true;
					q.Enqueue(new Vector2I(nx, ny));
				}
			}
		}
		return (id.ToString(), new Vector2I(minX, minY), new Vector2I(maxX - minX + 1, maxY - minY + 1));
	}

	public void SaveMatrixToFile(string path)
	{
		int[][] matrix = new int[_gridSize.Y][];
		for (int y = 0; y < _gridSize.Y; y++) matrix[y] = new int[_gridSize.X];
		foreach (var b in _blocks)
		{
			int id = int.Parse(b.ID);
			for (int dy = 0; dy < b.BlockSize.Y; dy++)
				for (int dx = 0; dx < b.BlockSize.X; dx++)
					matrix[b.GridPos.Y + dy][b.GridPos.X + dx] = id;
		}

		var outer = new Godot.Collections.Array();
		foreach (var rowArr in matrix)
		{
			var row = new Godot.Collections.Array();
			foreach (int val in rowArr) row.Add(val);
			outer.Add(row);
		}

		var dict = new Godot.Collections.Dictionary<string, Variant> { ["grid"] = outer };
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		file.StoreString(Json.Stringify(dict, "\t"));
	}
}
