using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.UI.Elements;

/// <summary> Cards of ready-made outfits and the player's own saved looks. </summary>
public class UIOutfitGrid : UIVirtualGrid
{
	public enum CardKind : byte
	{
		SaveCurrent,
		Preset,
		Saved,
	}

	public readonly record struct Card(CardKind Kind, int Index);

	private readonly List<Card> cards = new();

	public UIOutfitGrid(UIScrollbar scrollbar) : base(scrollbar)
	{
	}

	public event Action<Card, bool> OnPick;

	protected override int Count => cards.Count;

	protected override Point CellSize => new(176, 50);

	protected override int Gap => 6;

	private static WardrobePlayer Wardrobe => Main.LocalPlayer.GetModPlayer<WardrobePlayer>();

	public void Rebuild()
	{
		cards.Clear();
		cards.Add(new Card(CardKind.SaveCurrent, 0));
		for (int i = 0; i < Wardrobe.Outfits.Count; i++)
			cards.Add(new Card(CardKind.Saved, i));
		for (int i = 0; i < WardrobePresets.All.Count; i++)
			cards.Add(new Card(CardKind.Preset, i));
	}

	protected override void ClickCell(int index, bool right)
	{
		SoundEngine.PlaySound(right ? SoundID.MenuClose : SoundID.MenuTick);
		OnPick?.Invoke(cards[index], right);
	}

	private static string Title(Card card) => card.Kind switch {
		CardKind.SaveCurrent => Language.GetTextValue("Mods.TerraSkins.UI.SaveOutfit"),
		CardKind.Preset => WardrobePresets.All[card.Index].DisplayName.Value,
		_ => Wardrobe.Outfits[card.Index].Name,
	};

	private static WardrobeLoadout LoadoutOf(Card card) => card.Kind switch {
		CardKind.Preset => WardrobePresets.All[card.Index].Loadout,
		CardKind.Saved => Wardrobe.Outfits[card.Index].Loadout,
		_ => null,
	};

	protected override void DrawCell(SpriteBatch spriteBatch, int index, Rectangle area, bool hovered)
	{
		Card card = cards[index];
		Color accent = card.Kind == CardKind.Preset ? WardrobePresets.All[card.Index].Accent : UIStyle.Accent;
		WardrobeLoadout loadout = LoadoutOf(card);
		bool worn = loadout != null && loadout.ContentEquals(Wardrobe.Loadout);

		if (worn) {
			UIStyle.DrawPanel(spriteBatch, area, UIStyle.Gold);
			area.Inflate(-2, -2);
		}
		UIStyle.DrawPanel(spriteBatch, area, hovered ? UIStyle.PanelHover : UIStyle.Panel);

		// A thin accent stripe on the left.
		var stripe = new Rectangle(area.X + 5, area.Y + 6, 3, area.Height - 12);
		spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, stripe, accent);

		var iconCenter = new Vector2(area.X + 30, area.Center.Y);
		if (card.Kind == CardKind.SaveCurrent) {
			Texture2D icons = UIStyle.CellIcons;
			spriteBatch.Draw(icons, iconCenter, new Rectangle(3 * 32, 0, 32, 32), Color.White, 0f, new Vector2(16f), 1f, SpriteEffects.None, 0f);
		}
		else {
			int iconItem = IconItem(loadout);
			if (iconItem > 0) {
				Main.instance.LoadItem(iconItem);
				ItemSlot.DrawItemIcon(ContentSamples.ItemsByType[iconItem], ItemSlot.Context.InventoryItem, spriteBatch, iconCenter, 1f, 32f, Color.White);
			}
		}

		string title = UIStyle.Fit(Title(card), area.Width - 60, 0.8f);
		UIStyle.DrawText(spriteBatch, title, new Vector2(area.X + 52, area.Y + 9), hovered ? UIStyle.Gold : UIStyle.Text, 0.8f);

		string subtitle = card.Kind switch {
			CardKind.SaveCurrent => Language.GetTextValue("Mods.TerraSkins.UI.SaveOutfitHint"),
			CardKind.Preset => Language.GetTextValue("Mods.TerraSkins.UI.PresetOutfit"),
			_ => Language.GetTextValue("Mods.TerraSkins.UI.SavedOutfit"),
		};
		UIStyle.DrawText(spriteBatch, UIStyle.Fit(subtitle, area.Width - 60, 0.65f), new Vector2(area.X + 52, area.Y + 28), UIStyle.TextDim, 0.65f);
	}

	/// <summary> The head piece, or the first thing set in the loadout. </summary>
	private static int IconItem(WardrobeLoadout loadout)
	{
		if (loadout == null)
			return 0;

		foreach (WardrobeSlot slot in WardrobeSlots.All) {
			if (loadout[slot] > 0)
				return loadout[slot];
		}
		return 0;
	}

	protected override void HoverCell(int index)
	{
		Card card = cards[index];
		Main.hoverItemName = card.Kind switch {
			CardKind.SaveCurrent => Language.GetTextValue("Mods.TerraSkins.UI.SaveOutfitTooltip"),
			CardKind.Preset => Language.GetTextValue("Mods.TerraSkins.UI.PresetTooltip", Title(card)),
			_ => Language.GetTextValue("Mods.TerraSkins.UI.SavedTooltip", Title(card)),
		};
	}
}
