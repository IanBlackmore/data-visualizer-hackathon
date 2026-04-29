using Godot;
using System;
using System.Collections.Generic;

public partial class Board : Control
{
	[Export] public PackedScene BlockTemplate;
	[Export] public int CellSize = 100;

	// Animated board spritesheet settings
	[Export] public string BoardSpritePath = "res://Art/board.png";
	[Export] public int BoardFrameCount = 3;
	[Export] public float BoardAnimationFps = 12f;

	// No offset. Blocks start at top-left of board image.
	[Export] public Vector2 GridOffset = Vector2.Zero;

	[Export] public string LevelPath = "res://Layouts/level2.json";
	[Export] public string ExportPath = "res://Layouts/exported_level.json";

	// Turn this on only if you want grid lines for debugging
	[Export] public bool DrawDebugGrid = false;

	// Bottom button settings
	[Export] public int BottomButtonHeight = 56;
	[Export] public int BottomButtonMargin = 24;
	[Export] public int BottomButtonSpacing = 28;

	private Vector2I _gridSize = new Vector2I(4, 5);
	private List<KlotskiBlock> _blocks = new();
	private KlotskiBlock _selectedBlock = null;
	private bool _won = false;

	private AnimatedSprite2D _boardSprite;
	private Vector2 _boardFrameSize = Vector2.Zero;

	private CanvasLayer _uiLayer;
	private HBoxContainer _bottomButtons;

	private readonly Dictionary<Button, Tween> _buttonTweens = new();

	private readonly Color _boardColor = new Color(0.14f, 0.11f, 0.09f);
	private readonly Color _gridLineColor = new Color(1f, 1f, 1f, 0.12f);
	private readonly Color _borderColor = new Color(0.50f, 0.38f, 0.26f);

	private readonly Color _heroColor = new Color(0.80f, 0.25f, 0.22f);
	private readonly Color _neutralBlockColor = new Color(0.64f, 0.63f, 0.58f);

	private int _exitRow = 2;
	private int _exitHeightCells = 2;

	public override void _Ready()
	{
		LoadBoardAnimation();

		LoadMatrixFromFile(LevelPath);

		UpdateBoardMinimumSize();

		CreateBottomButtons();

		GetTree().Root.SetMeta("board", this);
	}

	private void LoadBoardAnimation()
	{
		if (!ResourceLoader.Exists(BoardSpritePath))
		{
			GD.PrintErr($"Board sprite not found: {BoardSpritePath}");
			return;
		}

		if (_boardSprite != null && IsInstanceValid(_boardSprite))
		{
			_boardSprite.QueueFree();
			_boardSprite = null;
		}

		Texture2D tex = ResourceLoader.Load<Texture2D>(BoardSpritePath);

		int safeFrameCount = Math.Max(1, BoardFrameCount);
		int frameWidth = tex.GetWidth() / safeFrameCount;
		int frameHeight = tex.GetHeight();

		_boardFrameSize = new Vector2(frameWidth, frameHeight);

		var frames = new SpriteFrames();

		if (!frames.HasAnimation("default"))
			frames.AddAnimation("default");

		for (int i = 0; i < safeFrameCount; i++)
		{
			var frame = new AtlasTexture();
			frame.Atlas = tex;
			frame.Region = new Rect2(
				i * frameWidth,
				0,
				frameWidth,
				frameHeight
			);

			frames.AddFrame("default", frame);
		}

		frames.SetAnimationSpeed("default", BoardAnimationFps);

		_boardSprite = new AnimatedSprite2D();
		_boardSprite.SpriteFrames = frames;
		_boardSprite.Animation = "default";

		_boardSprite.Position = new Vector2(
			frameWidth / 2f,
			frameHeight / 2f
		);

		_boardSprite.ZIndex = -100;

		AddChild(_boardSprite);
		MoveChild(_boardSprite, 0);

		_boardSprite.Play("default");
	}

