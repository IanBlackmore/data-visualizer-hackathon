using Godot;

public partial class KlotskiBlock : Panel
{
	public string ID;
	public Vector2I GridPos;
	public Vector2I BlockSize;
	public int CellSize = 100;

	// This now accepts 5 arguments: string, Vector2I, Vector2I, int, Color
	public void Setup(string id, Vector2I pos, Vector2I size, int cellSize, Color color)
	{
		ID = id;
		GridPos = pos;
		BlockSize = size;
		CellSize = cellSize;

		// Apply the color to the Panel
		var style = new StyleBoxFlat();
		style.BgColor = color;
		style.SetBorderWidthAll(2);
		style.BorderColor = new Color(0, 0, 0, 0.3f); // Subtle dark border
		AddThemeStyleboxOverride("panel", style);
		
		this.SelfModulate = color; 
		this.Show(); // Ensure it isn't hidden

		UpdateVisuals();
	}

	public void UpdateVisuals()
	{
		Vector2 pixelSize = (Vector2)BlockSize * CellSize;
		
		// Set EVERY size property Godot has
		this.Size = pixelSize;
		this.CustomMinimumSize = pixelSize;
		this.Position = (Vector2)GridPos * CellSize;
		
		// Force the node to be at the front of the draw calls
		this.ZIndex = 10; 
		
		GD.Print($"Block {ID} at {Position} with size {Size}"); 
	}

	public void SlideTo(Vector2I newGridPos)
	{
		GridPos = newGridPos;
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(this, "position", (Vector2)GridPos * CellSize, 0.15f);
	}

	// This allows the board to know which block you want to move
	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			// Find the Board script in the hierarchy and tell it this block is selected
			var board = GetParent<Board>();
			board.SelectBlock(this);
		}
	}
}
