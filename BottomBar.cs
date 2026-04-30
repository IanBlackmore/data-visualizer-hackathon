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
	[Export] public string GeminiApiKey = "";

	private Board _board;
	private HttpRequest _geminiRequest;
	private HBoxContainer _buttonRow;

	public override void _Ready()
	{
		CallDeferred(nameof(Initialize));

		_geminiRequest = GetNode<HttpRequest>("GeminiRequest");
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
		{
			if (child is HttpRequest) continue; // Don't free GeminiRequest
			child.QueueFree();
		}

		AnchorLeft   = 0f; AnchorRight  = 1f;
		AnchorTop    = 0f; AnchorBottom = 1f;
		OffsetLeft   = 0f; OffsetRight  = 0f;
		OffsetTop    = 0f; OffsetBottom = 0f;

		// Use the actual viewport space given to the bottom bar.
		// Do not assume the screen is wide enough for five natural 300px buttons.
		var bottomHolder = new Control
		{
			AnchorLeft   = 0f,
			AnchorRight  = 1f,
			AnchorTop    = 0f,
			AnchorBottom = 1f,
			OffsetLeft   =  ButtonMargin,
			OffsetRight  = -ButtonMargin,
			OffsetTop    =  ButtonMargin,
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
			SpriteSheetPath       = spritePath,
			TotalFrames           = TotalFrames,       // 3 frames, all looped
			AnimationFps          = ButtonAnimationFps, // 10 fps
			StretchToFit          = true,               // shrink/grow inside the real bar width
			PreserveAspectRatio   = true,               // no ugly horizontal squish
			CustomMinimumSize     = Vector2.Zero,       // let HBoxContainer shrink it

			SizeFlagsHorizontal   = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical     = Control.SizeFlags.ExpandFill,
			SizeFlagsStretchRatio = 1f,
			FocusMode             = Control.FocusModeEnum.None
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
			$"You are analyzing a Klotski-style sliding block puzzle represented as a JSON matrix.\n\n" +
			"CONTEXT:\n" +
			"- The board is a 2D grid of integers.\n" +
			"- Each non-zero integer represents a block ID.\n" +
			"- Block ID \"1\" is the HERO block.\n" +
			"- The hero block must reach the WINNING POSITION to solve the puzzle.\n\n" +
			"RULES:\n" +
			"- Blocks slide along their long face (a 1highx2wide will move LEFT/RIGHT, a 2highx1wide will move UP, DOWN).\n" +
			"- A \"move\" is defined as sliding a single block by one grid cell.\n" +
			"- Blocks cannot overlap or leave the board.\n" +
			"- The goal is to compute the MINIMUM number of moves required to reach the winning state.\n\n" +
			"WIN CONDITION:\n" +
			"The puzzle is solved when the HERO block (ID 1) has its RIGHT EDGE aligned with the RIGHT-CENTER of the board.\n" +
			"Example:\n\n" +
			"Start:\n" +
			"[0,0,0,0,0,0]\n" +
			"[0,0,0,0,0,0]\n" +
			"[1,1,0,0,0,0]\n" +
			"[0,0,0,0,0,0]\n" +
			"[0,0,0,0,0,0]\n" +
			"[0,0,0,0,0,0]\n\n" +
			"Winning:\n" +
			"[0,0,0,0,0,0]\n" +
			"[0,0,0,0,0,0]\n" +
			"[0,0,0,0,1,1]\n" +
			"[0,0,0,0,0,0]\n" +
			"[0,0,0,0,0,0]\n" +
			"[0,0,0,0,0,0]\n\n" +
			"TASK:\n" +
			"Given the following board state, compute:\n" +
			"1. The MINIMUM number of moves required to reach the winning state.\n" +
			"2. A step-by-step ordered list of moves.\n\n" +
			"REQUIRED OUTPUT FORMAT:\n" +
			"--- MOVE SEQUENCE TO SOLUTION ---\n" +
			"Step 1: Block 'X' moved DIRECTION to (newX, newY)\n" +
			"Step 2: Block 'Y' moved DIRECTION to (newX, newY)\n" +
			"...\n" +
			"Total Moves: N\n" +
			"---------------------------------\n\n" +
			"Here is the board JSON to analyze:\n" +
			$"{boardJson}";

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
		GD.Print("Gemini API response recieved: ");

		var json = new Godot.Json();
		if (json.Parse(response) != Error.Ok)
		{
			GD.PrintErr("Failed to parse JSON.");
			return;
		}

		var root = json.Data.AsGodotDictionary();

		if (!root.ContainsKey("candidates"))
		{
			GD.PrintErr("No candidates in response.");
			return;
		}

		var candidates = root["candidates"].AsGodotArray();
		if (candidates.Count == 0)
		{
			GD.PrintErr("Candidates array empty.");
			return;
		}

		var content = candidates[0].AsGodotDictionary()["content"].AsGodotDictionary();
		var parts = content["parts"].AsGodotArray();

		// Collect ONLY "text" fields
		System.Text.StringBuilder sb = new System.Text.StringBuilder();

		foreach (var partObj in parts)
		{
			var part = partObj.AsGodotDictionary();

			if (part.ContainsKey("text"))
			{
				sb.Append(part["text"].AsString());
				sb.Append("\n");
			}
		}

		string llmText = sb.ToString().Trim();
		GD.Print("Gemini LLM (cleaned):\n" + llmText);

		// Save parsed move sequence to JSON
		SaveGeminiResponseToJson(llmText);
	}

	/// <summary>
	/// Parses the Gemini LLM output for the required move sequence and saves it as JSON.
	/// </summary>
	/// <param name="llmText">The full Gemini LLM output text.</param>
	private void SaveGeminiResponseToJson(string llmText)
	   {
		   // Find the required section
		   string startTag = "--- MOVE SEQUENCE TO SOLUTION ---";
		   string endTag = "---------------------------------";
		   int startIdx = llmText.IndexOf(startTag);
		   int endIdx = llmText.IndexOf(endTag, startIdx + startTag.Length);
		   if (startIdx == -1 || endIdx == -1)
		   {
			   GD.PrintErr("Could not find required move sequence section in Gemini response.");
			   return;
		   }

		   // Extract the section
		   int contentStart = startIdx + startTag.Length;
		   string section = llmText.Substring(contentStart, endIdx - contentStart).Trim();

		   // Split into lines and parse steps
		   var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		   var steps = new System.Collections.Generic.List<string>();
		   int totalMoves = -1;
		   foreach (var line in lines)
		   {
			   if (line.StartsWith("Step "))
			   {
				   steps.Add(line.Trim());
			   }
			   else if (line.StartsWith("Total Moves:"))
			   {
				   var parts = line.Split(':');
				   if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int moves))
					   totalMoves = moves;
			   }
		   }

		   // Build the JSON object
		   var responseObj = new System.Collections.Generic.Dictionary<string, object>
		   {
			   { "steps", steps },
			   { "total_moves", totalMoves }
		   };

		   // Serialize to JSON
		   string jsonString = System.Text.Json.JsonSerializer.Serialize(responseObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

		   // Save to file
		   string savePath = "res://Layouts/GeminiResponse.json";
		   using var file = FileAccess.Open(savePath, FileAccess.ModeFlags.Write);
		   file.StoreString(jsonString);
		   GD.Print($"Gemini response saved to {savePath}");
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

	// Legacy signal handlers
	public void _on_scn_1_btn_pressed() => OnSolutionPressed(1);
	public void _on_scn_2_btn_pressed() => OnSolutionPressed(2);
	public void _on_ask_btn_pressed()   => OnAskGeminiPressed();
}
