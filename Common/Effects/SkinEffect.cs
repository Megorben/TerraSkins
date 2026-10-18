using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.Effects;

/// <summary>
/// An animated cosmetic effect (aura, halo, orbiting objects, trail, wings...). Each instance is one variant and is
/// represented in game by a <see cref="Content.Items.SkinEffectItem"/> of the same name.
/// <br/>Effects are purely visual: they never emit light, spawn gameplay entities or touch the player's stats.
/// </summary>
[CloneByReference]
public abstract class SkinEffect
{
	protected SkinEffect(string name, WardrobeSlot slot, FxPalette palette)
	{
		Name = name;
		Slot = slot;
		Palette = palette;
	}

	/// <summary> Unique internal name, also used as the item name. </summary>
	public string Name { get; }

	public WardrobeSlot Slot { get; }

	public FxPalette Palette { get; }

	/// <summary> Item rarity used for the name color of the item, purely cosmetic. </summary>
	public virtual int Rarity => Terraria.ID.ItemRarityID.LightPurple;

	/// <summary> Per-player runtime state. Stateless effects (animated purely from time) return null. </summary>
	public virtual EffectState CreateState() => null;

	/// <summary> Called once per game tick on clients while the effect is shown. Only called if <see cref="CreateState"/> returned a state. </summary>
	public virtual void Update(Player player, EffectState state) { }

	/// <summary> Drawn behind everything else of the player. </summary>
	public virtual void DrawBehind(in FxFrame frame) { }

	/// <summary> Drawn right after vanilla wings, behind the body. </summary>
	public virtual void DrawWings(in FxFrame frame) { }

	/// <summary> Drawn over the player. </summary>
	public virtual void DrawFront(in FxFrame frame) { }

	/// <summary> Called from <see cref="Terraria.ModLoader.ModPlayer.FrameEffects"/> while the effect is shown. </summary>
	public virtual void FrameEffects(Player player) { }

	/// <summary> Allows drawing extra copies of the player (afterimages). </summary>
	public virtual void DrawPlayerCopies(Player player, Camera camera, EffectState state) { }

	/// <summary> Allows recoloring the draw data of afterimage copies drawn by <see cref="DrawPlayerCopies"/>. </summary>
	public virtual void TransformCopy(ref Terraria.DataStructures.PlayerDrawSet drawInfo) { }
}

/// <summary> Base class for per-player effect state. </summary>
public class EffectState
{
	/// <summary> Ticks since the state was created. </summary>
	public int Age;
}
