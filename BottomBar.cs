using Godot;
using System;

public partial class BottomBar : Control
{
	private Board _board;
	private HttpRequest _geminiRequest;

	public override void _Ready()
	{
		// Go up to the root, then down the path to Board
		_board = GetNode<Board>("/root/Testscene/SubViewportContainer/SubViewport/Node2D/CanvasLayer/Control/Board");
		_geminiRequest = GetNode<HttpRequest>("GeminiRequest");
		GD.Print("Board reference: ", _board);
	}

	public void _on_scn_1_btn_pressed()
	{
		GD.Print("Scene 1 Button Pressed!");
		_board?.LoadMatrixFromFile("res://Layouts/level1.json");
		QueueRedraw();
	}

	public void _on_scn_2_btn_pressed()
	{
		GD.Print("Scene 2 Button Pressed!");
		_board?.LoadMatrixFromFile("res://Layouts/level2.json");
		QueueRedraw();
	}
	public void _on_ask_btn_pressed()
	{
		GD.Print("Export JSON Button Pressed!");
		
		// Save and load current matrix json
		_board?.SaveMatrixToFile("res://Layouts/exported_level.json");
		string boardJson = _board.ExportMatrixJson();

		// Gemini API endpoint and key
		string YOUR_API_KEY = "AIzaSyCkIg-Mdn13fSLqp8sou13oYhB-dGGgLcU";

		string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key="+YOUR_API_KEY;
		string[] headers = { "Content-Type: application/json" };

		// Prepare the prompt and request body
		string prompt = $"hello gemini can you read this json and return it back to me with all 2's set to 6's?\n\n{boardJson}";
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

		// TODO: Parse and use the response as needed
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

}