	private void CreateBottomButtons()
	{
		_uiLayer = new CanvasLayer();
		AddChild(_uiLayer);

		_bottomButtons = new HBoxContainer();

		_bottomButtons.AnchorLeft = 0f;
		_bottomButtons.AnchorRight = 1f;
		_bottomButtons.AnchorTop = 1f;
		_bottomButtons.AnchorBottom = 1f;

		_bottomButtons.OffsetLeft = BottomButtonMargin;
		_bottomButtons.OffsetRight = -BottomButtonMargin;
		_bottomButtons.OffsetTop = -(BottomButtonHeight + BottomButtonMargin);
		_bottomButtons.OffsetBottom = -BottomButtonMargin;

		_bottomButtons.AddThemeConstantOverride("separation", BottomButtonSpacing);

		_uiLayer.AddChild(_bottomButtons);

		for (int i = 1; i <= 4; i++)
		{
			int solutionNumber = i;

			Button solutionButton = CreateBottomButton($"Solution {solutionNumber}");

			solutionButton.Pressed += () =>
			{
				OnSolutionPressed(solutionNumber);
			};

			_bottomButtons.AddChild(solutionButton);
		}

		Button askGeminiButton = CreateBottomButton("Ask Gemini");

		askGeminiButton.Pressed += OnAskGeminiPressed;

		_bottomButtons.AddChild(askGeminiButton);
	}

