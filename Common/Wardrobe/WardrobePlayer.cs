using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerraSkins.Common.Effects;

namespace TerraSkins.Common.Wardrobe;

/// <summary> A named look saved by the player. </summary>
public sealed class WardrobeOutfit
{
	public WardrobeOutfit(string name, WardrobeLoadout loadout)
	{
		Name = name;
		Loadout = loadout;
	}

	public string Name { get; set; }

	public WardrobeLoadout Loadout { get; }
}

/// <summary>
/// Applies the wardrobe on top of the player's real equipment. Everything here only changes what is drawn:
/// equipment textures, dyes and cosmetic effects. Stats, set bonuses and lighting are never touched.
/// </summary>
public class WardrobePlayer : ModPlayer
{
	public const int MaxOutfits = 24;

	/// <summary> The look currently picked in the wardrobe. Synced to all clients. </summary>
	public WardrobeLoadout Loadout { get; private set; } = new();

	/// <summary> Looks saved by the player. Local only. </summary>
	public List<WardrobeOutfit> Outfits { get; } = new();

	/// <summary> Master switch, lets the player quickly show their real equipment. Synced. </summary>
	public bool Enabled { get; set; } = true;

	/// <summary> Marks the clone drawn in the wardrobe preview. </summary>
	public bool IsPreviewDummy { get; set; }

	// Effects granted by TerraSkins accessories worn in visible slots, gathered during this frame's UpdateVisibleAccessories.
	private readonly SkinEffect[] accessoryEffects = new SkinEffect[WardrobeSlots.Count];
	private readonly SkinEffect[] activeEffects = new SkinEffect[WardrobeSlots.Count];
	private readonly EffectState[] effectStates = new EffectState[WardrobeSlots.Count];
	private SkinEffect copyTintEffect;

	public SkinEffect GetActiveEffect(WardrobeSlot slot) => activeEffects[(int)slot];

	public EffectState GetEffectState(WardrobeSlot slot) => effectStates[(int)slot];

	public bool HasAnyActiveEffect {
		get {
			foreach (var effect in activeEffects) {
				if (effect != null)
					return true;
			}
			return false;
		}
	}

	internal void AddAccessoryEffect(SkinEffect effect)
	{
		// The first visible accessory wins, like vanilla wings.
		accessoryEffects[(int)effect.Slot] ??= effect;
	}

	public override void FrameEffects()
	{
		if (Enabled) {
			ApplyArmor();
			foreach (WardrobeSlot slot in WardrobeSlots.All) {
				if (WardrobeSlots.GetGroup(slot) == WardrobeSlotGroup.Accessory)
					ApplyAccessory(slot, Loadout[slot]);
			}
		}

		ResolveEffects();

		foreach (var effect in activeEffects)
			effect?.FrameEffects(Player);
	}

	private void ResolveEffects()
	{
		for (int i = 0; i < activeEffects.Length; i++) {
			int selected = Enabled ? Loadout[(WardrobeSlot)i] : WardrobeLoadout.None;

			// A regular item picked for a slot that also takes effects (wings) replaces accessory effects, hence no fallback.
			SkinEffect effect = selected switch {
				WardrobeLoadout.Hidden => null,
				WardrobeLoadout.None => accessoryEffects[i],
				_ => SkinEffectRegistry.GetEffect(selected),
			};

			if (activeEffects[i] != effect) {
				activeEffects[i] = effect;
				effectStates[i] = effect?.CreateState();
			}
			accessoryEffects[i] = null;
		}
	}

	private static Item Sample(int type) => ContentSamples.ItemsByType[type];

	private void ApplyArmor()
	{
		int body = Loadout[WardrobeSlot.Body];
		int head = Loadout[WardrobeSlot.Head];
		int legs = Loadout[WardrobeSlot.Legs];
		bool robeFromWardrobe = false;

		if (body != WardrobeLoadout.None) {
			// Undo the robe skirt of the real body armor before showing another body.
			if (Player.wearsRobe) {
				Player.wearsRobe = false;
				Player.legs = VisibleEquipmentLegs();
			}

			Player.body = body == WardrobeLoadout.Hidden ? -1 : Sample(body).bodySlot;
			if (Player.body > 0) {
				bool robes = false;
				int match = Player.SetMatch(1, Player.body, Player.Male, ref robes);
				if (match > -1) {
					Player.legs = match;
					Player.wearsRobe = robes;
					robeFromWardrobe = robes;
				}
			}
		}

		if (head != WardrobeLoadout.None) {
			Player.head = head == WardrobeLoadout.Hidden ? -1 : Sample(head).headSlot;
			if (Player.head > 0) {
				bool unused = false;
				int match = Player.SetMatch(0, Player.head, Player.Male, ref unused);
				if (match > -1)
					Player.head = match;
			}
		}

		if (legs != WardrobeLoadout.None && !robeFromWardrobe) {
			if (Player.wearsRobe) {
				// A wardrobe pick for the legs replaces the skirt of a worn robe.
				Player.wearsRobe = false;
			}

			Player.legs = legs == WardrobeLoadout.Hidden ? -1 : Sample(legs).legSlot;
			if (Player.legs > 0) {
				bool unused = false;
				int match = Player.SetMatch(2, Player.legs, Player.Male, ref unused);
				if (match > -1)
					Player.legs = match;
			}
		}
	}

