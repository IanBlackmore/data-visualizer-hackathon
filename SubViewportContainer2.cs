using Godot;
using System;

public partial class SubViewportContainer2 : SubViewportContainer
{
	[Export] public bool PinToViewportBottom = true;
	[Export] public int BottomBarHeight = 96;

	private SubViewport _subViewport;

	public override void _Ready()
	{
		ResolveSubViewport();
		FitToViewportBottom();
		ResizeSubViewport();
	}

	public override void _Process(double delta)
	{
		FitToViewportBottom();
		ResizeSubViewport();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
			ResizeSubViewport();
	}

	private void ResolveSubViewport()
	{
		_subViewport = GetNodeOrNull<SubViewport>("SubViewport") ?? GetNodeOrNull<SubViewport>("SubViewport2");

		if (_subViewport != null)
			return;

		foreach (Node child in GetChildren())
		{
			if (child is SubViewport viewport)
			{
				_subViewport = viewport;
				return;
			}
		}
	}

	private void FitToViewportBottom()
	{
		if (!PinToViewportBottom)
			return;

		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		float h = Mathf.Max(1f, BottomBarHeight);
		Vector2 targetPosition = new Vector2(0f, Mathf.Max(0f, viewportSize.Y - h));
		Vector2 targetSize = new Vector2(Mathf.Max(1f, viewportSize.X), h);

		if (Position != targetPosition)
			Position = targetPosition;

		if (Size != targetSize)
			Size = targetSize;
	}

	private void ResizeSubViewport()
	{
		if (_subViewport == null || !IsInstanceValid(_subViewport))
			ResolveSubViewport();

		if (_subViewport == null)
		{
			GD.PrintErr("SubViewportContainer2 could not find a SubViewport child.");
			return;
		}

		Vector2 size = Size;

		if (size.X <= 0 || size.Y <= 0)
			size = GetViewport().GetVisibleRect().Size;

		_subViewport.Size = new Vector2I(
			Math.Max(1, Mathf.RoundToInt(size.X)),
			Math.Max(1, Mathf.RoundToInt(size.Y))
		);
	}
}
