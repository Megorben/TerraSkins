using System;
using Terraria.ModLoader;

namespace TerraSkins.Common.Effects;

/// <summary> Maps item types to the effect they represent. </summary>
public class SkinEffectRegistry : ModSystem
{
	private static SkinEffect[] byItemType = Array.Empty<SkinEffect>();

	/// <summary> The effect of a TerraSkins effect item, or null for any other item. </summary>
	public static SkinEffect GetEffect(int itemType) => (uint)itemType < (uint)byItemType.Length ? byItemType[itemType] : null;

	internal static void Register(int itemType, SkinEffect effect)
	{
		if (itemType >= byItemType.Length)
			Array.Resize(ref byItemType, Math.Max(itemType + 1, ItemLoader.ItemCount));

		byItemType[itemType] = effect;
	}

	public override void Unload() => byItemType = Array.Empty<SkinEffect>();
}
