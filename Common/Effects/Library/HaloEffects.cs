using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.Effects.Library;

/// <summary> Base for everything floating above the head. </summary>
public abstract class HaloEffect(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Halo, palette)
{
	/// <summary> Hover point above the head, bobbing gently. </summary>
	protected static Vector2 Anchor(in FxFrame f, float height = 9f)
	{
		return f.Head + new Vector2(0f, (-height + MathF.Sin(f.Time * 2.1f) * 1.3f) * f.GravDir);
	}

	/// <summary> Draws a squashed ring (a halo seen slightly from above). </summary>
	protected static void Ring(in FxFrame f, Texture2D texture, Vector2 center, Color color, float radiusX, float radiusY, float rotation = 0f, float textureRadius = 0.44f, bool additive = true)
	{
		var scale = new Vector2(radiusX, radiusY) / (texture.Width * textureRadius);
		if (additive)
			f.Glow(texture, center, color, rotation, scale);
		else
			f.Draw(texture, center, color, rotation, scale);
	}
}

/// <summary> A classic shining halo. </summary>
public class AngelHalo(string name, FxPalette palette) : HaloEffect(name, palette)
{
	public override void DrawFront(in FxFrame f)
	{
		Vector2 center = Anchor(f);
		float t = f.Time;
		float pulse = 0.85f + 0.15f * MathF.Sin(t * 3f);

		f.Bloom(center, Palette.PrimaryAt(t * 0.1f) * (0.35f * pulse), 18f);
		Ring(f, FxTextures.Ring.Value, center, Palette.PrimaryAt(t * 0.1f) * (0.9f * pulse), 11f, 3.6f);
		Ring(f, FxTextures.RingThin.Value, center, Palette.Core * pulse, 9.5f, 3f, textureRadius: 0.47f);

		for (int i = 0; i < 3; i++) {
			float angle = t * 1.6f + i * MathHelper.TwoPi / 3f;
			Vector2 p = center + Fx.Ellipse(angle, 9.5f, 3f, 0f, out float depth);
			f.Sparkle(p, Palette.Core * (0.55f + 0.45f * depth), 7f + 3f * depth, t);
		}
	}
}

/// <summary> A dark, broken halo wreathed in flames. </summary>
public class FallenHalo(string name, FxPalette palette) : HaloEffect(name, palette)
{
	public override void DrawFront(in FxFrame f)
	{
		Vector2 center = Anchor(f);
		float t = f.Time;
		Texture2D flame = FxTextures.Flame.Value;
		var origin = new Vector2(flame.Width * 0.5f, flame.Height * 0.8f);

		f.Bloom(center, Palette.EdgeAt(t * 0.1f) * 0.45f, 16f);

		// Flames behind the ring.
		for (int i = 0; i < 9; i++) {
			float angle = i * MathHelper.TwoPi / 9f + t * 0.5f;
			Vector2 p = center + Fx.Ellipse(angle, 10f, 3.3f, 0f, out float depth);
			if (depth >= 0f)
				continue;
			float flicker = 0.7f + 0.3f * MathF.Sin(t * 11f + i * 2.3f);
			f.Glow(flame, p, null, Palette.PrimaryAt(t * 0.1f) * (0.8f * flicker), (f.GravDir < 0f ? MathF.PI : 0f), origin, new Vector2(0.16f, 0.2f * flicker));
		}

		Ring(f, FxTextures.Ring.Value, center, Color.Black * 0.9f, 11.5f, 3.8f, additive: false);
		Ring(f, FxTextures.RingThin.Value, center, Palette.PrimaryAt(t * 0.1f) * 0.7f, 10f, 3.3f, textureRadius: 0.47f);

		for (int i = 0; i < 9; i++) {
			float angle = i * MathHelper.TwoPi / 9f + t * 0.5f;
			Vector2 p = center + Fx.Ellipse(angle, 10f, 3.3f, 0f, out float depth);
			if (depth < 0f)
				continue;
			float flicker = 0.7f + 0.3f * MathF.Sin(t * 11f + i * 2.3f);
			f.Glow(flame, p, null, Palette.Ramp(0.35f, t * 0.1f) * flicker, (f.GravDir < 0f ? MathF.PI : 0f), origin, new Vector2(0.18f, 0.24f * flicker));
		}

		Fx.RisingEmbers(f, 4, Palette, 0.8f);
	}
}

