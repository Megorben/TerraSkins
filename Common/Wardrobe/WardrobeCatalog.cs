using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using TerraSkins.Common.Effects;

namespace TerraSkins.Common.Wardrobe;

/// <summary> Where an appearance comes from. Used for the source filter of the wardrobe. </summary>
public sealed record WardrobeSource(string Id, string DisplayName, int SortOrder);

/// <summary> One selectable appearance in the wardrobe: an item that has a visual for a slot. </summary>
public sealed class WardrobeEntry
{
	public required int ItemType { get; init; }
	public required WardrobeSlot Slot { get; init; }
	public required WardrobeSource Source { get; init; }

	private string searchName;

	/// <summary> Lower-case display name, built lazily because the language can change at runtime. </summary>
	public string SearchName => searchName ??= Lang.GetItemNameValue(ItemType).ToLowerInvariant();

	internal void ResetSearchName() => searchName = null;
}

/// <summary>
/// Collects every appearance that can be picked for every slot, from TerraSkins, vanilla Terraria and all other loaded mods.
/// </summary>
public class WardrobeCatalog : ModSystem
{
	public const string VanillaSourceId = "Terraria";

	private static readonly Dictionary<WardrobeSlot, List<WardrobeEntry>> entries = new();
	private static readonly List<WardrobeSource> sources = new();
	private static int[] dyes = System.Array.Empty<int>();

	public static IReadOnlyList<WardrobeSource> Sources => sources;

	/// <summary> All dye items, vanilla and modded. </summary>
	public static IReadOnlyList<int> Dyes => dyes;

	public static IReadOnlyList<WardrobeEntry> GetEntries(WardrobeSlot slot) => entries.TryGetValue(slot, out var list) ? list : System.Array.Empty<WardrobeEntry>();

	public override void PostSetupContent()
	{
		entries.Clear();
		sources.Clear();

		var sourceById = new Dictionary<string, WardrobeSource>();
		WardrobeSource GetSource(Item item)
		{
			Mod mod = item.ModItem?.Mod;
			string id = mod?.Name ?? VanillaSourceId;
			if (!sourceById.TryGetValue(id, out var source)) {
				int order = mod == null ? 1 : mod == Mod ? 0 : 2;
				source = new WardrobeSource(id, mod?.DisplayName ?? "Terraria", order);
				sourceById[id] = source;
			}
			return source;
		}

		var dyeList = new List<int>();
		foreach (WardrobeSlot slot in WardrobeSlots.All)
			entries[slot] = new List<WardrobeEntry>();

		// Several items often share one equipment texture (e.g. pre-1.4 copies), only list each look once.
		var seenEquipSlots = new HashSet<(WardrobeSlot, int)>();

		for (int type = 1; type < ItemLoader.ItemCount; type++) {
			if (!ContentSamples.ItemsByType.TryGetValue(type, out Item item) || item.IsAir)
				continue;

			if (ItemID.Sets.Deprecated[type] || item.ModItem is UnloadedItem)
				continue;

			if (item.dye > 0)
				dyeList.Add(type);

			if (SkinEffectRegistry.GetEffect(type) is SkinEffect effect) {
				entries[effect.Slot].Add(new WardrobeEntry { ItemType = type, Slot = effect.Slot, Source = GetSource(item) });
				continue;
			}

			foreach (WardrobeSlot slot in WardrobeSlots.All) {
				if (WardrobeSlots.IsEffect(slot))
					break;

				int equipSlot = WardrobeSlots.GetEquipSlot(item, slot);
				if (equipSlot <= 0 || !seenEquipSlots.Add((slot, equipSlot)))
					continue;

				entries[slot].Add(new WardrobeEntry { ItemType = type, Slot = slot, Source = GetSource(item) });
			}
		}

		sources.AddRange(sourceById.Values.OrderBy(s => s.SortOrder).ThenBy(s => s.DisplayName));

		foreach (var list in entries.Values) {
			// TerraSkins first, then vanilla, then other mods. Inside a source the item id order is kept (roughly chronological).
			var sorted = list.OrderBy(e => e.Source.SortOrder).ThenBy(e => e.Source.DisplayName).ThenBy(e => e.ItemType).ToList();
			list.Clear();
			list.AddRange(sorted);
		}

		dyes = dyeList.ToArray();
	}

	public override void OnLocalizationsLoaded()
	{
		foreach (var list in entries.Values) {
			foreach (var entry in list)
				entry.ResetSearchName();
		}
	}

	public override void Unload()
	{
		entries.Clear();
		sources.Clear();
		dyes = System.Array.Empty<int>();
	}
}
