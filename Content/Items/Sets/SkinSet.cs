using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace TerraSkins.Content.Items.Sets;

public enum HeadStyle
{
	/// <summary> Covers the whole head, the player's face and hair are not drawn. </summary>
	FullHelmet,
	/// <summary> Sits on top of the head, hair is drawn as under a hat. </summary>
	Hat,
	/// <summary> A circlet or crown, all hair is drawn. </summary>
	Circlet,
}

/// <summary>
/// A TerraSkins vanity set: its pieces, how they interact with the player sprite, and the effects of its preset outfit.
/// Sprites live in Assets/Textures/Items/Sets and are produced by tools/gen_armor.py.
/// </summary>
[CloneByReference]
public sealed record SkinSet(string Name, HeadStyle Head, bool Robe, Color Accent, float GlowPulse, string[] OutfitEffects)
{
	public bool HasLegs => !Robe;
}

public static class SkinSets
{
	/// <summary> One entry per line, the texture generator reads this list. </summary>
	public static readonly SkinSet[] All = [
		new("VoidSovereign", HeadStyle.FullHelmet, false, new Color(200, 120, 255), 0.25f, ["VioletSingularity", "VoidHalo", "VoidTrail", "VoidTendrils", "ShadowPool"]),
		new("CelestialSeraph", HeadStyle.Circlet, false, new Color(255, 230, 150), 0.15f, ["DivineRadiance", "AngelHalo", "Stardust", "SeraphWings", "HolyCircle"]),
		new("AstralVoyager", HeadStyle.Hat, true, new Color(170, 200, 255), 0.2f, ["MilkyWay", "StarCrown", "GoldenStars", "AzureStardust", "ConstellationWings"]),
		new("InfernalWarlord", HeadStyle.FullHelmet, false, new Color(255, 150, 60), 0.35f, ["SolarSoulfire", "SolarFlameCrown", "SolarFlameTrail", "PhoenixWings", "InfernalCircle"]),
		new("FrostMonarch", HeadStyle.Circlet, false, new Color(170, 230, 255), 0.2f, ["Blizzard", "SapphireShards", "FrostTrail", "FrostSteps", "MoonHalo"]),
		new("SakuraRonin", HeadStyle.Hat, true, new Color(255, 180, 210), 0.1f, ["SakuraStorm", "Butterflies", "SakuraTrail", "SakuraSteps", "RoseHalo"]),
		new("NeonPhantom", HeadStyle.FullHelmet, false, new Color(90, 240, 255), 0.3f, ["Thunderstorm", "Atom", "PhantomAfterimage", "ArcaneHalo", "ArcaneCircle"]),
		new("VerdantDruid", HeadStyle.Circlet, true, new Color(150, 255, 120), 0.2f, ["SpringBreeze", "MorphoButterflies", "FlowerSteps"]),
		new("AbyssalTide", HeadStyle.FullHelmet, false, new Color(80, 240, 220), 0.25f, ["DeepSea", "SpiritWisps", "BubbleTrail", "AbyssTendrils"]),
		new("PrismaticParagon", HeadStyle.FullHelmet, false, Color.White, 0.2f, ["PrismaticRadiance", "PrismHalo", "RainbowRibbon", "PrismaticWings", "PrismStars"]),
	];
}
