using Godot;
using System.Collections.Generic;

public partial class BottomBar : Control
{
	[Export] public NodePath BoardPath = new NodePath("/root/Testscene/SubViewportContainer/SubViewport/Node2D/CanvasLayer/Control/Board");
	[Export] public int ButtonHeight = 56;
	[Export] public int ButtonMargin = 24;
	[Export] public int ButtonSpacing = 28;
	[Export] public bool AutoSolveAfterLevelLoad = true;
	[Export] public string ExportPath = "res://Layouts/exported_level.json";
	[Export] public string GeminiApiKey = "";

	private Board _board;
	private HttpRequest _geminiRequest;
	private HBoxContainer _buttonRow;
	private readonly Dictionary<Button, Tween> _buttonTweens = new();

	public override void _Ready()
	{
		CallDeferred(nameof(Initialize));

		_geminiRequest = GetNode<HttpRequest>("GeminiRequest");
	}

	private void Initialize()
	{
		ResolveBoard();
		BuildVersionOneButtonsOnTestScene();
	}

	private void ResolveBoard()
	{
		_board = GetNodeOrNull<Board>(BoardPath);

		if (_board == null)
			_board = FindBoard(GetTree().Root);

		GD.Print("Board reference: ", _board);
	}

	private Board FindBoard(Node node)
	{
		if (node is Board board)
			return board;

		foreach (Node child in node.GetChildren())
		{
			Board found = FindBoard(child);
			if (found != null)
				return found;
		}

		return null;
	}

	private void BuildVersionOneButtonsOnTestScene()
	{
		foreach (Node child in GetChildren())
		{
			if (child is HttpRequest) continue; // Don't free GeminiRequest
			child.QueueFree();
		}

		AnchorLeft = 0f;
		AnchorRight = 1f;
		AnchorTop = 0f;
		AnchorBottom = 1f;
		OffsetLeft = 0f;
		OffsetRight = 0f;
		OffsetTop = 0f;
		OffsetBottom = 0f;

		var margin = new MarginContainer
		{
			AnchorLeft = 0f,
			AnchorRight = 1f,
			AnchorTop = 0f,
			AnchorBottom = 1f,
			OffsetLeft = 0f,
			OffsetRight = 0f,
			OffsetTop = 0f,
			OffsetBottom = 0f
		};

		margin.AddThemeConstantOverride("margin_left", ButtonMargin);
		margin.AddThemeConstantOverride("margin_right", ButtonMargin);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		AddChild(margin);

		_buttonRow = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};

		_buttonRow.AddThemeConstantOverride("separation", ButtonSpacing);
		margin.AddChild(_buttonRow);

		for (int i = 1; i <= 4; i++)
		{
			int n = i;
			var btn = CreateBottomButton($"Solution {n}");
			btn.Pressed += () => OnSolutionPressed(n);
			_buttonRow.AddChild(btn);
		}

