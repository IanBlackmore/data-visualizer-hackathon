using Godot;
using System;

public partial class BottomBar : Control
{
	private Board _board;

	public override void _Ready()
	{
		_board = (Board)GetTree().Root.GetMeta("board");
	}

	public void _on_scn_1_btn_pressed()
	{
		GD.Print("Scene 1 Button Pressed!");
		_board?.LoadMatrixFromFile("res://Layouts/level1.json");
	}

	public void _on_scn_2_btn_pressed()
	{
		GD.Print("Scene 2 Button Pressed!");
		_board?.LoadMatrixFromFile("res://Layouts/level2.json");
	}
}