	private Button CreateBottomButton(string text)
	{
		Button button = new Button();

		button.Text = text;

		// Equal button width
		button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		button.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

		button.FocusMode = Control.FocusModeEnum.None;

		// Slight transparency when idle
		button.Modulate = new Color(1f, 1f, 1f, 0.92f);

		// Text styling
		button.AddThemeFontSizeOverride("font_size", 18);
		button.AddThemeColorOverride("font_color", new Color(1f, 0.92f, 0.82f));
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.86f, 0.55f));

		// Normal style
		var normal = new StyleBoxFlat();
		normal.BgColor = new Color(0.22f, 0.10f, 0.16f);
		normal.BorderColor = new Color(0.75f, 0.35f, 0.45f);
		normal.SetBorderWidthAll(3);
		normal.SetCornerRadiusAll(14);
		normal.ContentMarginLeft = 12;
		normal.ContentMarginRight = 12;
		normal.ContentMarginTop = 8;
		normal.ContentMarginBottom = 8;

		// Hover style
		var hover = new StyleBoxFlat();
		hover.BgColor = new Color(0.34f, 0.14f, 0.24f);
		hover.BorderColor = new Color(1f, 0.50f, 0.62f);
		hover.SetBorderWidthAll(3);
		hover.SetCornerRadiusAll(14);
		hover.ContentMarginLeft = 12;
		hover.ContentMarginRight = 12;
		hover.ContentMarginTop = 8;
		hover.ContentMarginBottom = 8;

		// Pressed style
		var pressed = new StyleBoxFlat();
		pressed.BgColor = new Color(0.16f, 0.07f, 0.12f);
		pressed.BorderColor = new Color(1f, 0.75f, 0.45f);
		pressed.SetBorderWidthAll(3);
		pressed.SetCornerRadiusAll(14);
		pressed.ContentMarginLeft = 12;
		pressed.ContentMarginRight = 12;
		pressed.ContentMarginTop = 8;
		pressed.ContentMarginBottom = 8;

		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("focus", normal);

		// Animation signals
		button.MouseEntered += () =>
		{
			AnimateButtonHover(button, true);
		};

		button.MouseExited += () =>
		{
			AnimateButtonHover(button, false);
		};

		button.ButtonDown += () =>
		{
			AnimateButtonPress(button);
		};

		button.ButtonUp += () =>
		{
			AnimateButtonRelease(button);
		};

		return button;
	}

	private void KillButtonTween(Button button)
	{
		if (_buttonTweens.ContainsKey(button))
		{
			Tween oldTween = _buttonTweens[button];

			if (oldTween != null && IsInstanceValid(oldTween))
				oldTween.Kill();

			_buttonTweens.Remove(button);
		}
	}

	private void AnimateButtonHover(Button button, bool hovering)
	{
		KillButtonTween(button);

		Tween tween = CreateTween();
		_buttonTweens[button] = tween;

		float targetScale = hovering ? 1.06f : 1.0f;
		float targetAlpha = hovering ? 1.0f : 0.92f;

		button.PivotOffset = button.Size / 2f;

		tween.SetParallel(true);

		tween.TweenProperty(
			button,
			"scale",
			new Vector2(targetScale, targetScale),
			0.12
		).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

		tween.TweenProperty(
			button,
			"modulate:a",
			targetAlpha,
			0.12
		).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
	}

	private void AnimateButtonPress(Button button)
	{
		KillButtonTween(button);

		Tween tween = CreateTween();
		_buttonTweens[button] = tween;

		button.PivotOffset = button.Size / 2f;

		tween.TweenProperty(
			button,
			"scale",
			new Vector2(0.96f, 0.96f),
			0.06
		).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
	}

	private void AnimateButtonRelease(Button button)
	{
		KillButtonTween(button);

		Tween tween = CreateTween();
		_buttonTweens[button] = tween;

		button.PivotOffset = button.Size / 2f;

		tween.TweenProperty(
			button,
			"scale",
			new Vector2(1.06f, 1.06f),
			0.08
		).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}

	private void OnSolutionPressed(int solutionNumber)
	{
		GD.Print($"Solution {solutionNumber} pressed");

		// Later, play hard-coded solution here.
		// Example:
		// PlaySolution(solutionNumber);
	}

	private void OnAskGeminiPressed()
	{
		GD.Print("Ask Gemini pressed");

		string json = ExportMatrixJson();

		GD.Print("Current board sent to Gemini:");
		GD.Print(json);

		// Later, Gemini API request goes here.
	}

	private void UpdateBoardMinimumSize()
	{
		Vector2 gridPixelSize = GridOffset + new Vector2(
			_gridSize.X * CellSize,
			_gridSize.Y * CellSize
		);

		Vector2 finalSize = new Vector2(
			Mathf.Max(gridPixelSize.X, _boardFrameSize.X),
			Mathf.Max(gridPixelSize.Y, _boardFrameSize.Y)
		);

		CustomMinimumSize = finalSize;
		Size = finalSize;
	}

	private void CreateBlock(string id, Vector2I pos, Vector2I size, Color color)
	{
		var block = BlockTemplate.Instantiate<KlotskiBlock>();

		AddChild(block);

		block.ZIndex = 10;
		block.Board = this;

		block.Setup(id, pos, size, CellSize, color, GridOffset);

		_blocks.Add(block);
	}

	public bool CanMove(KlotskiBlock block, Vector2I direction)
	{
		bool isSquare = block.BlockSize.X == block.BlockSize.Y;

		if (!isSquare)
		{
			bool isHorizontal = block.BlockSize.X > block.BlockSize.Y;
			bool isVertical = block.BlockSize.Y > block.BlockSize.X;

			if (isHorizontal && direction.Y != 0) return false;
			if (isVertical && direction.X != 0) return false;
		}

		Vector2I newPos = block.GridPos + direction;

		if (
			newPos.X < 0 ||
			newPos.Y < 0 ||
			newPos.X + block.BlockSize.X > _gridSize.X ||
			newPos.Y + block.BlockSize.Y > _gridSize.Y
		)
		{
			return false;
		}

		foreach (var other in _blocks)
		{
			if (other == block) continue;

			bool overlapping =
				newPos.X < other.GridPos.X + other.BlockSize.X &&
				newPos.X + block.BlockSize.X > other.GridPos.X &&
				newPos.Y < other.GridPos.Y + other.BlockSize.Y &&
				newPos.Y + block.BlockSize.Y > other.GridPos.Y;

			if (overlapping)
				return false;
		}

		return true;
	}

	public void SelectBlock(KlotskiBlock block)
	{
		if (_won) return;

		if (_selectedBlock != null)
			_selectedBlock.SetHighlight(false);

		_selectedBlock = block;
		_selectedBlock.SetHighlight(true);

		GD.Print($"Selected: {block.ID}");
	}

	public override void _Input(InputEvent @event)
	{
		if (_won) return;

		if (@event.IsActionPressed("export_board"))
		{
			ExportCurrentBoard();
			return;
		}

		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
			{
				ExportCurrentBoard();
				return;
			}
		}

		if (_selectedBlock == null) return;

		Vector2I dir = Vector2I.Zero;

		if (@event.IsActionPressed("ui_up")) dir = Vector2I.Up;
		if (@event.IsActionPressed("ui_down")) dir = Vector2I.Down;
		if (@event.IsActionPressed("ui_left")) dir = Vector2I.Left;
		if (@event.IsActionPressed("ui_right")) dir = Vector2I.Right;

		if (dir != Vector2I.Zero && CanMove(_selectedBlock, dir))
		{
			_selectedBlock.SlideTo(_selectedBlock.GridPos + dir);
			CheckWin(_selectedBlock);
		}
	}

	private void ExportCurrentBoard()
	{
		string json = ExportMatrixJson();

		GD.Print("Current board JSON:");
		GD.Print(json);

		SaveMatrixToFile(ExportPath);

		GD.Print($"Board exported to: {ExportPath}");
	}

	private void CheckWin(KlotskiBlock block)
	{
		if (block.ID != "1") return;

		bool reachedExit =
			block.GridPos.X + block.BlockSize.X >= _gridSize.X &&
			block.GridPos.Y == _exitRow;

		if (reachedExit)
		{
			_won = true;

			block.PlayWinAnimation();

			if (_selectedBlock != null)
				_selectedBlock.SetHighlight(false);

			_selectedBlock = null;

			GD.Print("You win!");
		}
	}

	public override void _Draw()
	{
		float boardW = _gridSize.X * CellSize;
		float boardH = _gridSize.Y * CellSize;

		if (_boardSprite == null)
		{
			DrawRect(
				new Rect2(GridOffset, new Vector2(boardW, boardH)),
				_boardColor
			);

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

			DrawLine(
				new Vector2(x, GridOffset.Y),
				new Vector2(x, GridOffset.Y + boardH),
				_gridLineColor,
				1f
			);
		}

		for (int r = 1; r < _gridSize.Y; r++)
		{
			float y = GridOffset.Y + r * CellSize;

			DrawLine(
				new Vector2(GridOffset.X, y),
				new Vector2(GridOffset.X + boardW, y),
				_gridLineColor,
				1f
			);
		}
	}

	private void DrawBoardOutline(float boardW, float boardH)
	{
		float outlineWidth = 4f;

		float exitY = GridOffset.Y + _exitRow * CellSize;
		float exitH = _exitHeightCells * CellSize;

		float left = GridOffset.X;
		float right = GridOffset.X + boardW;
		float top = GridOffset.Y;
		float bottom = GridOffset.Y + boardH;

		DrawLine(new Vector2(left, top), new Vector2(right, top), _borderColor, outlineWidth);
		DrawLine(new Vector2(left, bottom), new Vector2(right, bottom), _borderColor, outlineWidth);
		DrawLine(new Vector2(left, top), new Vector2(left, bottom), _borderColor, outlineWidth);

		DrawLine(new Vector2(right, top), new Vector2(right, exitY), _borderColor, outlineWidth);
		DrawLine(new Vector2(right, exitY + exitH), new Vector2(right, bottom), _borderColor, outlineWidth);
	}

	public void LoadMatrixFromFile(string path)
	{
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

		if (file == null)
		{
			GD.PrintErr($"Could not open layout file: {path}");
			return;
		}

		var text = file.GetAsText();
		file.Close();

		var json = new Json();
		var error = json.Parse(text);

		if (error != Error.Ok)
		{
			GD.PrintErr("JSON parse error: ", json.GetErrorMessage());
			return;
		}

		var dict = json.Data.AsGodotDictionary();

		if (!dict.ContainsKey("grid"))
		{
			GD.PrintErr("JSON file does not contain a 'grid' field.");
			return;
		}

		var gridList = dict["grid"].AsGodotArray();

		if (gridList.Count == 0)
		{
			GD.PrintErr("Grid is empty.");
			return;
		}

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
		_won = false;

		if (_selectedBlock != null)
		{
			if (IsInstanceValid(_selectedBlock))
				_selectedBlock.SetHighlight(false);

			_selectedBlock = null;
		}

		foreach (var b in _blocks)
		{
			if (IsInstanceValid(b))
				b.QueueFree();
		}

		_blocks.Clear();

		_gridSize = new Vector2I(matrix[0].Length, matrix.Length);
		UpdateBoardMinimumSize();

		bool[,] visited = new bool[_gridSize.X, _gridSize.Y];

		for (int y = 0; y < _gridSize.Y; y++)
		{
			for (int x = 0; x < _gridSize.X; x++)
			{
				int id = matrix[y][x];

				if (id == 0 || visited[x, y])
					continue;

				var block = ExtractBlockFromMatrix(matrix, id, x, y, visited);
				Color color = block.id == "1" ? _heroColor : _neutralBlockColor;

				CreateBlock(block.id, block.pos, block.size, color);
			}
		}

		QueueRedraw();
	}

	private (string id, Vector2I pos, Vector2I size) ExtractBlockFromMatrix(
		int[][] matrix,
		int id,
		int startX,
		int startY,
		bool[,] visited
	)
	{
		int width = matrix[0].Length;
		int height = matrix.Length;

		int minX = startX;
		int maxX = startX;
		int minY = startY;
		int maxY = startY;

		Queue<Vector2I> q = new();
		q.Enqueue(new Vector2I(startX, startY));
		visited[startX, startY] = true;

		while (q.Count > 0)
		{
			var p = q.Dequeue();

			minX = Math.Min(minX, p.X);
			maxX = Math.Max(maxX, p.X);
			minY = Math.Min(minY, p.Y);
			maxY = Math.Max(maxY, p.Y);

			foreach (var dir in new Vector2I[]
			{
				Vector2I.Up,
				Vector2I.Down,
				Vector2I.Left,
				Vector2I.Right
			})
			{
				int nx = p.X + dir.X;
				int ny = p.Y + dir.Y;

				if (nx < 0 || ny < 0 || nx >= width || ny >= height)
					continue;

				if (visited[nx, ny])
					continue;

				if (matrix[ny][nx] == id)
				{
					visited[nx, ny] = true;
					q.Enqueue(new Vector2I(nx, ny));
				}
			}
		}

		return (
			id.ToString(),
			new Vector2I(minX, minY),
			new Vector2I(maxX - minX + 1, maxY - minY + 1)
		);
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
			{
				for (int dx = 0; dx < block.BlockSize.X; dx++)
				{
					matrix[block.GridPos.Y + dy][block.GridPos.X + dx] = id;
				}
			}
		}

		return matrix;
	}

	public string ExportMatrixJson()
	{
		int[][] matrix = ExportMatrixLayout();
		var outer = new Godot.Collections.Array();

		for (int y = 0; y < matrix.Length; y++)
		{
			var row = new Godot.Collections.Array();

			for (int x = 0; x < matrix[y].Length; x++)
				row.Add(matrix[y][x]);

			outer.Add(row);
		}

		var wrapper = new Godot.Collections.Dictionary<string, Variant>();
		wrapper["grid"] = outer;

		return Json.Stringify(wrapper, "\t");
	}

	public void SaveMatrixToFile(string path)
	{
		string json = ExportMatrixJson();

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

		if (file == null)
		{
			GD.PrintErr($"Could not write to file: {path}");
			return;
		}

		file.StoreString(json);
	}
}
