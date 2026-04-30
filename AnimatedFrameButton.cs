using Godot;
using System;

public partial class AnimatedFrameButton : Control
{
	[Signal]
	public delegate void PressedEventHandler();

	[Export] public string SpriteSheetPath = "";
	[Export] public int    TotalFrames     = 3;    // All frames in the sheet, played as one loop
	[Export] public float  AnimationFps    = 10f;  // Always-on animation speed
	[Export] public bool   StretchToFit    = false;

	private AnimatedSprite2D _sprite;
	private Vector2 _frameSize = Vector2.Zero;

	private bool _mouseInside = false;
	private bool _mouseDown   = false;

	private Tween _hoverTween;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;

		MouseEntered += OnMouseEntered;
		MouseExited  += OnMouseExited;
		Resized      += UpdateSpriteLayout;

		LoadSpriteSheet();
	}

	// -------------------------------------------------------------------------
	// Loading — one single looping animation using every frame in the sheet
	// -------------------------------------------------------------------------

	private void LoadSpriteSheet()
	{
		if (string.IsNullOrWhiteSpace(SpriteSheetPath))
		{
			GD.PrintErr("AnimatedFrameButton: no SpriteSheetPath.");
			return;
		}

		if (!ResourceLoader.Exists(SpriteSheetPath))
		{
			GD.PrintErr($"AnimatedFrameButton: sprite not found: {SpriteSheetPath}");
			return;
		}

		Texture2D tex = ResourceLoader.Load<Texture2D>(SpriteSheetPath);
		if (tex == null)
		{
			GD.PrintErr($"AnimatedFrameButton: could not load: {SpriteSheetPath}");
			return;
		}

		int safeTotal  = Math.Max(1, TotalFrames);
		int frameWidth = tex.GetWidth() / safeTotal;
		int frameHeight = tex.GetHeight();

		if (frameWidth <= 0 || frameHeight <= 0)
		{
			GD.PrintErr($"AnimatedFrameButton: invalid frame size for: {SpriteSheetPath}");
			return;
		}

		_frameSize = new Vector2(frameWidth, frameHeight);

		// Lock control to the natural pixel size of one frame (e.g. 300x72)
		CustomMinimumSize = _frameSize;
		Size              = _frameSize;

		var frames = new SpriteFrames();

		if (!frames.HasAnimation("play"))
			frames.AddAnimation("play");

		frames.SetAnimationSpeed("play", AnimationFps);
		frames.SetAnimationLoop("play", true);

		for (int i = 0; i < safeTotal; i++)
		{
			frames.AddFrame("play", new AtlasTexture
			{
				Atlas  = tex,
				Region = new Rect2(i * frameWidth, 0, frameWidth, frameHeight)
			});
		}

		_sprite = new AnimatedSprite2D
		{
			SpriteFrames = frames,
			Animation    = "play",
			Centered     = true
		};

		AddChild(_sprite);
		UpdateSpriteLayout();

		// Start playing immediately and never stop
		_sprite.Play("play");
	}

	// -------------------------------------------------------------------------
	// Layout
	// -------------------------------------------------------------------------

	private void UpdateSpriteLayout()
	{
		if (_sprite == null || _frameSize == Vector2.Zero)
			return;

		_sprite.Position = Size / 2f;

		if (!StretchToFit)
		{
			_sprite.Scale = Vector2.One;
			return;
		}

		if (Size.X <= 0 || Size.Y <= 0)
			return;

		_sprite.Scale = new Vector2(
			Size.X / _frameSize.X,
			Size.Y / _frameSize.Y
		);
	}

	// -------------------------------------------------------------------------
	// Hover glow — warm golden flash that settles into a steady shine
	// Animation keeps playing underneath at all times
	// -------------------------------------------------------------------------

	private void ApplyHoverGlow()
	{
		if (_sprite == null) return;

		_hoverTween?.Kill();
		_hoverTween = CreateTween();

		_hoverTween
			.TweenProperty(_sprite, "modulate", new Color(1.55f, 1.40f, 1.05f, 1f), 0.10f)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);

		_hoverTween
			.TweenProperty(_sprite, "modulate", new Color(1.25f, 1.20f, 1.05f, 1f), 0.20f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}

	private void RemoveHoverGlow()
	{
		_hoverTween?.Kill();
		_hoverTween = null;

		if (_sprite != null)
			_sprite.Modulate = Colors.White;
	}

	// -------------------------------------------------------------------------
	// Mouse events — animation NEVER stops, only modulate changes
	// -------------------------------------------------------------------------

	private void OnMouseEntered()
	{
		_mouseInside = true;
		if (_mouseDown) return; // already in pressed modulate
		ApplyHoverGlow();
	}

	private void OnMouseExited()
	{
		_mouseInside = false;
		RemoveHoverGlow();
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseEvent)
			return;

		if (mouseEvent.ButtonIndex != MouseButton.Left)
			return;

		if (mouseEvent.Pressed)
		{
			_mouseDown = true;
			RemoveHoverGlow();

			// Darken on press for tactile feel
			if (_sprite != null)
				_sprite.Modulate = new Color(0.72f, 0.72f, 0.72f, 1f);

			AcceptEvent();
			return;
		}

		// Released
		_mouseDown = false;

		if (_mouseInside)
		{
			ApplyHoverGlow();
			EmitSignal(SignalName.Pressed);
		}
		else
		{
			RemoveHoverGlow();
		}

		AcceptEvent();
	}
}
