using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using TerraSkins.Common.UI.Elements;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.UI;

/// <summary> The wardrobe window. </summary>
public class WardrobeState : UIState
{
	private const int OutfitsTab = -1;
	private const float LeftColumnWidth = 196f;
	private const float TitleHeight = 30f;

	private UIElement window;
	private UIPlayerPreview preview;
	private UIElement tabRow;
	private UIElement chipRow;
	private UISearchField search;
	private UIItemGrid itemGrid;
	private UIOutfitGrid outfitGrid;
	private UIScrollbar itemScrollbar;
	private UIScrollbar outfitScrollbar;
	private UIElement footer;

	/// <summary> Selected tab: <see cref="OutfitsTab"/> or a <see cref="WardrobeSlot"/>. </summary>
	private int tab = OutfitsTab;
	private string sourceFilter;
	private bool dyeMode;
	private bool dragging;
	private Vector2 dragOffset;

	private static WardrobePlayer Wardrobe => Main.LocalPlayer.GetModPlayer<WardrobePlayer>();

	private WardrobeSlot Slot => (WardrobeSlot)Math.Max(tab, 0);

	public override void OnInitialize()
	{
		window = new Window(this);
		window.SetPadding(10f);
		window.Width.Set(900f, 0f);
		window.Height.Set(560f, 0f);
		window.HAlign = 0.5f;
		window.VAlign = 0.5f;
		window.OnLeftMouseDown += StartDrag;
		window.OnLeftMouseUp += (_, _) => dragging = false;
		Append(window);

		var close = new UIWardrobeButton(() => "X", tooltip: () => Language.GetTextValue("Mods.TerraSkins.UI.Close"));
		close.Width.Set(26f, 0f);
		close.Height.Set(24f, 0f);
		close.HAlign = 1f;
		close.OnLeftClick += (_, _) => WardrobeUISystem.Close();
		window.Append(close);

		BuildLeftColumn();
		BuildRightColumn();
		SelectTab(OutfitsTab);
	}

	private void BuildLeftColumn()
	{
		preview = new UIPlayerPreview();
		preview.Top.Set(TitleHeight + 4f, 0f);
		preview.Width.Set(LeftColumnWidth, 0f);
		preview.Height.Set(-TitleHeight - 4f - 3 * 30f - 6f, 1f);
		window.Append(preview);

		AddLeftButton(0, () => Language.GetTextValue(Wardrobe.Enabled ? "Mods.TerraSkins.UI.WardrobeOn" : "Mods.TerraSkins.UI.WardrobeOff"), () => Wardrobe.Enabled,
			() => Language.GetTextValue("Mods.TerraSkins.UI.WardrobeToggleTooltip"), () => Wardrobe.Enabled = !Wardrobe.Enabled);
		AddLeftButton(1, () => Language.GetTextValue("Mods.TerraSkins.UI.Randomize"), null,
			() => Language.GetTextValue("Mods.TerraSkins.UI.RandomizeTooltip"), Randomize);
		AddLeftButton(2, () => Language.GetTextValue("Mods.TerraSkins.UI.ClearAll"), null,
			() => Language.GetTextValue("Mods.TerraSkins.UI.ClearAllTooltip"), () => Wardrobe.Loadout.Clear());
	}

	private void AddLeftButton(int row, Func<string> text, Func<bool> active, Func<string> tooltip, Action action)
	{
		var button = new UIWardrobeButton(text, active, tooltip: tooltip);
		button.Width.Set(LeftColumnWidth, 0f);
		button.Height.Set(26f, 0f);
		button.Top.Set(-(3 - row) * 30f + 4f, 1f);
		button.OnLeftClick += (_, _) => action();
		window.Append(button);
	}

