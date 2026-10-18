using Microsoft.Xna.Framework;

namespace TerraSkins.Common.Effects;

/// <summary>
/// Colors of an effect variant. <see cref="Core"/> is the hottest/brightest tone, <see cref="Primary"/> the dominant one,
/// <see cref="Edge"/> the outer/darker one and <see cref="Accent"/> a contrasting highlight.
/// A rainbow palette cycles its main colors over time.
/// </summary>
public sealed record FxPalette(string Name, Color Core, Color Primary, Color Edge, Color Accent, bool Rainbow = false)
{
	public Color PrimaryAt(float t) => Rainbow ? Terraria.Main.hslToRgb(Wrap(t), 1f, 0.62f) : Primary;

	public Color EdgeAt(float t) => Rainbow ? Terraria.Main.hslToRgb(Wrap(t + 0.18f), 1f, 0.5f) : Edge;

	public Color AccentAt(float t) => Rainbow ? Terraria.Main.hslToRgb(Wrap(t + 0.5f), 1f, 0.7f) : Accent;

	/// <summary> Blend from <see cref="Core"/> (0) through <see cref="Primary"/> (0.5) to <see cref="Edge"/> (1). </summary>
	public Color Ramp(float k, float t = 0f)
	{
		k = MathHelper.Clamp(k, 0f, 1f);
		return k < 0.5f ? Color.Lerp(Core, PrimaryAt(t), k * 2f) : Color.Lerp(PrimaryAt(t), EdgeAt(t), (k - 0.5f) * 2f);
	}

	private static float Wrap(float t) => t - (float)System.Math.Floor(t);
}

public static class Palettes
{
	public static readonly FxPalette Inferno = new("Inferno", new Color(255, 250, 230), new Color(255, 160, 50), new Color(200, 50, 10), new Color(255, 220, 120));
	public static readonly FxPalette Violet = new("Violet", new Color(250, 235, 255), new Color(190, 90, 255), new Color(90, 20, 170), new Color(255, 120, 220));
	public static readonly FxPalette Azure = new("Azure", new Color(235, 250, 255), new Color(80, 190, 255), new Color(20, 70, 200), new Color(140, 255, 240));
	public static readonly FxPalette Gold = new("Gold", new Color(255, 252, 230), new Color(255, 205, 80), new Color(210, 120, 20), new Color(255, 240, 170));
	public static readonly FxPalette Rose = new("Rose", new Color(255, 240, 248), new Color(255, 120, 180), new Color(190, 40, 110), new Color(255, 200, 230));
	public static readonly FxPalette Emerald = new("Emerald", new Color(235, 255, 240), new Color(70, 230, 140), new Color(10, 130, 70), new Color(190, 255, 120));
	public static readonly FxPalette Crimson = new("Crimson", new Color(255, 225, 220), new Color(255, 60, 70), new Color(130, 0, 20), new Color(255, 150, 90));
	public static readonly FxPalette Frost = new("Frost", new Color(245, 252, 255), new Color(150, 220, 255), new Color(60, 120, 220), new Color(220, 240, 255));
	public static readonly FxPalette Prism = new("Prism", new Color(255, 255, 255), new Color(255, 120, 120), new Color(120, 120, 255), new Color(255, 255, 180), Rainbow: true);
	public static readonly FxPalette Void = new("Void", new Color(240, 220, 255), new Color(150, 60, 255), new Color(40, 0, 90), new Color(255, 70, 200));
	public static readonly FxPalette Abyss = new("Abyss", new Color(220, 255, 250), new Color(60, 220, 200), new Color(0, 60, 90), new Color(140, 80, 255));
	public static readonly FxPalette Solar = new("Solar", new Color(255, 255, 220), new Color(255, 190, 40), new Color(230, 70, 0), new Color(255, 255, 140));
	public static readonly FxPalette Cursed = new("Cursed", new Color(245, 255, 220), new Color(170, 255, 60), new Color(40, 140, 0), new Color(220, 255, 140));
	public static readonly FxPalette Spectral = new("Spectral", new Color(240, 255, 255), new Color(120, 230, 255), new Color(40, 120, 255), new Color(200, 160, 255));
	public static readonly FxPalette Moonlight = new("Moonlight", new Color(250, 250, 255), new Color(190, 210, 255), new Color(90, 110, 200), new Color(230, 230, 255));
	public static readonly FxPalette Sakura = new("Sakura", new Color(255, 245, 250), new Color(255, 170, 205), new Color(220, 90, 150), new Color(255, 230, 240));
	public static readonly FxPalette Autumn = new("Autumn", new Color(255, 240, 200), new Color(255, 150, 40), new Color(170, 50, 10), new Color(255, 210, 90));
	public static readonly FxPalette Meadow = new("Meadow", new Color(250, 255, 230), new Color(140, 230, 80), new Color(40, 140, 40), new Color(255, 230, 120));
	public static readonly FxPalette Electric = new("Electric", new Color(240, 255, 255), new Color(90, 220, 255), new Color(40, 90, 255), new Color(255, 255, 255));
	public static readonly FxPalette Ocean = new("Ocean", new Color(230, 255, 255), new Color(90, 200, 255), new Color(10, 90, 170), new Color(160, 255, 230));
	public static readonly FxPalette Starlight = new("Starlight", new Color(255, 255, 255), new Color(170, 200, 255), new Color(80, 90, 220), new Color(255, 230, 160));
	public static readonly FxPalette Amethyst = new("Amethyst", new Color(250, 235, 255), new Color(200, 110, 255), new Color(110, 40, 200), new Color(240, 180, 255));
	public static readonly FxPalette Ruby = new("Ruby", new Color(255, 235, 235), new Color(255, 70, 100), new Color(150, 10, 40), new Color(255, 170, 170));
	public static readonly FxPalette Sapphire = new("Sapphire", new Color(235, 245, 255), new Color(70, 130, 255), new Color(20, 40, 170), new Color(160, 210, 255));
	public static readonly FxPalette Topaz = new("Topaz", new Color(255, 250, 230), new Color(255, 190, 60), new Color(190, 100, 0), new Color(255, 230, 150));
	public static readonly FxPalette Shadow = new("Shadow", new Color(230, 200, 255), new Color(120, 70, 200), new Color(25, 10, 50), new Color(255, 60, 90));
	public static readonly FxPalette Love = new("Love", new Color(255, 240, 245), new Color(255, 90, 150), new Color(200, 20, 80), new Color(255, 190, 220));
	public static readonly FxPalette Nebula = new("Nebula", new Color(255, 235, 250), new Color(230, 90, 220), new Color(70, 40, 200), new Color(90, 200, 255));
	public static readonly FxPalette Phoenix = new("Phoenix", new Color(255, 250, 220), new Color(255, 140, 30), new Color(220, 30, 30), new Color(255, 230, 90));
	public static readonly FxPalette Holy = new("Holy", new Color(255, 255, 245), new Color(255, 235, 170), new Color(240, 180, 60), new Color(200, 230, 255));
}
