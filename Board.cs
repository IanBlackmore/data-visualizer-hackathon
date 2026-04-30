using Godot;
using System;
using System.Collections.Generic;

public partial class Board : Control
{
	[Export] public PackedScene BlockTemplate;
	[Export] public int CellSize = 100;

	// Version 1 visual/art settings.
	[Export] public string BoardSpritePath = "res://Art/board.png";
	[Export] public int BoardFrameCount = 3;
	[Export] public float BoardAnimationFps = 12f;
	[Export] public Vector2 GridOffset = Vector2.Zero;
	[Export] public bool DrawDebugGrid = false;

	// Version 2/game settings.
	[Export] public string LevelPath = "res://Layouts/level1.json";
	[Export] public string ExportPath = "res://Layouts/exported_level.json";
	[Export] public bool AutoSolveOnReady = true;
	[Export] public bool AutoPlaySolution = true;
	[Export] public float SolutionStepDelay = 0.3f;
	private Godot.Collections.Array winStates = new Godot.Collections.Array();
	private Godot.Collections.Dictionary adjacency = new Godot.Collections.Dictionary();

	private const int sideLength = 6;

	private Vector2I _gridSize = new Vector2I(sideLength, sideLength);
	private List<KlotskiBlock> _blocks = new();
	private KlotskiBlock _selectedBlock = null;
	private bool _won = false;
	private bool _playingSolution = false;

	private AnimatedSprite2D _boardSprite;
	private Vector2 _boardFrameSize = Vector2.Zero;

	private readonly Color _boardColor       = new Color(0.14f, 0.11f, 0.09f);
	private readonly Color _gridLineColor    = new Color(1f,    1f,    1f,    0.12f);
	private readonly Color _borderColor      = new Color(0.50f, 0.38f, 0.26f);
	private readonly Color _heroColor        = new Color(0.80f, 0.25f, 0.22f);
	private readonly Color _neutralBlockColor = new Color(0.64f, 0.63f, 0.58f);

	private int _exitRow = 2;
	private int _exitHeightCells = 2;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(_gridSize.X * CellSize, _gridSize.Y * CellSize);
		LoadMatrixFromFile("res://Layouts/level2.json");
		QueueRedraw();

		GetNode("/root/AutoloadSignals").Connect("updateBoard", new Callable(this, MethodName.OnUpdateBoard));

