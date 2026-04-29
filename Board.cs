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
		LoadMatrixFromFile("res://Layouts/level1.json");
		QueueRedraw();
		
		GD.Print("Board ready. Launching BFS Solver...");
		SolvePuzzle();
	}

	// --- CORE SOLVER LOGIC ---

	public void SolvePuzzle()
	{
		Queue<byte[,]> queue = new();
		Dictionary<string, string> visited = new();
		string firstWinHash = null; // Defined here so it's in scope for the whole method

		byte[,] startState = GetState2D();
		string startHash = serializeState(startState);
		
		queue.Enqueue(startState);
		visited.Add(startHash, null);

		GD.Print("Starting full state space discovery...");

		while (queue.Count > 0)
		{
			byte[,] current = queue.Dequeue();
			string currentHash = serializeState(current);

			if (IsWinState(current) && firstWinHash == null)
			{
				firstWinHash = currentHash;
				GD.Print("Shortest path found! Continuing to map remaining states...");
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

		// Reconstruct and Play if a solution was found
		if (firstWinHash != null)
		{
			GD.Print($"Discovery complete. Total states in graph: {visited.Count}");
			var path = ReconstructPath(visited, firstWinHash);
			PrintMoveSequence(path);
			PlaySolution(path);
		}
		else
		{
			GD.Print("No solution exists in this state space.");
		}
	}

	private List<string> ReconstructPath(Dictionary<string, string> visited, string winHash)
	{
		List<string> path = new();
		string current = winHash;
		while (current != null)
		{
			path.Add(current);
			current = visited[current];
		}
		path.Reverse();
		return path;
	}
	
	private void PrintMoveSequence(List<string> path)
	{
		GD.Print("\n--- MOVE SEQUENCE TO SOLUTION ---");
		
		for (int i = 1; i < path.Count; i++)
		{
			byte[,] prevState = DeserializeState(path[i - 1]);
			byte[,] currState = DeserializeState(path[i]);
			
			var prevBlocks = FindBlocksInGrid(prevState);
			var currBlocks = FindBlocksInGrid(currState);

			foreach (var currB in currBlocks)
			{
				// Find the same block in the previous state to see if it moved
				var prevB = prevBlocks.Find(b => b.Type == currB.Type);
				if (prevB.Pos != currB.Pos)
				{
					Vector2I diff = currB.Pos - prevB.Pos;
					string direction = GetDirectionName(diff);
					char blockId = (char)currB.Type;
					
					GD.Print($"Step {i}: Block '{blockId}' moved {direction} to {currB.Pos}");
				}
			}
		}
		GD.Print("---------------------------------\n");
	}

	private string GetDirectionName(Vector2I dir)
	{
		if (dir == Vector2I.Up) return "UP";
		if (dir == Vector2I.Down) return "DOWN";
		if (dir == Vector2I.Left) return "LEFT";
		if (dir == Vector2I.Right) return "RIGHT";
		return dir.ToString();
	}

	private async void PlaySolution(List<string> path)
	{
		GD.Print($"Playing back {path.Count - 1} moves...");
		for (int i = 1; i < path.Count; i++)
		{
			ApplyStateToBoard(path[i]);
			await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
		}
		GD.Print("Playback finished!");
	}

	private void ApplyStateToBoard(string state)
	{
		// Use the newly added DeserializeState function
		var blocksInState = FindBlocksInGrid(DeserializeState(state));
		foreach (var bData in blocksInState)
		{
			var physicalBlock = _blocks.Find(b => b.ID == ((char)bData.Type).ToString());
			if (physicalBlock != null)
			{
				physicalBlock.SlideTo(bData.Pos);
			}
		}
	}

	// --- STATE UTILITIES ---

	private byte[,] DeserializeState(string state)
	{
		byte[,] grid = new byte[4, 5];
		int i = 0;
		for (int y = 0; y < 5; y++)
		{
			for (int x = 0; x < 4; x++)
			{
				grid[x, y] = (byte)state[i++];
			}
		}
		return grid;
	}

	public byte[,] GetState2D()
	{
		byte[,] grid = new byte[4, 5];
		for (int y = 0; y < 5; y++)
			for (int x = 0; x < 4; x++)
				grid[x, y] = (byte)'.';

		foreach (var block in _blocks)
		{
			byte idSymbol = (byte)block.ID[0]; 
			for (int x = 0; x < block.BlockSize.X; x++)
				for (int y = 0; y < block.BlockSize.Y; y++)
					grid[block.GridPos.X + x, block.GridPos.Y + y] = idSymbol;
		}
		return grid;
	}

	public string serializeState(byte[,] grid)
	{
		char[] flattenedState = new char[20];
		int ind = 0;
		for (int y = 0; y < 5; y++)
			for (int x = 0; x < 4; x++)
				flattenedState[ind++] = (char)grid[x, y];
		return new string(flattenedState);
	}

	private bool IsWinState(byte[,] grid)
	{
		byte heroId = (byte)'1'; 
		if(grid[3,2] == heroId) {
			return true;
		}
		return false;
	}

	// --- MOVEMENT LOGIC ---

	public List<byte[,]> GetNextStates(byte[,] currentGrid)
	{
		List<byte[,]> neighbors = new();
		Vector2I[] directions = { Vector2I.Right, Vector2I.Left, Vector2I.Down, Vector2I.Up };
		var blocks = FindBlocksInGrid(currentGrid);
		foreach (var b in blocks)
		{
			foreach (var dir in directions)
			{
				if (CanMove(currentGrid, b.Pos, b.Size, dir))
					neighbors.Add(ApplyMove(currentGrid, b.Pos, b.Size, dir));
			}
		}
		return neighbors;
	}

	public bool CanMove(byte[,] grid, Vector2I currentPos, Vector2I size, Vector2I direction)
	{
		Vector2I nextPos = currentPos + direction;
		for (int x = 0; x < size.X; x++)
		{
			for (int y = 0; y < size.Y; y++)
			{
				int tx = nextPos.X + x; int ty = nextPos.Y + y;
				if (tx < 0 || tx >= 4 || ty < 0 || ty >= 5) return false;
				bool self = (tx >= currentPos.X && tx < currentPos.X + size.X && ty >= currentPos.Y && ty < currentPos.Y + size.Y);
				if (!self && grid[tx, ty] != (byte)'.') return false;
			}
		}
		return true;
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

	// --- GRID UTILITIES ---

	public struct BlockData { public Vector2I Pos; public Vector2I Size; public byte Type; }

	private List<BlockData> FindBlocksInGrid(byte[,] grid) 
	{
		List<BlockData> blocks = new();
		HashSet<byte> processedIds = new();
		for (int y = 0; y < 5; y++) 
			for (int x = 0; x < 4; x++) 
			{
				byte id = grid[x, y];
				if (id != (byte)'.' && !processedIds.Contains(id)) 
				{
					Vector2I size = MeasureBlock(grid, x, y, id);
					blocks.Add(new BlockData { Pos = new Vector2I(x, y), Size = size, Type = id });
					processedIds.Add(id);
				}
			}
		return blocks;
	}

	private Vector2I MeasureBlock(byte[,] grid, int startX, int startY, byte id)
	{
		int w = 0; int h = 0;
		for (int x = startX; x < 4 && grid[x, startY] == id; x++) w++;
		for (int y = startY; y < 5 && grid[startX, y] == id; y++) h++;
		return new Vector2I(w, h);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("export_board")) { SaveMatrixToFile("res://Layouts/exported_level.json"); return; }
		if (_selectedBlock == null) return;
		Vector2I dir = Vector2I.Zero;
		if (@event.IsActionPressed("ui_up")) dir = Vector2I.Up;
		if (@event.IsActionPressed("ui_down")) dir = Vector2I.Down;
		if (@event.IsActionPressed("ui_left")) dir = Vector2I.Left;
		if (@event.IsActionPressed("ui_right")) dir = Vector2I.Right;
		if (dir != Vector2I.Zero && CanMove(GetState2D(), _selectedBlock.GridPos, _selectedBlock.BlockSize, dir))
			_selectedBlock.SlideTo(_selectedBlock.GridPos + dir);
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
		if (_selectedBlock != null) _selectedBlock.SetHighlight(false);
		_selectedBlock = block;
		_selectedBlock.SetHighlight(true);
	}

	public override void _Draw()
	{
		Vector2 boardSize = new Vector2(_gridSize.X * CellSize, _gridSize.Y * CellSize);
		DrawRect(new Rect2(Vector2.Zero, boardSize), _boardColor);
		for (int c = 1; c < _gridSize.X; c++) DrawLine(new Vector2(c * CellSize, 0), new Vector2(c * CellSize, boardSize.Y), _gridLineColor, 1f);
		for (int r = 1; r < _gridSize.Y; r++) DrawLine(new Vector2(0, r * CellSize), new Vector2(boardSize.X, r * CellSize), _gridLineColor, 1f);
		DrawBoardOutline(boardSize.X, boardSize.Y);
	}

	private void DrawBoardOutline(float boardW, float boardH)
	{
		float thick = 4f; float exitY = _exitRow * CellSize; float exitH = _exitHeightCells * CellSize;
		DrawLine(Vector2.Zero, new Vector2(boardW, 0), _borderColor, thick);
		DrawLine(new Vector2(0, boardH), new Vector2(boardW, boardH), _borderColor, thick);
		DrawLine(Vector2.Zero, new Vector2(0, boardH), _borderColor, thick);
		DrawLine(new Vector2(boardW, 0), new Vector2(boardW, exitY), _borderColor, thick);
		DrawLine(new Vector2(boardW, exitY + exitH), new Vector2(boardW, boardH), _borderColor, thick);
	}

	public void LoadMatrixFromFile(string path)
	{
		if (!FileAccess.FileExists(path)) return;
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		if (json.Parse(file.GetAsText()) != Error.Ok) return;
		var gridList = json.Data.AsGodotDictionary()["grid"].AsGodotArray();
		int[][] matrix = new int[gridList.Count][];
		for (int y = 0; y < gridList.Count; y++) {
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
			for (int x = 0; x < _gridSize.X; x++)
			{
				int id = matrix[y][x];
				if (id == 0 || visited[x, y]) continue;
				var bInfo = ExtractBlockFromMatrix(matrix, id, x, y, visited);
				Color color = (bInfo.id == "1") ? _heroColor : _neutralBlockColor;
				CreateBlock(bInfo.id, bInfo.pos, bInfo.size, color);
			}
		QueueRedraw();
	}

	private (string id, Vector2I pos, Vector2I size) ExtractBlockFromMatrix(int[][] matrix, int id, int startX, int startY, bool[,] visited)
	{
		int minX = startX, maxX = startX, minY = startY, maxY = startY;
		Queue<Vector2I> q = new(); q.Enqueue(new Vector2I(startX, startY)); visited[startX, startY] = true;
		while (q.Count > 0)
		{
			var p = q.Dequeue();
			minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X); minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
			foreach (var dir in new Vector2I[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
			{
				int nx = p.X + dir.X, ny = p.Y + dir.Y;
				if (nx >= 0 && ny >= 0 && nx < matrix[0].Length && ny < matrix.Length && !visited[nx, ny] && matrix[ny][nx] == id)
				{ visited[nx, ny] = true; q.Enqueue(new Vector2I(nx, ny)); }
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
		foreach (var rowArr in matrix) {
			var row = new Godot.Collections.Array();
			foreach (int val in rowArr) row.Add(val);
			outer.Add(row);
		}
		var dict = new Godot.Collections.Dictionary<string, Variant> { ["grid"] = outer };
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		file.StoreString(Json.Stringify(dict, "\t"));
	}
}