	private int VisibleEquipmentLegs()
	{
		Item vanity = Player.armor[12];
		return !vanity.IsAir ? vanity.legSlot : Player.armor[2].legSlot;
	}

	private void ApplyAccessory(WardrobeSlot slot, int type)
	{
		if (type == WardrobeLoadout.None)
			return;

		if (type > 0 && SkinEffectRegistry.GetEffect(type) != null) {
			// TerraSkins effect items (e.g. ethereal wings) are drawn by the effect itself, but they still hide the slot.
			ClearAccessory(slot);
			return;
		}

		ClearAccessory(slot);
		if (type == WardrobeLoadout.Hidden)
			return;

		Item item = Sample(type);
		switch (slot) {
			case WardrobeSlot.Wings:
				Player.wings = item.wingSlot;
				break;
			case WardrobeSlot.Back:
				if (item.backSlot > 0) {
					if (ArmorIDs.Back.Sets.DrawInBackpackLayer[item.backSlot])
						Player.backpack = item.backSlot;
					else if (ArmorIDs.Back.Sets.DrawInTailLayer[item.backSlot])
						Player.tail = item.backSlot;
					else
						Player.back = item.backSlot;
				}
				if (item.frontSlot > 0)
					Player.front = item.frontSlot;
				break;
			case WardrobeSlot.Face:
				if (item.faceSlot > 0) {
					if (ArmorIDs.Face.Sets.DrawInFaceHeadLayer[item.faceSlot])
						Player.faceHead = item.faceSlot;
					else if (ArmorIDs.Face.Sets.DrawInFaceFlowerLayer[item.faceSlot])
						Player.faceFlower = item.faceSlot;
					else
						Player.face = item.faceSlot;
				}
				if (item.beardSlot > 0)
					Player.beard = item.beardSlot;
				break;
			case WardrobeSlot.Neck:
				Player.neck = item.neckSlot;
				break;
			case WardrobeSlot.Hands:
				if (item.handOnSlot > 0)
					Player.handon = item.handOnSlot;
				if (item.handOffSlot > 0)
					Player.handoff = item.handOffSlot;
				break;
			case WardrobeSlot.Waist:
				Player.waist = item.waistSlot;
				break;
			case WardrobeSlot.Shoes:
				Player.shoe = item.shoeSlot;
				if (!Player.Male && Player.shoe > 0 && ArmorIDs.Shoe.Sets.MaleToFemaleID[Player.shoe] > 0)
					Player.shoe = ArmorIDs.Shoe.Sets.MaleToFemaleID[Player.shoe];
				break;
			case WardrobeSlot.Shield:
				Player.shield = item.shieldSlot;
				break;
			case WardrobeSlot.Balloon:
				if (item.balloonSlot > 0) {
					if (ArmorIDs.Balloon.Sets.DrawInFrontOfBackArmLayer[item.balloonSlot])
						Player.balloonFront = item.balloonSlot;
					else
						Player.balloon = item.balloonSlot;
				}
				break;
		}
	}

	private void ClearAccessory(WardrobeSlot slot)
	{
		switch (slot) {
			case WardrobeSlot.Wings:
				Player.wings = 0;
				break;
			case WardrobeSlot.Back:
				Player.back = Player.front = Player.backpack = Player.tail = -1;
				break;
			case WardrobeSlot.Face:
				Player.face = Player.faceHead = Player.faceFlower = Player.beard = -1;
				break;
			case WardrobeSlot.Neck:
				Player.neck = -1;
				break;
			case WardrobeSlot.Hands:
				Player.handon = Player.handoff = -1;
				break;
			case WardrobeSlot.Waist:
				Player.waist = -1;
				break;
			case WardrobeSlot.Shoes:
				Player.shoe = -1;
				break;
			case WardrobeSlot.Shield:
				Player.shield = -1;
				break;
			case WardrobeSlot.Balloon:
				Player.balloon = Player.balloonFront = -1;
				break;
		}
	}

