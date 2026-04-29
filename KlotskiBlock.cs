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
	private AnimatedSprite2D _sprite;
	private bool _isHero;

	private Vector2 _boardOffset = Vector2.Zero;

	private static readonly string[] NormalColors =
	{
		"purple",
		"cyan",
		"yellow"
	};

	public void Setup(string id, Vector2I pos, Vector2I size, int cellSize, Color color)
	{
		Setup(id, pos, size, cellSize, color, Vector2.Zero);
	}

	public void Setup(
		string id,
		Vector2I pos,
		Vector2I size,
		int cellSize,
		Color color,
		Vector2 boardOffset
	)
	{
		ID = id;
		GridPos = pos;
		BlockSize = size;
		_cellSize = cellSize;
		_baseColor = color;
		_boardOffset = boardOffset;
		_isHero = id == "1";

		Position = GridToPixel(GridPos);

		Size = new Vector2(
			BlockSize.X * _cellSize,
			BlockSize.Y * _cellSize
		);

		CustomMinimumSize = Size;
		MouseFilter = MouseFilterEnum.Stop;

		var style = new StyleBoxFlat();
		style.BgColor = Colors.Transparent;
		style.SetBorderWidthAll(0);
		style.BorderColor = Colors.Transparent;
		AddThemeStyleboxOverride("panel", style);

		LoadSprite(size, isWin: false);

		QueueRedraw();
	}

	private Vector2 GridToPixel(Vector2I gridPos)
	{
		return _boardOffset + new Vector2(
			gridPos.X * _cellSize,
			gridPos.Y * _cellSize
		);
	}

	private void LoadSprite(Vector2I size, bool isWin)
	{
		string colorVariant = isWin
			? "winning"
			: (_isHero ? "green" : NormalColors[(int)GD.RandRange(0, NormalColors.Length - 1)]);

		bool isHorizontal = size.X > size.Y;

		Vector2I loadSize = isHorizontal
			? new Vector2I(size.Y, size.X)
			: size;

		string pngName = $"res://Art/block_{loadSize.X}x{loadSize.Y}_{colorVariant}.png";

		if (!ResourceLoader.Exists(pngName))
		{
			GD.PrintErr($"Sprite not found: {pngName}");
			return;
		}

		if (_sprite != null && IsInstanceValid(_sprite))
		{
			_sprite.QueueFree();
			_sprite = null;
		}

		_sprite = new AnimatedSprite2D();
		AddChild(_sprite);

		Texture2D tex = ResourceLoader.Load<Texture2D>(pngName);

		int frameCount = isWin ? 6 : 3;
		int frameWidth = tex.GetWidth() / frameCount;

		var frames = new SpriteFrames();

		if (!frames.HasAnimation("default"))
			frames.AddAnimation("default");

		for (int i = 0; i < frameCount; i++)
		{
			var frame = new AtlasTexture();
			frame.Atlas = tex;
			frame.Region = new Rect2(
				i * frameWidth,
				0,
				frameWidth,
				tex.GetHeight()
			);

			frames.AddFrame("default", frame);
		}

		frames.SetAnimationSpeed("default", isWin ? 10 : 8);

		_sprite.SpriteFrames = frames;
		_sprite.Animation = "default";

		_sprite.Position = Size / 2f;
		_sprite.RotationDegrees = isHorizontal ? 90 : 0;

		_sprite.Play("default");
	}

	public void PlayWinAnimation()
	{
		LoadSprite(BlockSize, isWin: true);

		MouseFilter = MouseFilterEnum.Ignore;
	}

	public void SlideTo(Vector2I newGridPos)
	{
		GridPos = newGridPos;

		Vector2 targetPosition = GridToPixel(GridPos);

		if (_activeTween != null)
			_activeTween.Kill();

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
		// No white outline anymore.
		// Board.cs can still call this safely, but nothing visual appears.
		var style = new StyleBoxFlat();
		style.BgColor = Colors.Transparent;
		style.BorderColor = Colors.Transparent;
		style.SetBorderWidthAll(0);

		AddThemeStyleboxOverride("panel", style);

		QueueRedraw();
	}

	public override void _Draw()
	{
		// Sprite handles visuals.
	}
}