		GD.Print("Board ready. Launching BFS Solver...");
		SolvePuzzle();
	}

	private void OnUpdateBoard(Godot.Collections.Array gdMatrix)
	{
		int rows = gdMatrix.Count;
		int[][] matrix = new int[rows][];
		for (int y = 0; y < rows; y++)
		{
			var row = gdMatrix[y].AsGodotArray();
			matrix[y] = new int[row.Count];
			for (int x = 0; x < row.Count; x++)
				matrix[y][x] = row[x].AsInt32();
		}
		LoadMatrixLayout(matrix);
	}

	// -------------------------------------------------------------------------
	// Solver
	// -------------------------------------------------------------------------

	public void SolvePuzzle()
	{
		var queue   = new Queue<byte[,]>();
		var visited = new Dictionary<string, string>();
		string firstWinHash = null;

		byte[,] startState = GetState2D();
		string startHash   = SerializeState(startState);

		queue.Enqueue(startState);
		visited.Add(startHash, null);

		GD.Print("Starting full state-space discovery...");

		while (queue.Count > 0)
		{
			byte[,] current     = queue.Dequeue();
			string currentHash  = SerializeState(current);

			if (IsWinState(current) && firstWinHash == null)
			{
				firstWinHash = currentHash;
				winStates.Add(currentHash);
				GD.Print("Shortest path found! Continuing to map remaining states...");
			}

			var neighbors = new Godot.Collections.Array();
			foreach (byte[,] next in GetNextStates(current))
			{
				string nextHash = serializeState(next);
				neighbors.Add(nextHash);
				if (!visited.ContainsKey(nextHash))
				{
					visited.Add(nextHash, currentHash);
					queue.Enqueue(next);
				}
			}
			adjacency[currentHash] = neighbors;
		}

		GD.Print($"Discovery complete. {visited.Count} states, {winStates.Count} win states.");

		var graphData = new Godot.Collections.Dictionary();
		graphData["adjacency"] = adjacency;
		graphData["win_states"] = winStates;
		graphData["start"] = startHash;
		GetTree().Root.SetMeta("klotski_graph", graphData);
		GetNode("/root/AutoloadSignals").EmitSignal("graph_ready");

		if (firstWinHash != null)
		{
			var path = ReconstructPath(visited, firstWinHash);
			PrintMoveSequence(path);

			if (AutoPlaySolution)
				PlaySolution(path);
		}
		else
		{
			GD.Print("No solution exists in this state space.");
		}
	}

	private List<string> ReconstructPath(Dictionary<string, string> visited, string winHash)
	{
		var path   = new List<string>();
		string cur = winHash;

		while (cur != null)
		{
			path.Add(cur);
			cur = visited[cur];
		}

		path.Reverse();
		string[] arr = path.ToArray();
		GetNode("/root/AutoloadSignals").EmitSignal("winning_path", arr);
		return path;
	}

	private void PrintMoveSequence(List<string> path)
	{
		GD.Print("\n--- MOVE SEQUENCE TO SOLUTION ---");

		for (int i = 1; i < path.Count; i++)
		{
			var prevBlocks = FindBlocksInGrid(DeserializeState(path[i - 1]));
			var currBlocks = FindBlocksInGrid(DeserializeState(path[i]));

			foreach (var currB in currBlocks)
			{
				var prevB = prevBlocks.Find(b => b.Type == currB.Type);
				if (prevB.Pos != currB.Pos)
				{
					string dir = GetDirectionName(currB.Pos - prevB.Pos);
					GD.Print($"Step {i}: Block '{(char)currB.Type}' moved {dir} to {currB.Pos}");
				}
			}
		}

		GD.Print("---------------------------------\n");
	}

	private string GetDirectionName(Vector2I dir)
	{
		if (dir == Vector2I.Up)    return "UP";
		if (dir == Vector2I.Down)  return "DOWN";
		if (dir == Vector2I.Left)  return "LEFT";
		if (dir == Vector2I.Right) return "RIGHT";
		return dir.ToString();
	}

	private async void PlaySolution(List<string> path)
	{
		if (_playingSolution) return;

		_playingSolution = true;
		GD.Print($"Playing back {path.Count - 1} moves...");

		for (int i = 1; i < path.Count; i++)
		{
			await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
			ApplyStateToBoard(path[i]);
			await ToSignal(GetTree().CreateTimer(SolutionStepDelay), "timeout");
		}

		_playingSolution = false;
		GD.Print("Playback finished!");
	}

	private void ApplyStateToBoard(string state)
	{
		foreach (var bData in FindBlocksInGrid(DeserializeState(state)))
		{
			var physicalBlock = _blocks.Find(b => b.ID == ((char)bData.Type).ToString());
			physicalBlock?.SlideTo(bData.Pos);
		}
	}

	// -------------------------------------------------------------------------
	// State serialization
	// -------------------------------------------------------------------------

	private byte[,] DeserializeState(string state)
	{
		byte[,] grid = new byte[sideLength, sideLength];
		int i = 0;

		for (int y = 0; y < 5; y++)
			for (int x = 0; x < 4; x++)
				grid[x, y] = (byte)state[i++];

		return grid;
	}

	public byte[,] GetState2D()
	{
		byte[,] grid = new byte[sideLength, sideLength];
		for (int y = 0; y < sideLength; y++)
			for (int x = 0; x < sideLength; x++)
				grid[x, y] = (byte)'.';

		foreach (var block in _blocks)
		{
			byte sym = (byte)block.ID[0];
			for (int x = 0; x < block.BlockSize.X; x++)
				for (int y = 0; y < block.BlockSize.Y; y++)
					grid[block.GridPos.X + x, block.GridPos.Y + y] = sym;
		}

		return grid;
	}

	public string SerializeState(byte[,] grid)
	{
		char[] flattenedState = new char[sideLength * sideLength];
		int ind = 0;
		for (int y = 0; y < sideLength; y++)
			for (int x = 0; x < sideLength; x++)
				flattenedState[ind++] = (char)grid[x, y];
		return new string(flattenedState);
	}

	private bool IsWinState(byte[,] grid)
	{
		byte heroId = (byte)'1'; 
		if(grid[5,2] == heroId) {
			return true;
		}
		return false;
	}

	// Lowercase alias kept for backward compatibility.
	public string serializeState(byte[,] grid) => SerializeState(grid);

	private bool IsWinState(byte[,] grid) => grid[3, 2] == (byte)'1';

	// -------------------------------------------------------------------------
	// BFS helpers
	// -------------------------------------------------------------------------

	public List<byte[,]> GetNextStates(byte[,] currentGrid)
	{
		var neighbors  = new List<byte[,]>();
		var directions = new[] { Vector2I.Right, Vector2I.Left, Vector2I.Down, Vector2I.Up };

		foreach (var b in FindBlocksInGrid(currentGrid))
			foreach (var dir in directions)
			{
				if (IsDirectionAllowed(b.Size, dir) && CanMoveBFS(currentGrid, b.Pos, b.Size, dir))
					neighbors.Add(ApplyMove(currentGrid, b.Pos, b.Size, dir));
			}
		return neighbors;
	}

	public bool CanMoveBFS(byte[,] grid, Vector2I currentPos, Vector2I size, Vector2I direction)
	{
		if (size.X > size.Y && direction.Y != 0) return false;
		if (size.Y > size.X && direction.X != 0) return false;

		Vector2I nextPos = currentPos + direction;

		for (int x = 0; x < size.X; x++)
		{
			for (int y = 0; y < size.Y; y++)
			{
				int tx = nextPos.X + x; int ty = nextPos.Y + y;
				if (tx < 0 || tx >= sideLength || ty < 0 || ty >= sideLength) return false;
				bool self = (tx >= currentPos.X && tx < currentPos.X + size.X && ty >= currentPos.Y && ty < currentPos.Y + size.Y);
				if (!self && grid[tx, ty] != (byte)'.') return false;
			}
		}

		return true;
	}

	private byte[,] ApplyMove(byte[,] grid, Vector2I pos, Vector2I size, Vector2I dir)
	{
		byte[,] next = (byte[,])grid.Clone();
		byte sym     = grid[pos.X, pos.Y];

		for (int x = 0; x < size.X; x++)
			for (int y = 0; y < size.Y; y++)
				next[pos.X + x, pos.Y + y] = (byte)'.';

		for (int x = 0; x < size.X; x++)
			for (int y = 0; y < size.Y; y++)
				next[pos.X + dir.X + x, pos.Y + dir.Y + y] = sym;

		return next;
	}

	public struct BlockData
	{
		public Vector2I Pos;
		public Vector2I Size;
		public byte Type;
	}

	private List<BlockData> FindBlocksInGrid(byte[,] grid)
	{
		List<BlockData> blocks = new();
		HashSet<byte> processedIds = new();
		for (int y = 0; y < sideLength; y++) 
			for (int x = 0; x < sideLength; x++) 
			{
				byte id = grid[x, y];
				if (id != (byte)'.' && !processedIds.Contains(id))
				{
					Vector2I sz = MeasureBlock(grid, x, y, id);
					blocks.Add(new BlockData { Pos = new Vector2I(x, y), Size = sz, Type = id });
					processedIds.Add(id);
				}
			}
		}

		return blocks;
	}

	private Vector2I MeasureBlock(byte[,] grid, int startX, int startY, byte id)
	{
		int w = 0; int h = 0;
		for (int x = startX; x < sideLength && grid[x, startY] == id; x++) w++;
		for (int y = startY; y < sideLength && grid[startX, y] == id; y++) h++;
		return new Vector2I(w, h);
	}

	// -------------------------------------------------------------------------
	// Player interaction
	// -------------------------------------------------------------------------

	// public bool CanMove(KlotskiBlock block, Vector2I direction)
	// {
	// 	if (block == null) return false;
	// 	if (block.BlockSize.X > block.BlockSize.Y && direction.Y != 0) return false;
	// 	if (block.BlockSize.Y > block.BlockSize.X && direction.X != 0) return false;

	// 	Vector2I newPos = block.GridPos + direction;

	// 	if (newPos.X < 0 || newPos.Y < 0 ||
	// 		newPos.X + block.BlockSize.X > _gridSize.X ||
	// 		newPos.Y + block.BlockSize.Y > _gridSize.Y)
	// 	{
	// 		return false;
	// 	}

	// 	foreach (var other in _blocks)
	// 	{
	// 		if (other == block) continue;

	// 		bool overlaps =
	// 			newPos.X < other.GridPos.X + other.BlockSize.X &&
	// 			newPos.X + block.BlockSize.X > other.GridPos.X &&
	// 			newPos.Y < other.GridPos.Y + other.BlockSize.Y &&
	// 			newPos.Y + block.BlockSize.Y > other.GridPos.Y;

	// 		if (overlaps) return false;
	// 	}

	// 	return true;
	// }

	private void CheckWin(KlotskiBlock block)
	{
		if (block.ID == "1" && block.GridPos.X + block.BlockSize.X >= _gridSize.X && block.GridPos.Y == _exitRow)
		{
			_won = true;
			block.PlayWinAnimation();
			_selectedBlock?.SetHighlight(false);
			_selectedBlock = null;
			GD.Print("You win!");
		}
	}

	public void SelectBlock(KlotskiBlock block)
	{
		if (_won || _playingSolution) return;

		_selectedBlock?.SetHighlight(false);
		_selectedBlock = block;
		_selectedBlock.SetHighlight(true);

		GD.Print($"Selected: {block.ID}");
	}

	public override void _Input(InputEvent @event)
	{
		if (_won || _playingSolution) return;

		if (@event.IsActionPressed("export_board") ||
			(@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Right && mb.Pressed))
		{
			ExportCurrentBoard();
			return;
		}

		if (@event.IsActionPressed("export_board")) { SaveMatrixToFile("res://Layouts/level1.json"); return; }
		if (_selectedBlock == null) return;

		Vector2I dir = Vector2I.Zero;
		if (@event.IsActionPressed("ui_up"))    dir = Vector2I.Up;
		if (@event.IsActionPressed("ui_down"))  dir = Vector2I.Down;
		if (@event.IsActionPressed("ui_left"))  dir = Vector2I.Left;
		if (@event.IsActionPressed("ui_right")) dir = Vector2I.Right;
		if (dir != Vector2I.Zero && IsDirectionAllowed(_selectedBlock.BlockSize, dir) && CanMoveBFS(GetState2D(), _selectedBlock.GridPos, _selectedBlock.BlockSize, dir))
		{
			_selectedBlock.SlideTo(_selectedBlock.GridPos + dir);
			CheckWin(_selectedBlock);
		}
	}

	private bool IsDirectionAllowed(Vector2I size, Vector2I dir)
	{
		if (size.X > size.Y) return dir == Vector2I.Left || dir == Vector2I.Right;
		if (size.Y > size.X) return dir == Vector2I.Up || dir == Vector2I.Down;
		return true; // square (2×2 hero) moves in all directions
	}

	
	private void CreateBlock(string id, Vector2I pos, Vector2I size, Color color)
	{
		if (BlockTemplate == null)
		{
			GD.PrintErr("BlockTemplate is not assigned on Board.");
			return;
		}

		var block = BlockTemplate.Instantiate<KlotskiBlock>();
		AddChild(block);
		block.ZIndex = 10;
		block.Board  = this;
		block.Setup(id, pos, size, CellSize, color, GridOffset);
		_blocks.Add(block);
	}

	private void ExportCurrentBoard()
	{
		GD.Print("Current board JSON:");
		GD.Print(ExportMatrixJson());
		SaveMatrixToFile(ExportPath);
		GD.Print($"Board exported to: {ExportPath}");
	}

	// -------------------------------------------------------------------------
	// Board sprite / drawing
	// -------------------------------------------------------------------------

	private void LoadBoardAnimation()
	{
		if (!ResourceLoader.Exists(BoardSpritePath))
		{
			GD.PrintErr($"Board sprite not found: {BoardSpritePath}. Using fallback drawn board.");
			_boardSprite = null;
			return;
		}

		_boardSprite?.QueueFree();
		_boardSprite = null;

		Texture2D tex = ResourceLoader.Load<Texture2D>(BoardSpritePath);
		if (tex == null)
		{
			GD.PrintErr($"Could not load board sprite: {BoardSpritePath}");
			return;
		}

		int safeFrameCount = Math.Max(1, BoardFrameCount);
		int frameWidth     = tex.GetWidth() / safeFrameCount;
		int frameHeight    = tex.GetHeight();

		_boardFrameSize = new Vector2(frameWidth, frameHeight);

		var frames = new SpriteFrames();
		if (!frames.HasAnimation("default"))
			frames.AddAnimation("default");

		for (int i = 0; i < safeFrameCount; i++)
		{
			var frame = new AtlasTexture
			{
				Atlas  = tex,
				Region = new Rect2(i * frameWidth, 0, frameWidth, frameHeight)
			};
			frames.AddFrame("default", frame);
		}

		frames.SetAnimationSpeed("default", BoardAnimationFps);
		frames.SetAnimationLoop("default", true);

		_boardSprite = new AnimatedSprite2D
		{
			SpriteFrames = frames,
			Animation    = "default",
			Position     = new Vector2(frameWidth / 2f, frameHeight / 2f),
			ZIndex       = -100
		};

		AddChild(_boardSprite);
		MoveChild(_boardSprite, 0);
		_boardSprite.Play("default");
	}

	private void UpdateBoardMinimumSize()
	{
		Vector2 gridPixelSize = GridOffset + new Vector2(_gridSize.X * CellSize, _gridSize.Y * CellSize);
		Vector2 finalSize = new Vector2(
			Mathf.Max(gridPixelSize.X, _boardFrameSize.X),
			Mathf.Max(gridPixelSize.Y, _boardFrameSize.Y)
		);

		CustomMinimumSize = finalSize;
		Size = finalSize;
	}

	public override void _Draw()
	{
		float boardW = _gridSize.X * CellSize;
		float boardH = _gridSize.Y * CellSize;

		if (_boardSprite == null)
		{
			DrawRect(new Rect2(GridOffset, new Vector2(boardW, boardH)), _boardColor);
			DrawBoardOutline(boardW, boardH);
		}

		if (DrawDebugGrid)
			DrawGrid(boardW, boardH);
	}

	private void DrawGrid(float boardW, float boardH)
	{
		for (int c = 1; c < _gridSize.X; c++)
		{
			float x = GridOffset.X + c * CellSize;
			DrawLine(new Vector2(x, GridOffset.Y), new Vector2(x, GridOffset.Y + boardH), _gridLineColor, 1f);
		}

		for (int r = 1; r < _gridSize.Y; r++)
		{
			float y = GridOffset.Y + r * CellSize;
			DrawLine(new Vector2(GridOffset.X, y), new Vector2(GridOffset.X + boardW, y), _gridLineColor, 1f);
		}
	}

	private void DrawBoardOutline(float boardW, float boardH)
	{
		// boardW and boardH should now be (6 * CellSize)
		float thick = 4f; 
		float exitY = _exitRow * CellSize; 
		float exitH = _exitHeightCells * CellSize;

		// Top Border
		DrawLine(Vector2.Zero, new Vector2(boardW, 0), _borderColor, thick);
		// Bottom Border
		DrawLine(new Vector2(0, boardH), new Vector2(boardW, boardH), _borderColor, thick);
		// Left Border
		DrawLine(Vector2.Zero, new Vector2(0, boardH), _borderColor, thick);
		
		// Right Border (with Exit Gap)
		// Draw from top to the start of the exit
		DrawLine(new Vector2(boardW, 0), new Vector2(boardW, exitY), _borderColor, thick);
		// Draw from the end of the exit to the bottom
		DrawLine(new Vector2(boardW, exitY + exitH), new Vector2(boardW, boardH), _borderColor, thick);
	}

	// -------------------------------------------------------------------------
	// Level loading / exporting
	// -------------------------------------------------------------------------

	public void LoadMatrixFromFile(string path)
	{
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"Could not open: {path}");
			return;
		}

		var text = file.GetAsText();
		file.Close();

		var json = new Json();
		if (json.Parse(text) != Error.Ok)
		{
			GD.PrintErr("JSON parse error: ", json.GetErrorMessage());
			return;
		}

		var dict = json.Data.AsGodotDictionary();
		if (!dict.ContainsKey("grid"))
		{
			GD.PrintErr("Missing 'grid' field.");
			return;
		}

		var gridList = dict["grid"].AsGodotArray();
		int[][] matrix = new int[gridList.Count][];

		for (int y = 0; y < gridList.Count; y++)
		{
			var row = gridList[y].AsGodotArray();
			matrix[y] = new int[row.Count];
			for (int x = 0; x < row.Count; x++)
				matrix[y][x] = (int)row[x];
		}

		LoadMatrixLayout(matrix);
	}

	public void LoadMatrixLayout(int[][] matrix)
	{
		if (matrix == null || matrix.Length == 0 || matrix[0].Length == 0)
		{
			GD.PrintErr("Cannot load empty matrix.");
			return;
		}

		_won = false;
		_playingSolution = false;
		_selectedBlock?.SetHighlight(false);
		_selectedBlock = null;

		foreach (var b in _blocks)
			if (IsInstanceValid(b))
				b.QueueFree();

		_blocks.Clear();

		_gridSize = new Vector2I(matrix[0].Length, matrix.Length);
		UpdateBoardMinimumSize();

		bool[,] visited = new bool[_gridSize.X, _gridSize.Y];

		for (int y = 0; y < _gridSize.Y; y++)
		{
			for (int x = 0; x < _gridSize.X; x++)
			{
				int id = matrix[y][x];
				if (id == 0 || visited[x, y]) continue;

				var block = ExtractBlockFromMatrix(matrix, id, x, y, visited);
				CreateBlock(block.id, block.pos, block.size,
							block.id == "1" ? _heroColor : _neutralBlockColor);
			}
		}

		QueueRedraw();
	}

	private (string id, Vector2I pos, Vector2I size) ExtractBlockFromMatrix(
		int[][] matrix, int id, int startX, int startY, bool[,] visited)
	{
		int w = matrix[0].Length;
		int h = matrix.Length;
		int minX = startX, maxX = startX;
		int minY = startY, maxY = startY;

		var q = new Queue<Vector2I>();
		q.Enqueue(new Vector2I(startX, startY));
		visited[startX, startY] = true;

		while (q.Count > 0)
		{
			var p = q.Dequeue();
			minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
			minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);

			foreach (var dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
			{
				int nx = p.X + dir.X;
				int ny = p.Y + dir.Y;

				if (nx < 0 || ny < 0 || nx >= w || ny >= h || visited[nx, ny] || matrix[ny][nx] != id)
					continue;

				visited[nx, ny] = true;
				q.Enqueue(new Vector2I(nx, ny));
			}
		}

		return (id.ToString(), new Vector2I(minX, minY), new Vector2I(maxX - minX + 1, maxY - minY + 1));
	}


	public int[][] ExportMatrixLayout()
	{
		int[][] matrix = new int[_gridSize.Y][];
		for (int y = 0; y < _gridSize.Y; y++)
			matrix[y] = new int[_gridSize.X];

		foreach (var block in _blocks)
		{
			int id = int.Parse(block.ID);
			for (int dy = 0; dy < block.BlockSize.Y; dy++)
				for (int dx = 0; dx < block.BlockSize.X; dx++)
					matrix[block.GridPos.Y + dy][block.GridPos.X + dx] = id;
		}

		return matrix;
	}


	public void SaveMatrixToFile(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PrintErr($"Could not write to: {path}");
			return;
		}

		file.StoreString(ExportMatrixJson());
	}

	public string ExportMatrixJson()
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
		return Json.Stringify(dict, "\t");
	}

}