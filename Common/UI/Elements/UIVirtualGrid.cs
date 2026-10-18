using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace TerraSkins.Common.UI.Elements;

/// <summary>
/// A scrollable grid that only draws the visible cells, so it stays fast with thousands of entries.
/// Cells are drawn by a child element so that they are clipped to the grid area.
/// </summary>
public abstract class UIVirtualGrid : UIElement
{
	private readonly UIScrollbar scrollbar;

	protected UIVirtualGrid(UIScrollbar scrollbar)
	{
		this.scrollbar = scrollbar;
		OverflowHidden = true;
		var canvas = new Canvas(this);
		canvas.Width.Set(0f, 1f);
		canvas.Height.Set(0f, 1f);
		Append(canvas);
	}

	protected abstract int Count { get; }

	protected abstract Point CellSize { get; }

	protected virtual int Gap => 4;

	public void ResetScroll() => scrollbar.ViewPosition = 0f;

	private int Columns(float width) => Math.Max(1, (int)((width + Gap) / (CellSize.X + Gap)));

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		CalculatedStyle inner = GetInnerDimensions();
		int columns = Columns(inner.Width);
		int rows = (Count + columns - 1) / columns;
		float total = rows * (CellSize.Y + Gap);
		scrollbar.SetView(inner.Height, Math.Max(total, inner.Height));

		if (IsMouseHovering)
			PlayerInput.LockVanillaMouseScroll("TerraSkins/Wardrobe");
	}

	public override void ScrollWheel(UIScrollWheelEvent evt)
	{
		base.ScrollWheel(evt);
		scrollbar.ViewPosition -= evt.ScrollWheelValue;
	}

	private int IndexAt(Vector2 point)
	{
		CalculatedStyle inner = GetInnerDimensions();
		if (!inner.ToRectangle().Contains(point.ToPoint()))
			return -1;

		int columns = Columns(inner.Width);
		float x = point.X - inner.X;
		float y = point.Y - inner.Y + scrollbar.ViewPosition;
		int column = (int)(x / (CellSize.X + Gap));
		int row = (int)(y / (CellSize.Y + Gap));
		if (column >= columns || x - column * (CellSize.X + Gap) > CellSize.X || y - row * (CellSize.Y + Gap) > CellSize.Y)
			return -1;

		int index = row * columns + column;
		return index < Count ? index : -1;
	}

	public override void LeftClick(UIMouseEvent evt)
	{
		base.LeftClick(evt);
		int index = IndexAt(evt.MousePosition);
		if (index >= 0)
			ClickCell(index, right: false);
	}

	public override void RightClick(UIMouseEvent evt)
	{
		base.RightClick(evt);
		int index = IndexAt(evt.MousePosition);
		if (index >= 0)
			ClickCell(index, right: true);
	}

	protected abstract void ClickCell(int index, bool right);

	protected abstract void DrawCell(SpriteBatch spriteBatch, int index, Rectangle area, bool hovered);

	/// <summary> Called while the mouse is over a cell, typically to show a tooltip. </summary>
	protected virtual void HoverCell(int index)
	{
	}

	private void DrawCells(SpriteBatch spriteBatch)
	{
		CalculatedStyle inner = GetInnerDimensions();
		int columns = Columns(inner.Width);
		int rowHeight = CellSize.Y + Gap;
		float offset = scrollbar.ViewPosition;
		int firstRow = (int)(offset / rowHeight);
		int lastRow = (int)((offset + inner.Height) / rowHeight);
		int hovered = IsMouseHovering ? IndexAt(Main.MouseScreen) : -1;

		for (int row = firstRow; row <= lastRow; row++) {
			for (int column = 0; column < columns; column++) {
				int index = row * columns + column;
				if (index >= Count)
					break;

				var area = new Rectangle((int)(inner.X + column * (CellSize.X + Gap)), (int)(inner.Y + row * rowHeight - offset), CellSize.X, CellSize.Y);
				DrawCell(spriteBatch, index, area, index == hovered);
			}
		}

		if (hovered >= 0)
			HoverCell(hovered);
	}

	private sealed class Canvas(UIVirtualGrid grid) : UIElement
	{
		protected override void DrawSelf(SpriteBatch spriteBatch) => grid.DrawCells(spriteBatch);

		public override bool ContainsPoint(Vector2 point) => false;
	}
}