	private void BuildRightColumn()
	{
		float left = LeftColumnWidth + 10f;

		tabRow = new UIElement();
		tabRow.Left.Set(left, 0f);
		tabRow.Top.Set(TitleHeight + 4f, 0f);
		tabRow.Width.Set(-left, 1f);
		tabRow.Height.Set(30f, 0f);
		window.Append(tabRow);

		AddTab(OutfitsTab, 0, () => Language.GetTextValue("Mods.TerraSkins.UI.Outfits"), () => false);
		foreach (WardrobeSlot slot in WardrobeSlots.All) {
			WardrobeSlot captured = slot;
			AddTab((int)slot, (int)slot + 1, () => SlotTooltip(captured), () => Wardrobe.Loadout[captured] != WardrobeLoadout.None);
		}

		chipRow = new UIElement();
		chipRow.Left.Set(left, 0f);
		chipRow.Top.Set(TitleHeight + 40f, 0f);
		chipRow.Width.Set(-left - 196f, 1f);
		chipRow.Height.Set(24f, 0f);
		window.Append(chipRow);

		search = new UISearchField(() => Language.GetTextValue("Mods.TerraSkins.UI.SearchHint"));
		search.Top.Set(TitleHeight + 40f, 0f);
		search.Width.Set(186f, 0f);
		search.HAlign = 1f;
		search.OnTextChanged += _ => RebuildCells();
		window.Append(search);

		float gridTop = TitleHeight + 72f;
		const float footerHeight = 34f;

		itemScrollbar = new UIScrollbar();
		itemScrollbar.Top.Set(gridTop + 4f, 0f);
		itemScrollbar.Height.Set(-gridTop - footerHeight - 12f, 1f);
		itemScrollbar.HAlign = 1f;
		itemGrid = new UIItemGrid(itemScrollbar);
		itemGrid.Left.Set(left, 0f);
		itemGrid.Top.Set(gridTop, 0f);
		itemGrid.Width.Set(-left - 26f, 1f);
		itemGrid.Height.Set(-gridTop - footerHeight - 4f, 1f);
		itemGrid.IsSelected = IsCellSelected;
		itemGrid.SpecialTooltip = SpecialCellTooltip;
		itemGrid.OnPick += PickCell;

		outfitScrollbar = new UIScrollbar();
		outfitScrollbar.Top.Set(gridTop + 4f, 0f);
		outfitScrollbar.Height.Set(-gridTop - 12f, 1f);
		outfitScrollbar.HAlign = 1f;
		outfitGrid = new UIOutfitGrid(outfitScrollbar);
		outfitGrid.Left.Set(left, 0f);
		outfitGrid.Top.Set(gridTop, 0f);
		outfitGrid.Width.Set(-left - 26f, 1f);
		outfitGrid.Height.Set(-gridTop - 4f, 1f);
		outfitGrid.OnPick += PickOutfit;

		footer = new UIElement();
		footer.Left.Set(left, 0f);
		footer.Top.Set(-footerHeight + 4f, 1f);
		footer.Width.Set(-left, 1f);
		footer.Height.Set(footerHeight - 4f, 0f);

		var label = new UIFooterLabel(FooterText);
		label.Width.Set(-250f, 1f);
		label.Height.Set(0f, 1f);
		footer.Append(label);

		var dyeButton = new UIWardrobeButton(() => Language.GetTextValue(dyeMode ? "Mods.TerraSkins.UI.BackToLooks" : "Mods.TerraSkins.UI.Dye"), () => dyeMode,
			() => dyeMode || WardrobeSlots.AcceptsDye(Slot) && Wardrobe.Loadout[Slot] > 0, () => Language.GetTextValue("Mods.TerraSkins.UI.DyeTooltip"));
		dyeButton.Width.Set(120f, 0f);
		dyeButton.Height.Set(26f, 0f);
		dyeButton.HAlign = 1f;
		dyeButton.OnLeftClick += (_, _) => {
			dyeMode = !dyeMode;
			RebuildCells();
		};
		footer.Append(dyeButton);

		var resetButton = new UIWardrobeButton(() => Language.GetTextValue("Mods.TerraSkins.UI.ResetSlot"), tooltip: () => Language.GetTextValue("Mods.TerraSkins.UI.ResetSlotTooltip"));
		resetButton.Width.Set(120f, 0f);
		resetButton.Height.Set(26f, 0f);
		resetButton.Left.Set(-246f, 1f);
		resetButton.OnLeftClick += (_, _) => {
			Wardrobe.Loadout[Slot] = WardrobeLoadout.None;
			Wardrobe.Loadout.SetDye(Slot, 0);
		};
		footer.Append(resetButton);
	}

