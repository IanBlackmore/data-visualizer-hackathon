using Godot;
using System;

public partial class SubViewportContainer2 : SubViewportContainer
{
	private SubViewport _subViewport;

	public override void _Ready()
	{
		ResolveSubViewport();
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
