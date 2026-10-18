using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria.ModLoader;
using TerraSkins.Content.Items;
using TerraSkins.Content.Items.Sets;

namespace TerraSkins.Common.Wardrobe;

/// <summary> A ready-made full outfit: a TerraSkins set together with matching effects. </summary>
public sealed class WardrobePreset(string key, LocalizedText displayName, Color accent, int iconItem, WardrobeLoadout loadout)
{
	public string Key { get; } = key;

	public LocalizedText DisplayName { get; } = displayName;

	public Color Accent { get; } = accent;

	public int IconItem { get; } = iconItem;

	public WardrobeLoadout Loadout { get; } = loadout;
}

public class WardrobePresets : ModSystem
{
	private static readonly List<WardrobePreset> presets = new();

	public static IReadOnlyList<WardrobePreset> All => presets;

	public override void PostSetupContent()
	{
		presets.Clear();
		foreach (SkinSet set in SkinSets.All) {
			var loadout = new WardrobeLoadout {
				[WardrobeSlot.Head] = Mod.Find<ModItem>(set.Name + "Head").Type,
				[WardrobeSlot.Body] = Mod.Find<ModItem>(set.Name + "Body").Type,
			};
			if (set.HasLegs)
				loadout[WardrobeSlot.Legs] = Mod.Find<ModItem>(set.Name + "Legs").Type;

			foreach (string effectName in set.OutfitEffects) {
				var item = (SkinEffectItem)Mod.Find<ModItem>(effectName);
				loadout[item.Effect.Slot] = item.Type;
			}

			presets.Add(new WardrobePreset(set.Name, Mod.GetLocalization($"Sets.{set.Name}"), set.Accent, loadout[WardrobeSlot.Head], loadout));
		}
	}

	public override void Unload() => presets.Clear();
}
