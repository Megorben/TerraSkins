using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TerraSkins.Common.Effects;
using TerraSkins.Common.Effects.Library;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Content.Items;

/// <summary>
/// The item form of a <see cref="SkinEffect"/>. Wear it in any visible accessory or social slot to show the effect,
/// or pick it in the wardrobe without owning it. It has no stats at all.
/// </summary>
public class SkinEffectItem : ModItem, ICustomAutoload
{
	[CloneByReference]
	private readonly SkinEffect effect;

	public static void Autoload(Mod mod)
	{
		foreach (SkinEffect effect in SkinEffectCatalog.CreateAll())
			mod.AddContent(new SkinEffectItem(effect));
	}

	// Content loaded multiple times must have a non-default constructor.
	public SkinEffectItem(SkinEffect effect)
	{
		this.effect = effect;
	}

	public SkinEffect Effect => effect;

	public override string Name => effect.Name;

	public override string Texture => $"TerraSkins/Assets/Textures/Items/Effects/{effect.Name}";

	protected override bool CloneNewInstances => true;

	public override void SetStaticDefaults()
	{
		SkinEffectRegistry.Register(Type, effect);
	}

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 28;
		Item.accessory = true;
		Item.hasVanityEffects = true;
		Item.rare = effect.Rarity;
		Item.value = Item.buyPrice(gold: 2);
	}

	public override void UpdateVisibleAccessory(Player player, bool hideVisual)
	{
		if (!hideVisual)
			player.GetModPlayer<WardrobePlayer>().AddAccessoryEffect(effect);
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		CosmeticTooltips.Add(Mod, tooltips, effect.Slot);
	}

	public override void AddRecipes()
	{
		// Cheap on purpose: these are cosmetics, and the wardrobe lets everyone use them for free anyway.
		CreateRecipe()
			.AddIngredient(ItemID.Silk, 3)
			.AddIngredient(ItemID.FallenStar)
			.AddTile(TileID.Loom)
			.Register();
	}
}

public static class CosmeticTooltips
{
	public static void Add(Mod mod, List<TooltipLine> tooltips, WardrobeSlot slot)
	{
		string slotName = Language.GetTextValue($"Mods.TerraSkins.Slots.{slot}");
		tooltips.Add(new TooltipLine(mod, "TerraSkinsSlot", Language.GetTextValue("Mods.TerraSkins.Tooltips.Slot", slotName)) {
			OverrideColor = new Color(190, 160, 255),
		});
		tooltips.Add(new TooltipLine(mod, "TerraSkinsCosmetic", Language.GetTextValue("Mods.TerraSkins.Tooltips.Cosmetic")) {
			OverrideColor = new Color(150, 150, 170),
		});
	}
}
