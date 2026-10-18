using System;
using Terraria;

namespace TerraSkins.Common.Wardrobe;

/// <summary>
/// Every visual slot the wardrobe can override. The order is also the order of the tabs in the wardrobe window.
/// Do not reorder: the values are sent over the network.
/// </summary>
public enum WardrobeSlot : byte
{
	Head,
	Body,
	Legs,
	Wings,
	Back,
	Face,
	Neck,
	Hands,
	Waist,
	Shoes,
	Shield,
	Balloon,
	Aura,
	Halo,
	Orbit,
	Trail,
	Ground,
}

public enum WardrobeSlotGroup : byte
{
	Armor,
	Accessory,
	Effect,
}

public static class WardrobeSlots
{
	public static readonly WardrobeSlot[] All = Enum.GetValues<WardrobeSlot>();

	public static readonly int Count = All.Length;

	public static WardrobeSlotGroup GetGroup(WardrobeSlot slot) => slot switch {
		WardrobeSlot.Head or WardrobeSlot.Body or WardrobeSlot.Legs => WardrobeSlotGroup.Armor,
		>= WardrobeSlot.Aura => WardrobeSlotGroup.Effect,
		_ => WardrobeSlotGroup.Accessory,
	};

	public static bool IsEffect(WardrobeSlot slot) => slot >= WardrobeSlot.Aura;

	/// <summary> Effects bring their own palettes, everything else can be dyed. </summary>
	public static bool AcceptsDye(WardrobeSlot slot) => !IsEffect(slot);

	/// <summary> Whether "hide this slot" makes sense for the slot. </summary>
	public static bool CanHide(WardrobeSlot slot) => true;

	/// <summary>
	/// The equipment texture id the item shows in the given slot, or a value &lt;= 0 if the item has no visual for it.
	/// Effect slots are not backed by equipment textures and always return -1.
	/// </summary>
	public static int GetEquipSlot(Item item, WardrobeSlot slot) => slot switch {
		WardrobeSlot.Head => item.headSlot,
		WardrobeSlot.Body => item.bodySlot,
		WardrobeSlot.Legs => item.legSlot,
		WardrobeSlot.Wings => item.wingSlot,
		WardrobeSlot.Back => Math.Max(item.backSlot, item.frontSlot),
		WardrobeSlot.Face => Math.Max(item.faceSlot, item.beardSlot),
		WardrobeSlot.Neck => item.neckSlot,
		WardrobeSlot.Hands => Math.Max(item.handOnSlot, item.handOffSlot),
		WardrobeSlot.Waist => item.waistSlot,
		WardrobeSlot.Shoes => item.shoeSlot,
		WardrobeSlot.Shield => item.shieldSlot,
		WardrobeSlot.Balloon => item.balloonSlot,
		_ => -1,
	};
}