	private void AddTab(int tabValue, int iconIndex, Func<string> tooltip, Func<bool> marked)
	{
		var button = new UISlotTab(iconIndex, tooltip, () => tab == tabValue, marked);
		button.Left.Set(iconIndex * 29f, 0f);
		button.OnLeftClick += (_, _) => SelectTab(tabValue);
		tabRow.Append(button);
	}

	private static string SlotName(WardrobeSlot slot) => Language.GetTextValue($"Mods.TerraSkins.Slots.{slot}");

	private static string SlotTooltip(WardrobeSlot slot)
	{
		string name = SlotName(slot);
		int item = Main.LocalPlayer.GetModPlayer<WardrobePlayer>().Loadout[slot];
		return item switch {
			WardrobeLoadout.None => name,
			WardrobeLoadout.Hidden => $"{name}: {Language.GetTextValue("Mods.TerraSkins.UI.Hidden")}",
			_ => $"{name}: {Lang.GetItemNameValue(item)}",
		};
	}

	private void SelectTab(int newTab)
	{
		tab = newTab;
		dyeMode = false;
		sourceFilter = null;
		search.Unfocus();

		window.RemoveChild(itemGrid);
		window.RemoveChild(itemScrollbar);
		window.RemoveChild(outfitGrid);
		window.RemoveChild(outfitScrollbar);
		window.RemoveChild(footer);

		if (tab == OutfitsTab) {
			window.Append(outfitGrid);
			window.Append(outfitScrollbar);
			outfitGrid.Rebuild();
			outfitGrid.ResetScroll();
		}
		else {
			window.Append(itemGrid);
			window.Append(itemScrollbar);
			window.Append(footer);
		}

		RebuildChips();
		RebuildCells();
		Recalculate();
	}

	private void RebuildChips()
	{
		chipRow.RemoveAllChildren();
		if (tab == OutfitsTab)
			return;

		float x = 0f;
		void AddChip(string id, Func<string> text)
		{
			var chip = new UIChip(text, () => sourceFilter == id);
			chip.FitWidth();
			chip.Left.Set(x, 0f);
			chip.OnLeftClick += (_, _) => {
				sourceFilter = id;
				RebuildCells();
			};
			x += chip.Width.Pixels + 4f;
			chipRow.Append(chip);
		}

		AddChip(null, () => Language.GetTextValue("Mods.TerraSkins.UI.AllSources"));
		var present = WardrobeCatalog.GetEntries(Slot).Select(e => e.Source).ToHashSet();
		foreach (WardrobeSource source in WardrobeCatalog.Sources) {
			if (present.Contains(source))
				AddChip(source.Id, () => source.DisplayName);
		}
		chipRow.Recalculate();
	}

	private void RebuildCells()
	{
		if (tab == OutfitsTab) {
			outfitGrid.Rebuild();
			return;
		}

		string query = search.Text.Trim().ToLowerInvariant();
		var cells = new List<GridCell>();
		if (dyeMode) {
			cells.Add(new GridCell(CellKind.NoDye));
			foreach (int dye in WardrobeCatalog.Dyes) {
				if (query.Length == 0 || Lang.GetItemNameValue(dye).ToLowerInvariant().Contains(query))
					cells.Add(new GridCell(CellKind.Item, dye));
			}
		}
		else {
			cells.Add(new GridCell(CellKind.KeepOwn));
			cells.Add(new GridCell(CellKind.Hide));
			foreach (WardrobeEntry entry in WardrobeCatalog.GetEntries(Slot)) {
				if (sourceFilter != null && entry.Source.Id != sourceFilter)
					continue;
				if (query.Length > 0 && !entry.SearchName.Contains(query))
					continue;
				cells.Add(new GridCell(CellKind.Item, entry.ItemType));
			}
		}
		itemGrid.SetCells(cells);
	}

