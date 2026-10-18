using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.UI.Elements;

/// <summary>
/// Shows a copy of the local player wearing the current wardrobe look, effects included.
/// Left click toggles walking, right click turns the character around.
/// </summary>
public class UIPlayerPreview : UIElement
{
	private readonly Item blankItem = new();
	private Player dummy;
	private int equipmentSignature;

	public float Scale { get; set; } = 1.75f;

	public bool Walking { get; private set; } = true;

	public int Direction { get; private set; } = 1;

	/// <summary> Forces a fresh copy of the player, e.g. when the window opens. </summary>
	public void Refresh() => dummy = null;

	public override void LeftClick(UIMouseEvent evt)
	{
		base.LeftClick(evt);
		Walking = !Walking;
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	public override void RightClick(UIMouseEvent evt)
	{
		base.RightClick(evt);
		Direction = -Direction;
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Player local = Main.LocalPlayer;

		// Re-copy the player when their real equipment changes while the window is open.
		int signature = EquipmentSignature(local);
		if (dummy == null || signature != equipmentSignature) {
			equipmentSignature = signature;
			dummy = local.SerializedClone();
			dummy.GetModPlayer<WardrobePlayer>().IsPreviewDummy = true;
		}

		var source = local.GetModPlayer<WardrobePlayer>();
		var wardrobe = dummy.GetModPlayer<WardrobePlayer>();
		wardrobe.SetLoadout(source.Loadout);
		wardrobe.Enabled = source.Enabled;
		dummy.direction = Direction;
		dummy.velocity = Vector2.Zero;

		using (new Main.CurrentPlayerOverride(dummy)) {
			dummy.ResetEffects();
			dummy.ResetVisibleAccessories();
			dummy.UpdateMiscCounter();
			dummy.UpdateDyes();
			dummy.PlayerFrame();
		}
		wardrobe.UpdateEffects();
	}

	private static int EquipmentSignature(Player player)
	{
		var hash = new HashCode();
		foreach (Item item in player.armor)
			hash.Add(item.type);
		foreach (Item item in player.dye)
			hash.Add(item.type);
		foreach (bool hidden in player.hideVisibleAccessory)
			hash.Add(hidden);
		hash.Add(player.hair);
		hash.Add(player.skinVariant);
		hash.Add(player.hairColor);
		hash.Add(player.skinColor);
		hash.Add(player.shirtColor);
		return hash.ToHashCode();
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		spriteBatch.Draw(UIStyle.PreviewBackground, area, Color.White);
		if (dummy == null)
			return;

		using (new Main.CurrentPlayerOverride(dummy)) {
			if (Walking) {
				int frame = (int)(Effects.FxClock.Seconds / 0.07f) % 14 + 6;
				dummy.bodyFrame.Y = dummy.legFrame.Y = dummy.headFrame.Y = frame * 56;
			}
			else {
				dummy.bodyFrame.Y = dummy.legFrame.Y = dummy.headFrame.Y = 0;
			}
			dummy.WingFrame(wingFlap: false);

			// Feet sit a bit below the middle, the player is scaled around them.
			var feet = new Vector2(area.Center.X, area.Y + area.Height * 0.7f);
			Vector2 position = feet - new Vector2(dummy.width * 0.5f, dummy.height);

			Item held = dummy.inventory[dummy.selectedItem];
			dummy.inventory[dummy.selectedItem] = blankItem;
			Main.PlayerRenderer.DrawPlayer(Main.Camera, dummy, position + Main.screenPosition, 0f, Vector2.Zero, 0f, Scale);
			dummy.inventory[dummy.selectedItem] = held;
		}

		if (IsMouseHovering)
			Main.hoverItemName = Terraria.Localization.Language.GetTextValue("Mods.TerraSkins.UI.PreviewHint");
	}
}
