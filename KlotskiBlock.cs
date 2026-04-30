using Godot;
using System;

public partial class KlotskiBlock : Panel
{
	public string ID { get; private set; }
	public Vector2I GridPos { get; private set; }
	public Vector2I BlockSize { get; private set; }
	public Board Board { get; set; }
	public bool IsSliding { get; private set; } = false;

	private int _cellSize;
	private Color _baseColor;
	private Tween _activeTween;
	private AnimatedSprite2D _sprite;
	private bool _isHero;
	private bool _spriteLoaded;

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
		ID           = id;
		GridPos      = pos;
		BlockSize    = size;
		_cellSize    = cellSize;
		_baseColor   = color;
		_boardOffset = boardOffset;
		_isHero      = id == "1";
		IsSliding    = false;

		Position = GridToPixel(GridPos);

		Size = new Vector2(
			BlockSize.X * _cellSize,
			BlockSize.Y * _cellSize
		);

		CustomMinimumSize = Size;
		MouseFilter = MouseFilterEnum.Stop;

		ApplyTransparentStyle();
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

	private void ApplyTransparentStyle()
	{
		var style = new StyleBoxFlat();
		style.BgColor     = Colors.Transparent;
		style.BorderColor = Colors.Transparent;
		style.SetBorderWidthAll(0);
		AddThemeStyleboxOverride("panel", style);
	}

	private void ApplyFallbackStyle()
	{
		var style = new StyleBoxFlat();
		style.BgColor     = _baseColor;
		style.BorderColor = _baseColor.Darkened(0.45f);
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(8);
		AddThemeStyleboxOverride("panel", style);
	}

	private void LoadSprite(Vector2I size, bool isWin)
	{
		_spriteLoaded = false;

		string colorVariant;

		if (isWin)
		{
			colorVariant = "winning";
		}
		else if (_isHero)
		{
			colorVariant = "green";
		}
		else
		{
			int index = (int)GD.RandRange(0, NormalColors.Length - 1);
			colorVariant = NormalColors[index];
		}

		bool isHorizontal = size.X > size.Y;

		Vector2I loadSize = isHorizontal
			? new Vector2I(size.Y, size.X)
			: size;

		string pngName = $"res://Art/block_{loadSize.X}x{loadSize.Y}_{colorVariant}.png";

		if (!ResourceLoader.Exists(pngName))
		{
			GD.PrintErr($"Sprite not found: {pngName}. Using fallback block drawing.");
			ApplyFallbackStyle();
			QueueRedraw();
			return;
		}

		if (_sprite != null && IsInstanceValid(_sprite))
		{
			_sprite.QueueFree();
			_sprite = null;
		}

		Texture2D tex = ResourceLoader.Load<Texture2D>(pngName);

		if (tex == null)
		{
			GD.PrintErr($"Could not load sprite: {pngName}. Using fallback block drawing.");
			ApplyFallbackStyle();
			QueueRedraw();
			return;
		}

		_sprite = new AnimatedSprite2D();
		AddChild(_sprite);

		int frameCount = isWin ? 6 : 3;
		int frameWidth = tex.GetWidth() / frameCount;

		var frames = new SpriteFrames();

		if (!frames.HasAnimation("default"))
			frames.AddAnimation("default");

		for (int i = 0; i < frameCount; i++)
		{
			var frame = new AtlasTexture
			{
				Atlas  = tex,
				Region = new Rect2(
					i * frameWidth,
					0,
					frameWidth,
					tex.GetHeight()
				)
			};
			frames.AddFrame("default", frame);
		}

		frames.SetAnimationSpeed("default", isWin ? 10 : 8);
		frames.SetAnimationLoop("default", true);

		_sprite.SpriteFrames    = frames;
		_sprite.Animation       = "default";
		_sprite.Position        = Size / 2f;
		_sprite.RotationDegrees = isHorizontal ? 90 : 0;

		_sprite.Play("default");

		_spriteLoaded = true;
		ApplyTransparentStyle();
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

		IsSliding = true;
		_activeTween = CreateTween();
		_activeTween.SetTrans(Tween.TransitionType.Quad);
		_activeTween.SetEase(Tween.EaseType.Out);
		_activeTween.TweenProperty(this, "position", targetPosition, 0.12);
		_activeTween.TweenCallback(Callable.From(OnSlideFinished));
	}

	private void OnSlideFinished()
	{
		IsSliding = false;
		_activeTween = null;
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
		if (_spriteLoaded)
		{
			ApplyTransparentStyle();
			if (_sprite != null)
				_sprite.Modulate = active ? new Color(1.15f, 1.15f, 1.15f, 1f) : Colors.White;
		}
		else
		{
			ApplyFallbackStyle();
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_spriteLoaded)
			return;

		float w = Size.X;
		float h = Size.Y;
		float m = 4f;

		Rect2 rect = new Rect2(
			new Vector2(m, m),
			new Vector2(w - m * 2f, h - m * 2f)
		);

		DrawLine(
			new Vector2(rect.Position.X + 6f, rect.Position.Y + 3f),
			new Vector2(rect.End.X - 6f,      rect.Position.Y + 3f),
			_baseColor.Lightened(0.45f), 1.5f
		);

		DrawLine(
			new Vector2(rect.Position.X + 3f, rect.Position.Y + 6f),
			new Vector2(rect.Position.X + 3f, rect.End.Y - 6f),
			_baseColor.Lightened(0.20f), 1.0f
		);

		DrawLine(
			new Vector2(rect.Position.X + 6f, rect.End.Y - 3f),
			new Vector2(rect.End.X - 4f,      rect.End.Y - 3f),
			_baseColor.Darkened(0.35f), 1.5f
		);

		DrawLine(
			new Vector2(rect.End.X - 3f, rect.Position.Y + 6f),
			new Vector2(rect.End.X - 3f, rect.End.Y - 4f),
			_baseColor.Darkened(0.35f), 1.0f
		);
	}
}