	private bool IsCellSelected(GridCell cell)
	{
		WardrobeLoadout loadout = Wardrobe.Loadout;
		return cell.Kind switch {
			CellKind.KeepOwn => loadout[Slot] == WardrobeLoadout.None,
			CellKind.Hide => loadout[Slot] == WardrobeLoadout.Hidden,
			CellKind.NoDye => loadout.GetDye(Slot) == 0,
			_ => dyeMode ? loadout.GetDye(Slot) == cell.ItemType : loadout[Slot] == cell.ItemType,
		};
	}

	private string SpecialCellTooltip(GridCell cell) => cell.Kind switch {
		CellKind.KeepOwn => Language.GetTextValue("Mods.TerraSkins.UI.KeepOwnTooltip"),
		CellKind.Hide => Language.GetTextValue("Mods.TerraSkins.UI.HideTooltip"),
		CellKind.NoDye => Language.GetTextValue("Mods.TerraSkins.UI.NoDyeTooltip"),
		_ => string.Empty,
	};

	private void PickCell(GridCell cell, bool right)
	{
		WardrobeLoadout loadout = Wardrobe.Loadout;
		if (right) {
			// Right click always means "back to my own equipment" for this slot.
			loadout[Slot] = WardrobeLoadout.None;
			loadout.SetDye(Slot, 0);
			return;
		}

		switch (cell.Kind) {
			case CellKind.KeepOwn:
				loadout[Slot] = WardrobeLoadout.None;
				loadout.SetDye(Slot, 0);
				break;
			case CellKind.Hide:
				loadout[Slot] = WardrobeLoadout.Hidden;
				loadout.SetDye(Slot, 0);
				break;
			case CellKind.NoDye:
				loadout.SetDye(Slot, 0);
				break;
			default:
				if (dyeMode)
					loadout.SetDye(Slot, cell.ItemType);
				else
					loadout[Slot] = cell.ItemType;
				break;
		}
	}

	private void PickOutfit(UIOutfitGrid.Card card, bool right)
	{
		WardrobePlayer wardrobe = Wardrobe;
		switch (card.Kind) {
			case UIOutfitGrid.CardKind.SaveCurrent:
				if (right || wardrobe.Outfits.Count >= WardrobePlayer.MaxOutfits)
					return;

				// The search box doubles as the name field.
				string name = search.Text.Trim();
				if (name.Length == 0)
					name = Language.GetTextValue("Mods.TerraSkins.UI.DefaultOutfitName", wardrobe.Outfits.Count + 1);
				wardrobe.Outfits.Add(new WardrobeOutfit(name, wardrobe.Loadout.Clone()));
				search.SetText(string.Empty);
				SoundEngine.PlaySound(SoundID.Grab);
				break;
			case UIOutfitGrid.CardKind.Saved:
				if (right)
					wardrobe.Outfits.RemoveAt(card.Index);
				else
					wardrobe.SetLoadout(wardrobe.Outfits[card.Index].Loadout);
				break;
			case UIOutfitGrid.CardKind.Preset:
				if (!right)
					wardrobe.SetLoadout(WardrobePresets.All[card.Index].Loadout);
				break;
		}
		outfitGrid.Rebuild();
	}

	private string FooterText()
	{
		WardrobeLoadout loadout = Wardrobe.Loadout;
		string current = loadout[Slot] switch {
			WardrobeLoadout.None => Language.GetTextValue("Mods.TerraSkins.UI.KeepOwn"),
			WardrobeLoadout.Hidden => Language.GetTextValue("Mods.TerraSkins.UI.Hidden"),
			int type => Lang.GetItemNameValue(type),
		};
		int dye = loadout.GetDye(Slot);
		if (dye > 0)
			current += $" + {Lang.GetItemNameValue(dye)}";
		return $"{SlotName(Slot)}: {current}";
	}

