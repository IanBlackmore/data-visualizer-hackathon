using Godot;
using System;

public partial class BottomBar : Control
{
	private Board _board;

	public override void _Ready()
	{
		_board = (Board)GetTree().Root.GetMeta("board");
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
		_board?.SaveMatrixToFile("res://Layouts/exported_level.json");
		GD.Print("Board exported!");
	}
	
}
