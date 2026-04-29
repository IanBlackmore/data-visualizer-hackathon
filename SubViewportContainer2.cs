using Godot;
using System;

public partial class SubViewportContainer2 : SubViewportContainer
{
	public override void _Notification(int what)
	{
		if (what == NotificationResized)
		{
			var viewport = GetNode<SubViewport>("SubViewport");
			viewport.Size = (Vector2I)GetViewport().GetVisibleRect().Size;
		}
	}
}