	/// <summary> Throws together a random full look: a random set or random armor, plus a few random effects. </summary>
	private static void Randomize()
	{
		var rand = Main.rand;
		var loadout = new WardrobeLoadout();
		foreach (WardrobeSlot slot in new[] { WardrobeSlot.Head, WardrobeSlot.Body, WardrobeSlot.Legs }) {
			var entries = WardrobeCatalog.GetEntries(slot);
			if (entries.Count > 0)
				loadout[slot] = entries[rand.Next(entries.Count)].ItemType;
		}

		foreach (WardrobeSlot slot in WardrobeSlots.All) {
			if (!WardrobeSlots.IsEffect(slot) && slot != WardrobeSlot.Wings || rand.NextFloat() > 0.6f)
				continue;
			var entries = WardrobeCatalog.GetEntries(slot).Where(e => e.Source.Id == "TerraSkins" || slot == WardrobeSlot.Wings).ToList();
			if (entries.Count > 0)
				loadout[slot] = entries[rand.Next(entries.Count)].ItemType;
		}
		Main.LocalPlayer.GetModPlayer<WardrobePlayer>().SetLoadout(loadout);
	}

	private void StartDrag(UIMouseEvent evt, UIElement target)
	{
		// Only the title bar drags the window.
		if (evt.Target != window || evt.MousePosition.Y > window.GetDimensions().Y + TitleHeight + 10f)
			return;

		dragging = true;
		dragOffset = evt.MousePosition - new Vector2(window.Left.Pixels, window.Top.Pixels);
	}

	public override void OnActivate()
	{
		preview?.Refresh();
		if (tab == OutfitsTab)
			outfitGrid?.Rebuild();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		if (dragging) {
			window.Left.Set(Main.MouseScreen.X - dragOffset.X, 0f);
			window.Top.Set(Main.MouseScreen.Y - dragOffset.Y, 0f);
			window.Recalculate();
		}

		// Shrink on small screens.
		float maxWidth = Main.screenWidth / Main.UIScale - 40f;
		float maxHeight = Main.screenHeight / Main.UIScale - 120f;
		float width = Math.Clamp(maxWidth, 760f, 900f);
		float height = Math.Clamp(maxHeight, 420f, 560f);
		if (window.Width.Pixels != width || window.Height.Pixels != height) {
			window.Width.Set(width, 0f);
			window.Height.Set(height, 0f);
			Recalculate();
		}

		if (window.ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;
	}

	/// <summary> The window panel with its title. </summary>
	private sealed class Window(WardrobeState owner) : UIElement
	{
		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Rectangle area = GetDimensions().ToRectangle();
			UIStyle.DrawPanel(spriteBatch, area, UIStyle.Window);

			var titlePosition = new Vector2(area.X + 14f, area.Y + 12f);
			UIStyle.DrawText(spriteBatch, Language.GetTextValue("Mods.TerraSkins.UI.Title"), titlePosition, UIStyle.Gold, 1f);

			if (owner.tab != OutfitsTab && owner.dyeMode) {
				string hint = Language.GetTextValue("Mods.TerraSkins.UI.DyeModeHint", SlotName(owner.Slot));
				UIStyle.DrawText(spriteBatch, hint, new Vector2(area.Right - 44f, area.Y + 14f), UIStyle.Accent, 0.75f, 1f, 0f);
			}
		}
	}

	private sealed class UIFooterLabel(Func<string> text) : UIElement
	{
		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Rectangle area = GetDimensions().ToRectangle();
			UIStyle.DrawText(spriteBatch, UIStyle.Fit(text(), area.Width - 8, 0.85f), new Vector2(area.X + 4, area.Center.Y + 2), UIStyle.Text, 0.85f, 0f, 0.5f);
		}
	}
}