	public override void UpdateDyes()
	{
		if (!Enabled)
			return;

		foreach (WardrobeSlot slot in WardrobeSlots.All) {
			if (!WardrobeSlots.AcceptsDye(slot) || Loadout[slot] == WardrobeLoadout.None)
				continue;

			int dyeItem = Loadout.GetDye(slot);
			int shader = dyeItem > 0 ? Sample(dyeItem).dye : 0;
			switch (slot) {
				case WardrobeSlot.Head:
					Player.cHead = shader;
					break;
				case WardrobeSlot.Body:
					Player.cBody = shader;
					break;
				case WardrobeSlot.Legs:
					Player.cLegs = shader;
					break;
				case WardrobeSlot.Wings:
					Player.cWings = shader;
					break;
				case WardrobeSlot.Back:
					Player.cBack = Player.cFront = Player.cBackpack = Player.cTail = shader;
					break;
				case WardrobeSlot.Face:
					Player.cFace = Player.cFaceHead = Player.cFaceFlower = Player.cBeard = shader;
					break;
				case WardrobeSlot.Neck:
					Player.cNeck = shader;
					break;
				case WardrobeSlot.Hands:
					Player.cHandOn = Player.cHandOff = shader;
					break;
				case WardrobeSlot.Waist:
					Player.cWaist = shader;
					break;
				case WardrobeSlot.Shoes:
					Player.cShoe = shader;
					break;
				case WardrobeSlot.Shield:
					Player.cShield = shader;
					break;
				case WardrobeSlot.Balloon:
					Player.cBalloon = Player.cBalloonFront = shader;
					break;
			}
		}
	}

	public override void PostUpdate()
	{
		if (Main.dedServ)
			return;

		UpdateEffects();
	}

	/// <summary> Advances stateful effects (trails). Also called by the wardrobe preview for its clone. </summary>
	internal void UpdateEffects()
	{
		for (int i = 0; i < activeEffects.Length; i++) {
			if (activeEffects[i] is { } effect && effectStates[i] is { } state) {
				state.Age++;
				effect.Update(Player, state);
			}
		}
	}

	public override void DrawPlayer(Camera camera)
	{
		if (!EffectVisibility.ShouldDraw(Player))
			return;

		for (int i = 0; i < activeEffects.Length; i++) {
			if (activeEffects[i] is { } effect) {
				copyTintEffect = effect;
				effect.DrawPlayerCopies(Player, camera, effectStates[i]);
				copyTintEffect = null;
			}
		}
	}

	public override void TransformDrawData(ref PlayerDrawSet drawInfo)
	{
		if (copyTintEffect != null && drawInfo.shadow > 0f)
			copyTintEffect.TransformCopy(ref drawInfo);
	}

	public override void SaveData(TagCompound tag)
	{
		tag["loadout"] = Loadout.Save();
		if (!Enabled)
			tag["disabled"] = true;

		var outfits = new List<TagCompound>();
		foreach (var outfit in Outfits)
			outfits.Add(new TagCompound { ["name"] = outfit.Name, ["loadout"] = outfit.Loadout.Save() });
		tag["outfits"] = outfits;
	}

	public override void LoadData(TagCompound tag)
	{
		Loadout = tag.ContainsKey("loadout") ? WardrobeLoadout.Load(tag.GetCompound("loadout")) : new WardrobeLoadout();
		Enabled = !tag.GetBool("disabled");

		Outfits.Clear();
		foreach (TagCompound outfit in tag.GetList<TagCompound>("outfits")) {
			if (Outfits.Count >= MaxOutfits)
				break;
			Outfits.Add(new WardrobeOutfit(outfit.GetString("name"), WardrobeLoadout.Load(outfit.GetCompound("loadout"))));
		}
	}

	public override void CopyClientState(ModPlayer targetCopy)
	{
		var target = (WardrobePlayer)targetCopy;
		target.Loadout.CopyFrom(Loadout);
		target.Enabled = Enabled;
	}

	public override void SendClientChanges(ModPlayer clientPlayer)
	{
		var previous = (WardrobePlayer)clientPlayer;
		if (previous.Enabled != Enabled || !previous.Loadout.ContentEquals(Loadout))
			WardrobeNetwork.SendLoadout(Mod, Player.whoAmI, -1, -1);
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
	{
		WardrobeNetwork.SendLoadout(Mod, Player.whoAmI, toWho, fromWho);
	}

	/// <summary> Replaces the whole look, e.g. when an outfit is picked. </summary>
	public void SetLoadout(WardrobeLoadout loadout) => Loadout.CopyFrom(loadout);
}
