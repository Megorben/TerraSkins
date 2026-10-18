using System.Collections.Generic;

namespace TerraSkins.Common.Effects.Library;

/// <summary>
/// Every effect variant shipped with TerraSkins. Each entry becomes an item with the same name.
/// The texture generator in tools/ reads this file to know which item icons to draw, keep one entry per line.
/// </summary>
public static class SkinEffectCatalog
{
	public static IEnumerable<SkinEffect> CreateAll() => [
		// Auras
		new BlackHoleAura("EventHorizon", Palettes.Inferno),
		new BlackHoleAura("VioletSingularity", Palettes.Violet),
		new BlackHoleAura("AzureAbyss", Palettes.Azure),
		new BlackHoleAura("Quasar", Palettes.Spectral, jets: true),
		new GalaxyAura("MilkyWay", Palettes.Starlight),
		new GalaxyAura("NebulaGalaxy", Palettes.Nebula),
		new GalaxyAura("EmeraldGalaxy", Palettes.Emerald),
		new SoulFlameAura("SpiritSoulfire", Palettes.Spectral),
		new SoulFlameAura("CursedSoulfire", Palettes.Cursed),
		new SoulFlameAura("ShadowSoulfire", Palettes.Shadow),
		new SoulFlameAura("SolarSoulfire", Palettes.Solar),
		new DivineRaysAura("DivineRadiance", Palettes.Holy),
		new DivineRaysAura("MoonlitRadiance", Palettes.Moonlight),
		new DivineRaysAura("PrismaticRadiance", Palettes.Prism),
		new ConstellationAura("GreatBear", Palettes.Starlight, ConstellationAura.DipperStars, ConstellationAura.DipperLinks),
		new ConstellationAura("Hunter", Palettes.Azure, ConstellationAura.OrionStars, ConstellationAura.OrionLinks),
		new VoidAura("VoidMaw", Palettes.Void),
		new PetalStormAura("SakuraStorm", Palettes.Sakura, 0),
		new PetalStormAura("SpringBreeze", Palettes.Meadow, 1),
		new PetalStormAura("AutumnGale", Palettes.Autumn, 2),
		new BlizzardAura("Blizzard", Palettes.Frost),
		new StormAura("Thunderstorm", Palettes.Electric),
		new StormAura("BloodStorm", Palettes.Crimson),
		new BubbleAura("DeepSea", Palettes.Ocean),
		new HeartsAura("Lovestruck", Palettes.Love),
		new MelodyAura("Melody", Palettes.Prism),
		new NebulaAura("NebulaVeil", Palettes.Nebula),
		new NebulaAura("AuroraVeil", Palettes.Abyss),

		// Halos
		new AngelHalo("AngelHalo", Palettes.Holy),
		new AngelHalo("MoonHalo", Palettes.Moonlight),
		new AngelHalo("RoseHalo", Palettes.Rose),
		new AngelHalo("PrismHalo", Palettes.Prism),
		new FallenHalo("FallenHalo", Palettes.Crimson),
		new FallenHalo("VoidHalo", Palettes.Void),
		new SingularityHalo("SingularityCrown", Palettes.Inferno),
		new SingularityHalo("VoidCrown", Palettes.Violet),
		new StarCrownHalo("StarCrown", Palettes.Starlight),
		new RuneHalo("ArcaneHalo", Palettes.Azure),
		new RuneHalo("InfernalHalo", Palettes.Solar),
		new PlanetHalo("RingedPlanet", Palettes.Topaz),
		new FlameCrownHalo("SpiritFlameCrown", Palettes.Spectral),
		new FlameCrownHalo("SolarFlameCrown", Palettes.Solar),

		// Orbits
		new StarOrbit("GoldenStars", Palettes.Gold),
		new StarOrbit("AzureStars", Palettes.Azure),
		new StarOrbit("PrismStars", Palettes.Prism, 5),
		new CrystalOrbit("AmethystShards", Palettes.Amethyst),
		new CrystalOrbit("EmeraldShards", Palettes.Emerald),
		new CrystalOrbit("RubyShards", Palettes.Ruby),
		new CrystalOrbit("SapphireShards", Palettes.Sapphire),
		new MoonOrbit("TinyMoons", Palettes.Moonlight),
		new WispOrbit("SpiritWisps", Palettes.Spectral),
		new WispOrbit("CursedWisps", Palettes.Cursed),
		new BladeOrbit("SpectralBlades", Palettes.Spectral),
		new BladeOrbit("ShadowBlades", Palettes.Shadow),
		new AtomOrbit("Atom", Palettes.Electric),
		new ButterflyOrbit("Butterflies", Palettes.Sakura),
		new ButterflyOrbit("MorphoButterflies", Palettes.Azure),

		// Trails
		new StardustTrail("Stardust", Palettes.Gold),
		new StardustTrail("AzureStardust", Palettes.Azure),
		new StardustTrail("RoseStardust", Palettes.Rose),
		new RibbonTrail("RainbowRibbon", Palettes.Prism),
		new RibbonTrail("NebulaRibbon", Palettes.Nebula),
		new FlameTrail("SolarFlameTrail", Palettes.Solar),
		new FlameTrail("CursedFlameTrail", Palettes.Cursed),
		new FlameTrail("SpiritFlameTrail", Palettes.Spectral),
		new VoidTrail("VoidTrail", Palettes.Void),
		new PetalTrail("SakuraTrail", Palettes.Sakura, 0),
		new PetalTrail("AutumnTrail", Palettes.Autumn, 2),
		new LightningTrail("LightningTrail", Palettes.Electric),
		new FrostTrail("FrostTrail", Palettes.Frost),
		new BubbleTrail("BubbleTrail", Palettes.Ocean),
		new AfterimageTrail("PhantomAfterimage", Palettes.Spectral),
		new AfterimageTrail("ShadowAfterimage", Palettes.Shadow),
		new AfterimageTrail("PrismAfterimage", Palettes.Prism),

		// Wings
		new SeraphWings("SeraphWings", Palettes.Holy),
		new SeraphWings("AzureSeraphWings", Palettes.Azure),
		new SeraphWings("RoseSeraphWings", Palettes.Rose),
		new VoidTendrilWings("VoidTendrils", Palettes.Void),
		new VoidTendrilWings("AbyssTendrils", Palettes.Abyss),
		new CrystalWings("PrismaticWings", Palettes.Prism),
		new CrystalWings("AmethystWings", Palettes.Amethyst),
		new PhoenixWings("PhoenixWings", Palettes.Phoenix),
		new PhoenixWings("SpiritFlameWings", Palettes.Spectral),
		new ConstellationWings("ConstellationWings", Palettes.Starlight),

		// Ground
		new MagicCircleGround("ArcaneCircle", Palettes.Azure),
		new MagicCircleGround("InfernalCircle", Palettes.Solar),
		new MagicCircleGround("HolyCircle", Palettes.Holy),
		new FootstepGround("FlowerSteps", Palettes.Meadow, FootstepStyle.Flowers),
		new FootstepGround("SakuraSteps", Palettes.Sakura, FootstepStyle.Flowers),
		new FootstepGround("FrostSteps", Palettes.Frost, FootstepStyle.Frost),
		new ShadowPoolGround("ShadowPool", Palettes.Shadow),
		new SunbeamGround("Sunbeam", Palettes.Holy),
	];
}
