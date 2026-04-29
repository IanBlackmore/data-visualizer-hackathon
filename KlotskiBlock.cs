using Godot;
using System;

public partial class KlotskiBlock : Panel
{
	public string ID { get; private set; }
	public Vector2I GridPos { get; private set; }
	public Vector2I BlockSize { get; private set; }

	public Board Board { get; set; }

	private int _cellSize;
	private Color _baseColor;
	private Tween _activeTween;

	public void Setup(string id, Vector2I pos, Vector2I size, int cellSize, Color color)
	{
		ID = id;
		GridPos = pos;
		BlockSize = size;

		_cellSize = cellSize;
		_baseColor = color;

		Position = new Vector2(GridPos.X * _cellSize, GridPos.Y * _cellSize);
		Size = new Vector2(BlockSize.X * _cellSize, BlockSize.Y * _cellSize);
		CustomMinimumSize = Size;

		MouseFilter = MouseFilterEnum.Stop;

		SetHighlight(false);
		QueueRedraw();
	}

	public void SlideTo(Vector2I newGridPos)
	{
		GridPos = newGridPos;

		Vector2 targetPosition = new Vector2(
			GridPos.X * _cellSize,
			GridPos.Y * _cellSize
		);

		if (_activeTween != null)
		{
			_activeTween.Kill();
		}

		_activeTween = CreateTween();
		_activeTween.SetTrans(Tween.TransitionType.Quad);
		_activeTween.SetEase(Tween.EaseType.Out);
		_activeTween.TweenProperty(this, "position", targetPosition, 0.12);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
			{
				Board?.SelectBlock(this);
				AcceptEvent();
			}
		}
	}

	public void SetHighlight(bool active)
	{
		var style = new StyleBoxFlat();

		style.BgColor = _baseColor;
		style.BorderColor = active
			? Colors.White
			: _baseColor.Darkened(0.45f);

		style.SetBorderWidthAll(active ? 4 : 2);
		style.SetCornerRadiusAll(8);

		AddThemeStyleboxOverride("panel", style);

		QueueRedraw();
	}

	public override void _Draw()
	{
		float w = Size.X;
		float h = Size.Y;
		float m = 4f;

		Rect2 rect = new Rect2(
			new Vector2(m, m),
			new Vector2(w - m * 2f, h - m * 2f)
		);

		// Top highlight
		DrawLine(
			new Vector2(rect.Position.X + 6f, rect.Position.Y + 3f),
			new Vector2(rect.End.X - 6f, rect.Position.Y + 3f),
			_baseColor.Lightened(0.45f),
			1.5f
		);

		// Left highlight
		DrawLine(
			new Vector2(rect.Position.X + 3f, rect.Position.Y + 6f),
			new Vector2(rect.Position.X + 3f, rect.End.Y - 6f),
			_baseColor.Lightened(0.20f),
			1.0f
		);

		// Bottom shadow
		DrawLine(
			new Vector2(rect.Position.X + 6f, rect.End.Y - 3f),
			new Vector2(rect.End.X - 4f, rect.End.Y - 3f),
			_baseColor.Darkened(0.35f),
			1.5f
		);

		// Right shadow
		DrawLine(
			new Vector2(rect.End.X - 3f, rect.Position.Y + 6f),
			new Vector2(rect.End.X - 3f, rect.End.Y - 4f),
			_baseColor.Darkened(0.35f),
			1.0f
		);
	}
}