		var geminiBtn = CreateBottomButton("Ask Gemini");
		geminiBtn.Pressed += OnAskGeminiPressed;
		_buttonRow.AddChild(geminiBtn);
	}

	private Button CreateBottomButton(string text)
	{
		var button = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(0, ButtonHeight),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			FocusMode = Control.FocusModeEnum.None,
			Modulate = new Color(1f, 1f, 1f, 0.92f)
		};

		button.AddThemeFontSizeOverride("font_size", 18);
		button.AddThemeColorOverride("font_color", new Color(1f, 0.92f, 0.82f));
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.86f, 0.55f));

		StyleBoxFlat MakeStyle(Color bg, Color border)
		{
			var s = new StyleBoxFlat();
			s.BgColor = bg;
			s.BorderColor = border;
			s.SetBorderWidthAll(3);
			s.SetCornerRadiusAll(14);
			s.ContentMarginLeft = 12;
			s.ContentMarginRight = 12;
			s.ContentMarginTop = 8;
			s.ContentMarginBottom = 8;
			return s;
		}

		var normal = MakeStyle(new Color(0.22f, 0.10f, 0.16f), new Color(0.75f, 0.35f, 0.45f));
		var hover = MakeStyle(new Color(0.34f, 0.14f, 0.24f), new Color(1f, 0.50f, 0.62f));
		var pressed = MakeStyle(new Color(0.16f, 0.07f, 0.12f), new Color(1f, 0.75f, 0.45f));

		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("focus", normal);

		button.MouseEntered += () => AnimateButtonHover(button, true);
		button.MouseExited += () => AnimateButtonHover(button, false);
		button.ButtonDown += () => AnimateButtonPress(button);
		button.ButtonUp += () => AnimateButtonRelease(button);

		return button;
	}

	private void KillButtonTween(Button button)
	{
		if (_buttonTweens.TryGetValue(button, out var t) && t != null && IsInstanceValid(t))
			t.Kill();

		_buttonTweens.Remove(button);
	}

	private void AnimateButtonHover(Button button, bool hovering)
	{
		KillButtonTween(button);

		var tween = CreateTween();
		_buttonTweens[button] = tween;
		button.PivotOffset = button.Size / 2f;

		tween.SetParallel(true);

		float scale = hovering ? 1.06f : 1.0f;
		float alpha = hovering ? 1.0f : 0.92f;

		tween.TweenProperty(button, "scale", new Vector2(scale, scale), 0.12)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		tween.TweenProperty(button, "modulate:a", alpha, 0.12)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
	}

	private void AnimateButtonPress(Button button)
	{
		KillButtonTween(button);

		var tween = CreateTween();
		_buttonTweens[button] = tween;
		button.PivotOffset = button.Size / 2f;

		tween.TweenProperty(button, "scale", new Vector2(0.96f, 0.96f), 0.06)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
	}

	private void AnimateButtonRelease(Button button)
	{
		KillButtonTween(button);

		var tween = CreateTween();
		_buttonTweens[button] = tween;
		button.PivotOffset = button.Size / 2f;

		tween.TweenProperty(button, "scale", new Vector2(1.06f, 1.06f), 0.08)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
	}

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

		GD.Print("Ask Gemini pressed...");
		
		// Save and load current matrix json
		_board?.SaveMatrixToFile("res://Layouts/exported_level.json");
		string boardJson = _board.ExportMatrixJson();

		// Gemini API endpoint and key
		string apiKey = LoadGeminiApiKey();

		string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key="+apiKey;
		string[] headers = { "Content-Type: application/json" };

		// Prepare the prompt and request body
		string prompt = 
			$"hello gemini can you read this json and return it back to me with all 2's set to 6's?\n\n{boardJson}";
		string escapedPrompt = System.Text.Json.JsonSerializer.Serialize(prompt);
		// Remove the surrounding quotes added by Serialize
		escapedPrompt = escapedPrompt.Substring(1, escapedPrompt.Length - 2);
		string requestBody = $"{{\"contents\":[{{\"role\":\"user\",\"parts\":[{{\"text\":\"{escapedPrompt}\"}}]}}]}}";
				
		// Send Request
		GD.Print("Sending Request");
		_geminiRequest.Request(url, headers, HttpClient.Method.Post, requestBody);
	}

	
	private void _on_gemini_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		string response = System.Text.Encoding.UTF8.GetString(body);
		GD.Print("Gemini API response: ", response);

		// Parse the response JSON
		var json = new Godot.Json();
		if (json.Parse(response) == Error.Ok)
		{
			var dict = json.Data.AsGodotDictionary();
			if (dict.ContainsKey("candidates"))
			{
				var candidates = dict["candidates"].AsGodotArray();
				if (candidates.Count > 0)
				{
					var content = candidates[0].AsGodotDictionary()["content"].AsGodotDictionary();
					var parts = content["parts"].AsGodotArray();
					if (parts.Count > 0)
					{
						string llmText = parts[0].AsGodotDictionary()["text"].AsString();
						GD.Print("Gemini LLM returned:\n" + llmText);
					}
				}
			}
		}
	}

	private string LoadGeminiApiKey(string path = "res://gemini_api_key.n")
	{
		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"API key file not found: {path}");
			return "";
		}
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		return file.GetLine().StripEdges();
	}

	private void ResolveBoardIfMissing()
	{
		if (_board == null || !IsInstanceValid(_board))
			ResolveBoard();
	}

	public void _on_scn_1_btn_pressed() => OnSolutionPressed(1);
	public void _on_scn_2_btn_pressed() => OnSolutionPressed(2);
	public void _on_ask_btn_pressed() => OnAskGeminiPressed();
}
