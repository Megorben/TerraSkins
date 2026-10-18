using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace TerraSkins.Common.Effects;

/// <summary> Textures shared by all effects. Most are white so they can be tinted with any palette color. </summary>
public class FxTextures : ModSystem
{
	private const string Folder = "TerraSkins/Assets/Textures/FX/";

	public static Asset<Texture2D> Glow { get; private set; }
	public static Asset<Texture2D> GlowSharp { get; private set; }
	public static Asset<Texture2D> Disc { get; private set; }
	public static Asset<Texture2D> Ring { get; private set; }
	public static Asset<Texture2D> RingThin { get; private set; }
	public static Asset<Texture2D> Star { get; private set; }
	public static Asset<Texture2D> Streak { get; private set; }
	public static Asset<Texture2D> Smoke { get; private set; }
	public static Asset<Texture2D> Disk { get; private set; }
	public static Asset<Texture2D> Galaxy { get; private set; }
	public static Asset<Texture2D> RuneCircle { get; private set; }
	public static Asset<Texture2D> Runes { get; private set; }
	public static Asset<Texture2D> Ray { get; private set; }
	public static Asset<Texture2D> Orb { get; private set; }
	public static Asset<Texture2D> Crystal { get; private set; }
	public static Asset<Texture2D> Blade { get; private set; }
	public static Asset<Texture2D> Flame { get; private set; }
	public static Asset<Texture2D> Bubble { get; private set; }
	public static Asset<Texture2D> Petals { get; private set; }
	public static Asset<Texture2D> Snowflake { get; private set; }
	public static Asset<Texture2D> Heart { get; private set; }
	public static Asset<Texture2D> Notes { get; private set; }
	public static Asset<Texture2D> Feather { get; private set; }
	public static Asset<Texture2D> WingFeathered { get; private set; }
	public static Asset<Texture2D> WingCrystal { get; private set; }
	public static Asset<Texture2D> Butterfly { get; private set; }
	public static Asset<Texture2D> Eye { get; private set; }
	public static Asset<Texture2D> Shard { get; private set; }
	public static Asset<Texture2D> Nebula { get; private set; }
	public static Asset<Texture2D> Planet { get; private set; }
	public static Asset<Texture2D> Pixel { get; private set; }

	public override void Load()
	{
		if (Main.dedServ)
			return;

		Glow = Request("Glow");
		GlowSharp = Request("GlowSharp");
		Disc = Request("Disc");
		Ring = Request("Ring");
		RingThin = Request("RingThin");
		Star = Request("Star");
		Streak = Request("Streak");
		Smoke = Request("Smoke");
		Disk = Request("Disk");
		Galaxy = Request("Galaxy");
		RuneCircle = Request("RuneCircle");
		Runes = Request("Runes");
		Ray = Request("Ray");
		Orb = Request("Orb");
		Crystal = Request("Crystal");
		Blade = Request("Blade");
		Flame = Request("Flame");
		Bubble = Request("Bubble");
		Petals = Request("Petals");
		Snowflake = Request("Snowflake");
		Heart = Request("Heart");
		Notes = Request("Notes");
		Feather = Request("Feather");
		WingFeathered = Request("WingFeathered");
		WingCrystal = Request("WingCrystal");
		Butterfly = Request("Butterfly");
		Eye = Request("Eye");
		Shard = Request("Shard");
		Nebula = Request("Nebula");
		Planet = Request("Planet");
		Pixel = Request("Pixel");
	}

	public override void Unload()
	{
		Glow = GlowSharp = Disc = Ring = RingThin = Star = Streak = Smoke = Disk = Galaxy = RuneCircle = Runes = Ray = Orb = Crystal = Blade = null;
		Flame = Bubble = Petals = Snowflake = Heart = Notes = Feather = WingFeathered = WingCrystal = Butterfly = Eye = Shard = Nebula = Planet = Pixel = null;
	}

	private static Asset<Texture2D> Request(string name) => ModContent.Request<Texture2D>(Folder + name);
}