/// <summary> A tiny black hole spinning above the head. </summary>
public class SingularityHalo(string name, FxPalette palette) : HaloEffect(name, palette)
{
	private static readonly BlackHoleRenderer.Settings settings = new(Clumps: 24, Infall: 6, Tilt: 0.34f, Angle: -0.1f);

	public override int Rarity => Terraria.ID.ItemRarityID.Red;

	public override void DrawFront(in FxFrame f)
	{
		Vector2 center = Anchor(f, 15f);
		BlackHoleRenderer.DrawBack(f, center, 15f, Palette, settings);
		BlackHoleRenderer.DrawFront(f, center, 15f, Palette, settings);
	}
}

/// <summary> A circlet of stars. </summary>
public class StarCrownHalo(string name, FxPalette palette) : HaloEffect(name, palette)
{
	public override void DrawBehind(in FxFrame f) => DrawStars(f, front: false);

	public override void DrawFront(in FxFrame f)
	{
		Vector2 center = Anchor(f, 7f);
		Ring(f, FxTextures.RingThin.Value, center, Palette.PrimaryAt(f.Time * 0.1f) * 0.35f, 11f, 3.5f, textureRadius: 0.47f);
		DrawStars(f, front: true);
	}

	private void DrawStars(in FxFrame f, bool front)
	{
		Vector2 center = Anchor(f, 7f);
		const int count = 7;
		for (int i = 0; i < count; i++) {
			float angle = i * MathHelper.TwoPi / count + f.Time * 0.9f;
			Vector2 p = center + Fx.Ellipse(angle, 11f, 3.5f, 0f, out float depth);
			if (depth >= 0f != front)
				continue;
			float twinkle = 0.75f + 0.25f * MathF.Sin(f.Time * 4f + i * 1.9f);
			float size = (8f + 4f * depth) * twinkle;
			f.Bloom(p, Palette.PrimaryAt(f.Time * 0.1f + i / (float)count) * 0.35f, 5f);
			f.Sparkle(p, Palette.Core * (0.6f + 0.4f * depth), size, f.Time * 0.5f + i);
		}
	}
}

/// <summary> Glowing runes turning in a ring above the head. </summary>
public class RuneHalo(string name, FxPalette palette) : HaloEffect(name, palette)
{
	public override void DrawBehind(in FxFrame f) => DrawRunes(f, front: false);

	public override void DrawFront(in FxFrame f)
	{
		Vector2 center = Anchor(f, 8f);
		f.Bloom(center, Palette.PrimaryAt(f.Time * 0.1f) * 0.25f, 16f);
		Ring(f, FxTextures.RingThin.Value, center, Palette.PrimaryAt(f.Time * 0.1f) * 0.55f, 14f, 4.2f, textureRadius: 0.47f);
		DrawRunes(f, front: true);
	}

	private void DrawRunes(in FxFrame f, bool front)
	{
		Vector2 center = Anchor(f, 8f);
		Texture2D runes = FxTextures.Runes.Value;
		int size = runes.Height;
		const int count = 8;
		for (int i = 0; i < count; i++) {
			float angle = i * MathHelper.TwoPi / count - f.Time * 0.7f;
			Vector2 p = center + Fx.Ellipse(angle, 14f, 4.2f, 0f, out float depth);
			if (depth >= 0f != front)
				continue;
			// Glyphs face outwards, so they get thinner towards the sides of the ring.
			float facing = MathF.Max(0.25f, MathF.Abs(MathF.Sin(angle)));
			float pulse = 0.7f + 0.3f * MathF.Sin(f.Time * 3f + i);
			var source = new Rectangle(i % (runes.Width / size) * size, 0, size, size);
			Color color = (front ? Palette.Core : Palette.PrimaryAt(f.Time * 0.1f)) * (pulse * (front ? 1f : 0.6f));
			f.Glow(runes, p, source, color, 0f, new Vector2(size * 0.5f), new Vector2(0.45f * facing, 0.45f));
		}
	}
}

