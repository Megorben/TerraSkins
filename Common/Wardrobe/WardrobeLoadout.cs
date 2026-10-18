using System;
using System.Collections.Generic;
using System.IO;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TerraSkins.Common.Wardrobe;

/// <summary>
/// The look a player picked in the wardrobe: one item (appearance) and one dye per <see cref="WardrobeSlot"/>.
/// Purely visual data. It is saved with the character and synced to every client.
/// </summary>
public sealed class WardrobeLoadout
{
	/// <summary> The slot is not overridden: the player's own equipment shows. </summary>
	public const int None = 0;

	/// <summary> The slot is forced to be empty, hiding whatever the player wears there. </summary>
	public const int Hidden = -1;

	private readonly int[] items = new int[WardrobeSlots.Count];
	private readonly int[] dyes = new int[WardrobeSlots.Count];

	public int this[WardrobeSlot slot] {
		get => items[(int)slot];
		set => items[(int)slot] = value;
	}

	public int GetDye(WardrobeSlot slot) => dyes[(int)slot];

	public void SetDye(WardrobeSlot slot, int dyeItemType) => dyes[(int)slot] = dyeItemType;

	public bool IsEmpty {
		get {
			for (int i = 0; i < items.Length; i++) {
				if (items[i] != None || dyes[i] != 0)
					return false;
			}
			return true;
		}
	}

	public void Clear()
	{
		Array.Clear(items);
		Array.Clear(dyes);
	}

	public void CopyFrom(WardrobeLoadout other)
	{
		Array.Copy(other.items, items, items.Length);
		Array.Copy(other.dyes, dyes, dyes.Length);
	}

	public WardrobeLoadout Clone()
	{
		var clone = new WardrobeLoadout();
		clone.CopyFrom(this);
		return clone;
	}

	public bool ContentEquals(WardrobeLoadout other)
	{
		return items.AsSpan().SequenceEqual(other.items) && dyes.AsSpan().SequenceEqual(other.dyes);
	}

	/// <summary> Copies only the slots that <paramref name="other"/> sets, leaving the rest untouched. </summary>
	public void ApplyOver(WardrobeLoadout other)
	{
		for (int i = 0; i < items.Length; i++) {
			if (other.items[i] != None) {
				items[i] = other.items[i];
				dyes[i] = other.dyes[i];
			}
		}
	}

	public void Write(BinaryWriter writer)
	{
		for (int i = 0; i < items.Length; i++) {
			// +1 so that Hidden (-1) fits into the unsigned 7-bit encoding.
			writer.Write7BitEncodedInt(items[i] + 1);
			writer.Write7BitEncodedInt(dyes[i]);
		}
	}

	public void Read(BinaryReader reader)
	{
		for (int i = 0; i < items.Length; i++) {
			items[i] = SanitizeItem(reader.Read7BitEncodedInt() - 1);
			dyes[i] = SanitizeDye(reader.Read7BitEncodedInt());
		}
	}

	private static int SanitizeItem(int type) => type == Hidden || type > 0 && type < ItemLoader.ItemCount ? type : None;

	private static int SanitizeDye(int type) => type > 0 && type < ItemLoader.ItemCount ? type : 0;

	/// <summary> Items are stored by name so that the save survives mods being added, removed or reordered. </summary>
	public TagCompound Save()
	{
		var entries = new List<TagCompound>();
		foreach (WardrobeSlot slot in WardrobeSlots.All) {
			int item = this[slot];
			int dye = GetDye(slot);
			if (item == None)
				continue;

			var entry = new TagCompound {
				["slot"] = slot.ToString(),
				["item"] = item == Hidden ? "-" : ItemID.Search.GetName(item),
			};
			if (dye > 0)
				entry["dye"] = ItemID.Search.GetName(dye);

			entries.Add(entry);
		}
		return new TagCompound { ["entries"] = entries };
	}

	public static WardrobeLoadout Load(TagCompound tag)
	{
		var loadout = new WardrobeLoadout();
		foreach (TagCompound entry in tag.GetList<TagCompound>("entries")) {
			if (!Enum.TryParse(entry.GetString("slot"), out WardrobeSlot slot))
				continue;

			string item = entry.GetString("item");
			if (item == "-") {
				loadout[slot] = Hidden;
			}
			else if (ItemID.Search.TryGetId(item, out int type) && type > 0) {
				loadout[slot] = type;
				if (entry.ContainsKey("dye") && ItemID.Search.TryGetId(entry.GetString("dye"), out int dye))
					loadout.SetDye(slot, dye);
			}
		}
		return loadout;
	}
}
