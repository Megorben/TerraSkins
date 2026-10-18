using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace TerraSkins.Common.UI.Elements;

public enum CellKind : byte
{
	Item,
	KeepOwn,
	Hide,
	NoDye,
}

public readonly record struct GridCell(CellKind Kind, int ItemType = 0);

/// <summary> Grid of item appearances (or dyes) to pick from. </summary>
public class UIItemGrid : UIVirtualGrid
{
	/// <summary> Index of each special cell in <see cref="UIStyle.CellIcons"/>. </summary>
	private static int IconIndex(CellKind kind) => kind switch {
		CellKind.KeepOwn => 0,
		CellKind.Hide => 1,
		CellKind.NoDye => 2,
		_ => 0,
	};

	private readonly List<GridCell> cells = new();

	public UIItemGrid(UIScrollbar scrollbar) : base(scrollbar)
	{
	}

	public Func<GridCell, bool> IsSelected { get; set; } = _ => false;

	public Func<GridCell, string> SpecialTooltip { get; set; } = _ => string.Empty;

	public event Action<GridCell, bool> OnPick;

	protected override int Count => cells.Count;

	protected override Point CellSize => new(42, 42);

	public void SetCells(IEnumerable<GridCell> newCells)
	{
		cells.Clear();
		cells.AddRange(newCells);
		ResetScroll();
	}

	protected override void ClickCell(int index, bool right)
	{
		SoundEngine.PlaySound(right ? SoundID.MenuClose : SoundID.MenuTick);
		OnPick?.Invoke(cells[index], right);
	}

	protected override void DrawCell(SpriteBatch spriteBatch, int index, Rectangle area, bool hovered)
	{
		GridCell cell = cells[index];
		bool selected = IsSelected(cell);
		if (selected) {
			UIStyle.DrawPanel(spriteBatch, area, UIStyle.Gold);
			area.Inflate(-2, -2);
		}
		UIStyle.DrawPanel(spriteBatch, area, selected ? UIStyle.PanelActive : hovered ? UIStyle.PanelHover : UIStyle.Panel);

		if (cell.Kind == CellKind.Item) {
			Main.instance.LoadItem(cell.ItemType);
			Item item = ContentSamples.ItemsByType[cell.ItemType];
			float scale = hovered ? 1.08f : 1f;
			ItemSlot.DrawItemIcon(item, ItemSlot.Context.InventoryItem, spriteBatch, area.Center.ToVector2(), scale, 30f, Color.White);
			return;
		}

		Texture2D icons = UIStyle.CellIcons;
		const int size = 32;
		var source = new Rectangle(IconIndex(cell.Kind) * size, 0, size, size);
		spriteBatch.Draw(icons, area.Center.ToVector2(), source, hovered ? Color.White : new Color(210, 205, 235), 0f, new Vector2(size * 0.5f), 1f, SpriteEffects.None, 0f);
	}

	protected override void HoverCell(int index)
	{
		GridCell cell = cells[index];
		if (cell.Kind != CellKind.Item) {
			Main.hoverItemName = SpecialTooltip(cell);
			return;
		}

		Main.HoverItem = ContentSamples.ItemsByType[cell.ItemType].Clone();
		Main.hoverItemName = Main.HoverItem.Name;
	}
}
