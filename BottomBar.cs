using Godot;
using System;

public partial class BottomBar : Control
{
	[Export] public NodePath BoardPath = new NodePath("/root/Testscene/SubViewportContainer/SubViewport/Node2D/CanvasLayer/Control/Board");

	[Export] public int ButtonMargin  = 12;
	[Export] public int ButtonSpacing = 8;

	// The sprite sheet has 3 frames total — all played as one looping animation at 10 fps.
	[Export] public int   TotalFrames       = 3;
	[Export] public float ButtonAnimationFps = 10f;

	[Export] public string Solution1SpritePath = "res://Art/solution_1.png";
	[Export] public string Solution2SpritePath = "res://Art/solution_2.png";
	[Export] public string Solution3SpritePath = "res://Art/solution_3.png";
	[Export] public string Solution4SpritePath = "res://Art/solution_4.png";
	[Export] public string AskGeminiSpritePath  = "res://Art/ask_gemini.png";

	[Export] public bool   AutoSolveAfterLevelLoad = true;
	[Export] public string ExportPath = "res://Layouts/exported_level.json";

	private Board _board;
	private HBoxContainer _buttonRow;

	public override void _Ready()
	{
		CallDeferred(nameof(Initialize));
	}

	private void Initialize()
	{
		ResolveBoard();
		BuildButtons();
	}

	// -------------------------------------------------------------------------
	// Board resolution
	// -------------------------------------------------------------------------

	private void ResolveBoard()
	{
		_board = GetNodeOrNull<Board>(BoardPath);
		if (_board == null)
			_board = FindBoard(GetTree().Root);
		GD.Print("Board reference: ", _board);
	}

	private Board FindBoard(Node node)
	{
		if (node is Board board) return board;
		foreach (Node child in node.GetChildren())
		{
			Board found = FindBoard(child);
			if (found != null) return found;
		}
		return null;
	}

	private void ResolveBoardIfMissing()
	{
		if (_board == null || !IsInstanceValid(_board))
			ResolveBoard();
	}

	// -------------------------------------------------------------------------
	// UI construction
	// -------------------------------------------------------------------------

	private void BuildButtons()
	{
		foreach (Node child in GetChildren())
			child.QueueFree();

		AnchorLeft   = 0f; AnchorRight  = 1f;
		AnchorTop    = 0f; AnchorBottom = 1f;
		OffsetLeft   = 0f; OffsetRight  = 0f;
		OffsetTop    = 0f; OffsetBottom = 0f;

		// Strip pinned to the bottom — height = 72 (natural sprite height) + margins
		var bottomHolder = new Control
		{
			AnchorLeft   = 0f,
			AnchorRight  = 1f,
			AnchorTop    = 1f,
			AnchorBottom = 1f,
			OffsetLeft   =  ButtonMargin,
			OffsetRight  = -ButtonMargin,
			OffsetTop    = -(72 + ButtonMargin),
			OffsetBottom = -ButtonMargin
		};
		AddChild(bottomHolder);

		_buttonRow = new HBoxContainer
		{
			AnchorLeft   = 0f, AnchorRight  = 1f,
			AnchorTop    = 0f, AnchorBottom = 1f,
			OffsetLeft   = 0f, OffsetRight  = 0f,
			OffsetTop    = 0f, OffsetBottom = 0f,
			Alignment           = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical   = Control.SizeFlags.ExpandFill
		};
		_buttonRow.AddThemeConstantOverride("separation", ButtonSpacing);
		bottomHolder.AddChild(_buttonRow);

		for (int n = 1; n <= 4; n++)
		{
			int captured = n;
			var btn = CreateImageButton(ResolveSpritePath(GetSolutionSpritePath(n)));
			btn.Pressed += () => OnSolutionPressed(captured);
			_buttonRow.AddChild(btn);
		}

		var askGemini = CreateImageButton(ResolveSpritePath(AskGeminiSpritePath));
		askGemini.Pressed += OnAskGeminiPressed;
		_buttonRow.AddChild(askGemini);
	}

	private string GetSolutionSpritePath(int n) => n switch
	{
		1 => Solution1SpritePath,
		2 => Solution2SpritePath,
		3 => Solution3SpritePath,
		4 => Solution4SpritePath,
		_ => Solution1SpritePath
	};

	private string ResolveSpritePath(string path)
	{
		if (ResourceLoader.Exists(path)) return path;

		if (path.Contains("solution"))
		{
			string typoPath = path.Replace("solution", "soulution");
			if (ResourceLoader.Exists(typoPath))
			{
				GD.Print($"Using typo sprite path: {typoPath}");
				return typoPath;
			}
		}

		GD.PrintErr($"Button sprite file not found: {path}");
		return path;
	}

	private AnimatedFrameButton CreateImageButton(string spritePath)
	{
		return new AnimatedFrameButton
		{
			SpriteSheetPath  = spritePath,
			TotalFrames      = TotalFrames,       // 3 frames, all looped
			AnimationFps     = ButtonAnimationFps, // 10 fps
			StretchToFit     = false,              // keep natural 300x72 size

			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter,
			FocusMode           = Control.FocusModeEnum.None
		};
	}

	// -------------------------------------------------------------------------
	// Button handlers
	// -------------------------------------------------------------------------

	private void OnSolutionPressed(int n)
	{
		ResolveBoardIfMissing();

		if (_board == null)
		{
			GD.PrintErr("No Board found. Check BoardPath on BottomBar.");
			return;
		}

		string levelPath = $"res://Layouts/level{n}.json";
		if (!FileAccess.FileExists(levelPath))
		{
			GD.PrintErr($"Missing level file: {levelPath}");
			return;
		}

		GD.Print($"Solution {n} pressed. Loading {levelPath}");
		_board.LoadMatrixFromFile(levelPath);

		if (AutoSolveAfterLevelLoad)
			_board.SolvePuzzle();
	}

	private void OnAskGeminiPressed()
	{
		ResolveBoardIfMissing();

		if (_board == null)
		{
			GD.PrintErr("No Board found. Check BoardPath on BottomBar.");
			return;
		}

		GD.Print("Ask Gemini pressed. Current board JSON:");
		GD.Print(_board.ExportMatrixJson());
		_board.SaveMatrixToFile(ExportPath);
		GD.Print($"Board exported to: {ExportPath}");
	}

	// Legacy signal handlers
	public void _on_scn_1_btn_pressed() => OnSolutionPressed(1);
	public void _on_scn_2_btn_pressed() => OnSolutionPressed(2);
	public void _on_ask_btn_pressed()   => OnAskGeminiPressed();
}
