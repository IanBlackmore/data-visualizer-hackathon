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

	// Right-side hole in the outline
	private int _exitRow = 2;
	private int _exitHeightCells = 1;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(_gridSize.X * CellSize, _gridSize.Y * CellSize);

		SpawnInitialLayout();
		QueueRedraw();
	}

	private void SpawnInitialLayout()
	{
		// Format: ID, Position(x,y), Size(w,h), Color
		CreateBlock("Hero", new Vector2I(1, 0), new Vector2I(2, 2), _heroColor);

		// Neutral/colorless blocks
		CreateBlock("Vert1", new Vector2I(0, 0), new Vector2I(1, 2), _neutralBlockColor);
		CreateBlock("Vert2", new Vector2I(3, 0), new Vector2I(1, 2), _neutralBlockColor);
		CreateBlock("Small1", new Vector2I(1, 2), new Vector2I(1, 1), _neutralBlockColor);
	}

	private void CreateBlock(string id, Vector2I pos, Vector2I size, Color color)
	{
		var block = BlockTemplate.Instantiate<KlotskiBlock>();

		AddChild(block);

		block.Board = this;
		block.Setup(id, pos, size, CellSize, color);

		_blocks.Add(block);
	}

	public bool CanMove(KlotskiBlock block, Vector2I direction)
	{
		Vector2I newPos = block.GridPos + direction;

		// 1. Boundary check
		if (
			newPos.X < 0 ||
			newPos.Y < 0 ||
			newPos.X + block.BlockSize.X > _gridSize.X ||
			newPos.Y + block.BlockSize.Y > _gridSize.Y
		)
		{
			return false;
		}

		// 2. Collision check against other blocks
		foreach (var other in _blocks)
		{
			if (other == block)
			{
				continue;
			}

			if (
				newPos.X < other.GridPos.X + other.BlockSize.X &&
				newPos.X + block.BlockSize.X > other.GridPos.X &&
				newPos.Y < other.GridPos.Y + other.BlockSize.Y &&
				newPos.Y + block.BlockSize.Y > other.GridPos.Y
			)
			{
				return false;
			}
		}

		return true;
	}

	public void SelectBlock(KlotskiBlock block)
	{
		if (_selectedBlock != null)
		{
			_selectedBlock.SetHighlight(false);
		}

		_selectedBlock = block;
		_selectedBlock.SetHighlight(true);

		GD.Print($"Selected: {block.ID}");
	}

	public override void _Input(InputEvent @event)
	{
		if (_selectedBlock == null)
		{
			return;
		}

		Vector2I dir = Vector2I.Zero;

		if (@event.IsActionPressed("ui_up"))
		{
			dir = Vector2I.Up;
		}

		if (@event.IsActionPressed("ui_down"))
		{
			dir = Vector2I.Down;
		}

		if (@event.IsActionPressed("ui_left"))
		{
			dir = Vector2I.Left;
		}

		if (@event.IsActionPressed("ui_right"))
		{
			dir = Vector2I.Right;
		}

		if (dir != Vector2I.Zero && CanMove(_selectedBlock, dir))
		{
			_selectedBlock.SlideTo(_selectedBlock.GridPos + dir);
		}
	}

	public override void _Draw()
	{
		float boardW = _gridSize.X * CellSize;
		float boardH = _gridSize.Y * CellSize;

		DrawRect(
			new Rect2(Vector2.Zero, new Vector2(boardW, boardH)),
			_boardColor
		);

		DrawGrid(boardW, boardH);
		DrawBoardOutline(boardW, boardH);
	}

	private void DrawGrid(float boardW, float boardH)
	{
		// Internal vertical grid lines only
		for (int c = 1; c < _gridSize.X; c++)
		{
			float x = c * CellSize;

			DrawLine(
				new Vector2(x, 0),
				new Vector2(x, boardH),
				_gridLineColor,
				1f
			);
		}

		// Internal horizontal grid lines only
		for (int r = 1; r < _gridSize.Y; r++)
		{
			float y = r * CellSize;

			DrawLine(
				new Vector2(0, y),
				new Vector2(boardW, y),
				_gridLineColor,
				1f
			);
		}
	}

	private void DrawBoardOutline(float boardW, float boardH)
	{
		float outlineWidth = 4f;

		float exitY = _exitRow * CellSize;
		float exitH = _exitHeightCells * CellSize;

		// Top border
		DrawLine(
			new Vector2(0, 0),
			new Vector2(boardW, 0),
			_borderColor,
			outlineWidth
		);

		// Bottom border
		DrawLine(
			new Vector2(0, boardH),
			new Vector2(boardW, boardH),
			_borderColor,
			outlineWidth
		);

		// Left border
		DrawLine(
			new Vector2(0, 0),
			new Vector2(0, boardH),
			_borderColor,
			outlineWidth
		);

		// Right border, split into two parts to create a hole
		DrawLine(
			new Vector2(boardW, 0),
			new Vector2(boardW, exitY),
			_borderColor,
			outlineWidth
		);

		DrawLine(
			new Vector2(boardW, exitY + exitH),
			new Vector2(boardW, boardH),
			_borderColor,
			outlineWidth
		);
	}
}
