using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Content.Items.Sets;

/// <summary> Adds the three pieces of every <see cref="SkinSet"/>. </summary>
public class SkinSetLoader : ILoadable
{
	public void Load(Mod mod)
	{
		foreach (SkinSet set in SkinSets.All) {
			mod.AddContent(new SkinHeadItem(set));
			mod.AddContent(new SkinBodyItem(set));
			if (set.HasLegs)
				mod.AddContent(new SkinLegsItem(set));
		}
	}

	public void Unload()
	{
	}
}

/// <summary>
/// A vanity armor piece of a TerraSkins set. Vanity only: no defense, no set bonus.
/// Glowing parts are drawn from an extra "_Glow" texture on top of the regular equipment texture.
/// </summary>
public abstract class SkinArmorItem : ModItem
{
	[CloneByReference]
	private readonly SkinSet set;

	private Asset<Texture2D> glow;
	private Asset<Texture2D> robeGlow;

	protected SkinArmorItem(SkinSet set)
	{
		this.set = set;
	}

	public SkinSet Set => set;

	protected abstract EquipType Piece { get; }

	protected abstract WardrobeSlot Slot { get; }

	public override string Name => set.Name + Piece;

	public override string Texture => $"TerraSkins/Assets/Textures/Items/Sets/{Name}";

	protected override bool CloneNewInstances => true;

	public override void SetStaticDefaults()
	{
		if (Main.dedServ)
			return;

		glow = RequestIfExists($"{Texture}_{Piece}_Glow");
		if (Piece == EquipType.Body && set.Robe)
			robeGlow = RequestIfExists($"{Texture}_{EquipType.Legs}_Glow");
	}

	private static Asset<Texture2D> RequestIfExists(string path) => ModContent.HasAsset(path) ? ModContent.Request<Texture2D>(path) : null;

	public override void SetDefaults()
	{
		Item.width = 18;
		Item.height = 18;
		Item.vanity = true;
		Item.rare = ItemRarityID.LightPurple;
		Item.value = Item.buyPrice(gold: 3);
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		CosmeticTooltips.Add(Mod, tooltips, Slot);
	}

	public override bool ModifyEquipTextureDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, EquipTexture equipTexture, string methodName)
	{
		drawInfo.DrawDataCache.Add(drawData);

		Asset<Texture2D> glowTexture = equipTexture.Type == EquipType.Legs && Piece == EquipType.Body ? robeGlow : glow;
		if (glowTexture == null)
			return false;

		// Emissive: ignores lighting, but follows the transparency of the base piece (afterimages, immunity flicker).
		float pulse = 1f - set.GlowPulse * (0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f));
		DrawData glowData = drawData;
		glowData.texture = glowTexture.Value;
		glowData.color = Color.White * (drawData.color.A / 255f * pulse);
		drawInfo.DrawDataCache.Add(glowData);
		return false;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Silk, Piece == EquipType.Body ? 8 : 5)
			.AddTile(TileID.Loom)
			.Register();
	}
}

[AutoloadEquip(EquipType.Head)]
public class SkinHeadItem : SkinArmorItem
{
	public SkinHeadItem(SkinSet set) : base(set)
	{
	}

	protected override EquipType Piece => EquipType.Head;

	protected override WardrobeSlot Slot => WardrobeSlot.Head;

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		switch (Set.Head) {
			case HeadStyle.FullHelmet:
				ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
				break;
			case HeadStyle.Hat:
				ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
				break;
			case HeadStyle.Circlet:
				ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
				break;
		}
	}
}

[AutoloadEquip(EquipType.Body)]
public class SkinBodyItem : SkinArmorItem
{
	public SkinBodyItem(SkinSet set) : base(set)
	{
	}

	protected override EquipType Piece => EquipType.Body;

	protected override WardrobeSlot Slot => WardrobeSlot.Body;

	public override void Load()
	{
		// Robes bring their own skirt, registered as a leg texture of this item.
		if (Set.Robe && Main.netMode != NetmodeID.Server)
			EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Legs}", EquipType.Legs, this);
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = true;
	}

	public override void SetMatch(bool male, ref int equipSlot, ref bool robes)
	{
		if (!Set.Robe)
			return;

		robes = true;
		equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
	}
}

[AutoloadEquip(EquipType.Legs)]
public class SkinLegsItem : SkinArmorItem
{
	public SkinLegsItem(SkinSet set) : base(set)
	{
	}

	protected override EquipType Piece => EquipType.Legs;

	protected override WardrobeSlot Slot => WardrobeSlot.Legs;
}