/// <summary> A small ringed planet with moons, hovering above the head. </summary>
public class PlanetHalo(string name, FxPalette palette) : HaloEffect(name, palette)
{
	public override void DrawFront(in FxFrame f)
	{
		Vector2 center = Anchor(f, 15f);
		float t = f.Time;
		Texture2D ring = FxTextures.Ring.Value;
		const float tilt = -0.32f;
		var ringScale = new Vector2(16f, 5f) / (ring.Width * 0.44f);
		int half = ring.Height / 2;

		f.Bloom(center, Palette.PrimaryAt(t * 0.1f) * 0.25f, 20f);
		DrawMoons(f, center, front: false);

		// Back half of the ring, the planet, then the front half over it.
		f.Glow(ring, center, new Rectangle(0, 0, ring.Width, half), Palette.AccentAt(t * 0.1f) * 0.8f, tilt, new Vector2(ring.Width * 0.5f, half), ringScale);
		Texture2D planet = FxTextures.Planet.Value;
		f.Draw(planet, center, null, Palette.PrimaryAt(t * 0.1f), t * 0.3f, planet.Size() * 0.5f, new Vector2(15f / planet.Width));
		f.Glow(planet, center, null, Palette.Core * 0.25f, t * 0.3f, planet.Size() * 0.5f, new Vector2(15f / planet.Width));
		f.Glow(ring, center, new Rectangle(0, half, ring.Width, half), Palette.AccentAt(t * 0.1f), tilt, new Vector2(ring.Width * 0.5f, 0f), ringScale);

		DrawMoons(f, center, front: true);
	}

	private void DrawMoons(in FxFrame f, Vector2 center, bool front)
	{
		Texture2D orb = FxTextures.Orb.Value;
		for (int i = 0; i < 2; i++) {
			float angle = f.Time * (1.6f - i * 0.5f) + i * 2f;
			Vector2 p = center + Fx.Ellipse(angle, 15f + i * 5f, 4f + i * 1.5f, -0.32f, out float depth);
			if (depth >= 0f != front)
				continue;
			f.Draw(orb, p, null, Color.Lerp(Palette.Core, Palette.PrimaryAt(0f), 0.3f), 0f, orb.Size() * 0.5f, new Vector2((3f + i) / orb.Width * 2f));
		}
	}
}

/// <summary> A crown of flickering flames. </summary>
public class FlameCrownHalo(string name, FxPalette palette) : HaloEffect(name, palette)
{
	public override void DrawBehind(in FxFrame f) => DrawFlames(f, front: false);

	public override void DrawFront(in FxFrame f)
	{
		f.Bloom(Anchor(f, 5f), Palette.PrimaryAt(f.Time * 0.1f) * 0.3f, 14f);
		DrawFlames(f, front: true);
	}

	private void DrawFlames(in FxFrame f, bool front)
	{
		Vector2 center = Anchor(f, 5f);
		Texture2D flame = FxTextures.Flame.Value;
		var origin = new Vector2(flame.Width * 0.5f, flame.Height * 0.8f);
		const int count = 9;
		for (int i = 0; i < count; i++) {
			float angle = i * MathHelper.TwoPi / count + f.Time * 0.6f;
			Vector2 p = center + Fx.Ellipse(angle, 9.5f, 3f, 0f, out float depth);
			if (depth >= 0f != front)
				continue;
			float flicker = 0.75f + 0.25f * MathF.Sin(f.Time * 12f + i * 1.7f);
			float size = (0.17f + 0.04f * depth) * flicker;
			float rotation = MathF.Sin(f.Time * 3f + i) * 0.1f + (f.GravDir < 0f ? MathF.PI : 0f);
			f.Glow(flame, p, null, Palette.PrimaryAt(f.Time * 0.1f) * (front ? 0.9f : 0.55f), rotation, origin, new Vector2(size * 0.85f, size));
			f.Glow(flame, p, null, Palette.Core * (front ? 0.6f : 0.3f), rotation, origin, new Vector2(size * 0.45f, size * 0.6f));
		}
	}
}
