using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.Effects.Library;

/// <summary> A black hole hanging behind the player. </summary>
public class BlackHoleAura(string name, FxPalette palette, bool jets = false) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	private readonly BlackHoleRenderer.Settings settings = new(Jets: jets);

	public override int Rarity => Terraria.ID.ItemRarityID.Red;

	public override void DrawBehind(in FxFrame f)
	{
		Vector2 center = f.Local(0f, -6f);
		float radius = 44f * (1f + 0.02f * MathF.Sin(f.Time * 1.3f));
		BlackHoleRenderer.DrawBack(f, center, radius, Palette, settings);
		BlackHoleRenderer.DrawFront(f, center, radius, Palette, settings);
	}
}

/// <summary> A slowly turning spiral galaxy. </summary>
public class GalaxyAura(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		Vector2 center = f.Local(0f, -4f);
		float t = f.Time;
		float spin = t * 0.22f;

		f.Bloom(center, Palette.EdgeAt(t * 0.05f) * 0.35f, 62f);

		Texture2D galaxy = FxTextures.Galaxy.Value;
		float scale = 104f / galaxy.Width;
		f.Glow(galaxy, center, Palette.EdgeAt(t * 0.05f) * 0.55f, spin * 0.8f + 0.5f, scale * 1.12f);
		f.Glow(galaxy, center, Palette.PrimaryAt(t * 0.05f) * 0.9f, spin, scale);
		f.Glow(galaxy, center, Palette.AccentAt(t * 0.05f) * 0.45f, spin + 1.9f, scale * 0.72f);

		f.Bloom(center, Palette.Core * 0.75f, 13f);
		f.Bloom(center, Palette.PrimaryAt(t * 0.05f) * 0.5f, 24f);

		Fx.StarField(f, center, 26, 54f, spin, Palette, 9.3f);
	}
}

/// <summary> Ghostly flames licking up the player's body. </summary>
public class SoulFlameAura(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		f.Bloom(f.Local(0f, 8f), Palette.PrimaryAt(f.Time * 0.2f) * 0.32f, 34f);
		DrawFlames(f, 30, 1f, 0);
		Fx.RisingEmbers(f, 12, Palette, 1.4f);
	}

	public override void DrawFront(in FxFrame f)
	{
		// A few translucent tongues in front of the body sell the "engulfed" look.
		DrawFlames(f, 8, 0.4f, 100);
	}

	private void DrawFlames(in FxFrame f, int count, float alpha, int seed)
	{
		Texture2D flame = FxTextures.Flame.Value;
		var origin = new Vector2(flame.Width * 0.5f, flame.Height * 0.8f);
		for (int i = 0; i < count; i++) {
			float age = FxRandom.Cycle(f.Time, i, count, 1.15f, out int gen);
			float x = FxRandom.Signed(gen, i, seed) * 11f;
			float rise = age * (34f + 14f * FxRandom.Float(gen, i, seed + 1));
			float sway = MathF.Sin(f.Time * 4f + i) * 2.5f * age;
			Vector2 p = f.Local(x + sway, 20f - rise);
			float size = MathF.Sin(age * MathF.PI) * MathHelper.Lerp(0.32f, 0.5f, FxRandom.Float(gen, i, seed + 2));
			Color color = Palette.Ramp(age * 0.85f, f.Time * 0.2f + i * 0.05f) * (alpha * (1f - age * 0.6f));
			float rotation = sway * 0.05f + (f.GravDir < 0f ? MathF.PI : 0f);
			f.Glow(flame, p, null, color, rotation, origin, new Vector2(size * 0.8f, size));
		}
	}
}

/// <summary> Radiant god rays turning slowly behind the player. </summary>
public class DivineRaysAura(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		Vector2 center = f.Local(0f, -6f);
		float t = f.Time;
		Texture2D ray = FxTextures.Ray.Value;
		var origin = new Vector2(ray.Width * 0.5f, ray.Height);

		f.Bloom(center, Palette.PrimaryAt(t * 0.1f) * 0.3f, 58f);

		for (int layer = 0; layer < 2; layer++) {
			int count = layer == 0 ? 12 : 9;
			float direction = layer == 0 ? 1f : -1.4f;
			for (int i = 0; i < count; i++) {
				float angle = i * MathHelper.TwoPi / count + t * 0.18f * direction + layer * 0.3f;
				float pulse = 0.75f + 0.25f * MathF.Sin(t * 2.2f + i * 1.7f + layer);
				float length = (layer == 0 ? 58f : 42f) * pulse / ray.Height;
				float width = (layer == 0 ? 11f : 7f) / ray.Width;
				Color color = (i % 2 == 0 ? Palette.PrimaryAt(t * 0.1f + i / (float)count) : Palette.AccentAt(t * 0.1f + i / (float)count)) * (layer == 0 ? 0.42f : 0.55f);
				f.Glow(ray, center, null, color, angle, origin, new Vector2(width, length));
			}
		}

		f.Bloom(center, Palette.Core * 0.7f, 16f);
		Fx.StarField(f, center, 12, 46f, t * 0.05f, Palette, 3.1f);
	}
}

/// <summary> A constellation drawn in the sky around the player. </summary>
public class ConstellationAura(string name, FxPalette palette, Vector2[] stars, (int, int)[] links) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public static readonly Vector2[] DipperStars = [new(-1f, -0.35f), new(-0.62f, -0.5f), new(-0.25f, -0.38f), new(0.08f, -0.25f), new(0.2f, 0.18f), new(0.75f, 0.32f), new(0.82f, -0.12f)];
	public static readonly (int, int)[] DipperLinks = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 3)];

	public static readonly Vector2[] OrionStars = [new(-0.55f, -0.95f), new(0.5f, -0.85f), new(-0.18f, -0.05f), new(0f, 0f), new(0.18f, 0.05f), new(-0.62f, 0.9f), new(0.6f, 0.82f), new(0f, -1.05f)];
	public static readonly (int, int)[] OrionLinks = [(0, 2), (1, 4), (2, 3), (3, 4), (2, 5), (4, 6), (0, 7), (7, 1)];

	public override void DrawBehind(in FxFrame f)
	{
		Vector2 center = f.Local(0f, -6f);
		float t = f.Time;
		float rotation = MathF.Sin(t * 0.25f) * 0.12f;

		Texture2D nebula = FxTextures.Nebula.Value;
		f.Glow(nebula, center, Palette.EdgeAt(t * 0.05f) * 0.22f, t * 0.03f, 110f / nebula.Width);

		Span<Vector2> points = stackalloc Vector2[stars.Length];
		for (int i = 0; i < stars.Length; i++)
			points[i] = center + (stars[i] * new Vector2(38f * f.Direction, 34f * f.GravDir)).RotatedBy(rotation);

		foreach (var (a, b) in links) {
			float pulse = 0.5f + 0.5f * MathF.Sin(t * 1.5f + a * 0.7f);
			f.Line(points[a], points[b], Palette.PrimaryAt(t * 0.1f) * (0.25f + 0.2f * pulse), 1.4f);
			f.Line(points[a], points[b], Palette.Core * (0.12f + 0.1f * pulse), 0.6f);
		}

		for (int i = 0; i < points.Length; i++) {
			float twinkle = 0.65f + 0.35f * MathF.Sin(t * (2f + i * 0.37f) + i);
			f.Bloom(points[i], Palette.PrimaryAt(t * 0.1f + i * 0.1f) * 0.4f, 7f);
			f.Sparkle(points[i], Palette.Core * twinkle, 11f + 5f * twinkle, t * 0.2f + i);
		}

		Fx.StarField(f, center, 14, 50f, 0f, Palette, 5.7f, 0.5f);
	}
}

/// <summary> Dark smoke, purple sparks and something watching from inside. </summary>
public class VoidAura(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		float t = f.Time;
		Texture2D smoke = FxTextures.Smoke.Value;
		int frameWidth = smoke.Width / 4;
		Color dark = Color.Lerp(Palette.Edge, Color.Black, 0.6f);

		f.Bloom(f.Local(0f, 0f), Palette.EdgeAt(t * 0.1f) * 0.4f, 40f);

		const int puffs = 16;
		for (int i = 0; i < puffs; i++) {
			float age = FxRandom.Cycle(t, i, puffs, 2.6f, out int gen);
			float angle = FxRandom.Float(gen, i) * MathHelper.TwoPi;
			float distance = 6f + age * 26f;
			Vector2 p = f.Local(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance * 0.7f - age * 16f + 4f);
			float alpha = MathF.Sin(age * MathF.PI) * 0.55f;
			var source = new Rectangle(frameWidth * (int)(FxRandom.Hash(gen, i) % 4), 0, frameWidth, smoke.Height);
			float scale = MathHelper.Lerp(0.35f, 0.75f, age);
			f.Draw(smoke, p, source, dark * alpha, angle + age * 1.5f, new Vector2(frameWidth, smoke.Height) * 0.5f, new Vector2(scale));
			f.Glow(smoke, p, source, Palette.PrimaryAt(t * 0.1f) * (alpha * 0.25f), angle + age * 1.5f, new Vector2(frameWidth, smoke.Height) * 0.5f, new Vector2(scale * 0.8f));
		}

		Fx.RisingEmbers(f, 14, Palette, 1.8f, spiral: true);
		DrawEyes(f);
	}

	private void DrawEyes(in FxFrame f)
	{
		Texture2D eye = FxTextures.Eye.Value;
		const int count = 3;
		for (int i = 0; i < count; i++) {
			float age = FxRandom.Cycle(f.Time, i, count, 3.7f, out int gen);
			var offset = new Vector2(FxRandom.Signed(gen, i, 3) * 34f, FxRandom.Signed(gen, i, 4) * 26f - 6f);
			if (Math.Abs(offset.X) < 14f)
				offset.X = 14f * Math.Sign(offset.X == 0f ? 1f : offset.X);

			// Open, blink once, close.
			float open = MathF.Sin(age * MathF.PI);
			float blink = MathF.Abs(age - 0.55f) < 0.03f ? 0.1f : 1f;
			Vector2 p = f.Center + offset;
			f.Bloom(p, Palette.AccentAt(f.Time * 0.1f) * (0.35f * open), 9f);
			f.Glow(eye, p, null, Palette.AccentAt(f.Time * 0.1f) * open, 0f, eye.Size() * 0.5f, new Vector2(1f, open * blink));
		}
	}
}

/// <summary> Petals or leaves swirling around the player. </summary>
public class PetalStormAura(string name, FxPalette palette, int petalKind) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f) => DrawPetals(f, front: false);

	public override void DrawFront(in FxFrame f) => DrawPetals(f, front: true);

	private void DrawPetals(in FxFrame f, bool front)
	{
		Texture2D petals = FxTextures.Petals.Value;
		int size = petals.Height;
		var source = new Rectangle(petalKind * size, 0, size, size);
		var origin = new Vector2(size * 0.5f);
		const int count = 24;

		for (int i = 0; i < count; i++) {
			float angle = FxRandom.Float(i, 1) * MathHelper.TwoPi + f.Time * MathHelper.Lerp(1.1f, 1.8f, FxRandom.Float(i, 2));
			float depth = MathF.Sin(angle);
			if (depth >= 0f != front)
				continue;

			float radius = MathHelper.Lerp(17f, 34f, FxRandom.Float(i, 3));
			float height = FxRandom.Cycle(f.Time, i, count, 4.5f, out int gen);
			float y = 24f - height * 56f + MathF.Sin(f.Time * 2f + i) * 3f;
			Vector2 p = f.Local(MathF.Cos(angle) * radius, y + depth * 5f);
			float fade = MathF.Min(1f, MathF.Min(height * 6f, (1f - height) * 4f));
			float flip = MathF.Cos(f.Time * 3.5f + i * 1.3f);
			Color tint = Color.Lerp(Palette.PrimaryAt(f.Time * 0.1f + i * 0.04f), Palette.AccentAt(f.Time * 0.1f), FxRandom.Float(gen, i, 4) * 0.6f);
			Color lit = Fx.Lit(f, p, tint) * (fade * (front ? 1f : 0.8f));
			float scale = MathHelper.Lerp(0.55f, 0.9f, FxRandom.Float(i, 5)) * (front ? 1.05f : 0.85f);
			f.Draw(petals, p, source, lit, f.Time * 2f + i, origin, new Vector2(scale * flip, scale));
		}
	}
}

/// <summary> Swirling snow and frosty mist. </summary>
public class BlizzardAura(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		Texture2D smoke = FxTextures.Smoke.Value;
		int frameWidth = smoke.Width / 4;
		for (int i = 0; i < 6; i++) {
			float angle = i * MathHelper.TwoPi / 6f + f.Time * 0.35f;
			Vector2 p = f.Local(MathF.Cos(angle) * 20f, MathF.Sin(angle) * 10f + 6f);
			var source = new Rectangle(frameWidth * (i % 4), 0, frameWidth, smoke.Height);
			f.Glow(smoke, p, source, Palette.PrimaryAt(f.Time * 0.1f) * 0.16f, angle, new Vector2(frameWidth, smoke.Height) * 0.5f, new Vector2(0.9f));
		}
		DrawFlakes(f, front: false);
	}

	public override void DrawFront(in FxFrame f) => DrawFlakes(f, front: true);

	private void DrawFlakes(in FxFrame f, bool front)
	{
		Texture2D flake = FxTextures.Snowflake.Value;
		const int count = 26;
		for (int i = 0; i < count; i++) {
			float angle = FxRandom.Float(i, 1) * MathHelper.TwoPi + f.Time * MathHelper.Lerp(1.4f, 2.4f, FxRandom.Float(i, 2));
			float depth = MathF.Sin(angle);
			if (depth >= 0f != front)
				continue;

			float radius = MathHelper.Lerp(15f, 36f, FxRandom.Float(i, 3));
			float fall = FxRandom.Cycle(f.Time, i, count, 3.2f, out _);
			Vector2 p = f.Local(MathF.Cos(angle) * radius, -30f + fall * 56f + depth * 4f);
			float fade = MathF.Min(1f, MathF.Min(fall * 5f, (1f - fall) * 5f));
			float scale = MathHelper.Lerp(0.2f, 0.42f, FxRandom.Float(i, 4)) * (front ? 1.1f : 0.85f);
			f.Glow(flake, p, null, Palette.Core * (fade * 0.9f), f.Time * 1.5f + i, flake.Size() * 0.5f, new Vector2(scale));
			if (i % 4 == 0)
				f.Bloom(p, Palette.PrimaryAt(f.Time * 0.1f) * (0.3f * fade), 5f);
		}
	}
}

/// <summary> Crackling lightning arcing around the player. </summary>
public class StormAura(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		float t = f.Time;
		f.Bloom(f.Center, Palette.EdgeAt(t * 0.1f) * (0.25f + 0.1f * MathF.Sin(t * 13f)), 40f);

		// A new set of bolts every few frames, seeded from the time bucket.
		int bucket = (int)(t * 9f);
		for (int bolt = 0; bolt < 3; bolt++) {
			if (FxRandom.Float(bucket, bolt, 1) < 0.35f)
				continue;

			float angle = FxRandom.Float(bucket, bolt, 2) * MathHelper.TwoPi;
			Vector2 start = f.Local(FxRandom.Signed(bucket, bolt, 3) * 6f, FxRandom.Signed(bucket, bolt, 4) * 12f);
			Vector2 end = start + angle.ToRotationVector2() * MathHelper.Lerp(26f, 46f, FxRandom.Float(bucket, bolt, 5));
			Fx.Lightning(f, start, end, bucket * 7 + bolt, Palette, 1f);
		}

		Fx.RisingEmbers(f, 10, Palette, 0.9f);
	}
}

/// <summary> Bubbles drifting up from the player. </summary>
public class BubbleAura(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		float t = f.Time;
		f.Bloom(f.Local(0f, 4f), Palette.PrimaryAt(t * 0.1f) * (0.22f + 0.08f * MathF.Sin(t * 1.7f)), 42f);

		Texture2D bubble = FxTextures.Bubble.Value;
		const int count = 18;
		for (int i = 0; i < count; i++) {
			float age = FxRandom.Cycle(t, i, count, 3f, out int gen);
			float x = FxRandom.Signed(gen, i) * 24f + MathF.Sin(t * 2.5f + i * 1.9f) * 3f;
			Vector2 p = f.Local(x, 22f - age * 60f);
			float size = MathHelper.Lerp(0.18f, 0.42f, FxRandom.Float(gen, i, 1));
			float pop = age > 0.92f ? 1f + (age - 0.92f) * 8f : 1f;
			float alpha = MathF.Min(1f, age * 8f) * (age > 0.92f ? (1f - age) * 12.5f : 1f);
			f.Draw(bubble, p, null, Fx.Lit(f, p, Palette.Core) * (alpha * 0.85f), 0f, bubble.Size() * 0.5f, new Vector2(size * pop));
			f.Glow(bubble, p, null, Palette.PrimaryAt(t * 0.1f) * (alpha * 0.25f), 0f, bubble.Size() * 0.5f, new Vector2(size * pop * 1.15f));
		}
	}
}

/// <summary> Little hearts floating up. </summary>
public class HeartsAura(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		Texture2D heart = FxTextures.Heart.Value;
		const int count = 12;
		for (int i = 0; i < count; i++) {
			float age = FxRandom.Cycle(f.Time, i, count, 2.8f, out int gen);
			float x = FxRandom.Signed(gen, i) * 22f + MathF.Sin(f.Time * 2.2f + i) * 5f * age;
			Vector2 p = f.Local(x, 16f - age * 52f);
			float alpha = MathF.Sin(age * MathF.PI);
			float beat = 1f + 0.12f * MathF.Max(0f, MathF.Sin(f.Time * 9f + i));
			float scale = MathHelper.Lerp(0.3f, 0.5f, FxRandom.Float(gen, i, 1)) * beat;
			Color color = Color.Lerp(Palette.PrimaryAt(f.Time * 0.1f), Palette.AccentAt(f.Time * 0.1f), FxRandom.Float(gen, i, 2));
			float rotation = MathF.Sin(f.Time * 2.2f + i) * 0.25f;
			f.Bloom(p, color * (alpha * 0.4f), 9f * scale * 2f);
			f.Glow(heart, p, null, color * alpha, rotation, heart.Size() * 0.5f, new Vector2(scale));
			f.Glow(heart, p, null, Palette.Core * (alpha * 0.5f), rotation, heart.Size() * 0.5f, new Vector2(scale * 0.55f));
		}
		Fx.StarField(f, f.Local(0f, -4f), 8, 32f, 0f, Palette, 8.1f, 0.6f);
	}
}

/// <summary> Music notes dancing up in rainbow colors. </summary>
public class MelodyAura(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		Texture2D notes = FxTextures.Notes.Value;
		int size = notes.Height;
		const int count = 10;
		for (int i = 0; i < count; i++) {
			float age = FxRandom.Cycle(f.Time, i, count, 3.2f, out int gen);
			float side = FxRandom.Float(gen, i) < 0.5f ? -1f : 1f;
			float x = side * (12f + age * 20f) + MathF.Sin(f.Time * 3f + i) * 4f;
			Vector2 p = f.Local(x, 4f - age * 46f);
			float alpha = MathF.Sin(age * MathF.PI);
			var source = new Rectangle((int)(FxRandom.Hash(gen, i, 1) % 2) * size, 0, size, size);
			Color color = Palette.PrimaryAt(f.Time * 0.15f + i / (float)count);
			float rotation = MathF.Sin(f.Time * 4f + i) * 0.3f;
			f.Bloom(p, color * (alpha * 0.35f), 10f);
			f.Glow(notes, p, source, color * alpha, rotation, new Vector2(size * 0.5f), new Vector2(0.55f));
		}
	}
}

/// <summary> Soft colored clouds of a nebula, slowly churning. </summary>
public class NebulaAura(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Aura, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		Vector2 center = f.Local(0f, -4f);
		float t = f.Time;
		Texture2D nebula = FxTextures.Nebula.Value;

		for (int i = 0; i < 7; i++) {
			float angle = i * MathHelper.TwoPi / 7f + t * (i % 2 == 0 ? 0.12f : -0.09f);
			float distance = 14f + 10f * MathF.Sin(t * 0.4f + i * 1.3f);
			Vector2 p = center + angle.ToRotationVector2() * distance;
			Color color = i % 3 == 0 ? Palette.AccentAt(t * 0.05f + i * 0.1f) : Palette.PrimaryAt(t * 0.05f + i * 0.1f);
			float scale = (56f + 10f * MathF.Sin(t * 0.7f + i)) / nebula.Width;
			f.Glow(nebula, p, color * 0.24f, angle * 0.5f + i, scale);
		}

		f.Bloom(center, Palette.Core * 0.25f, 18f);
		Fx.StarField(f, center, 20, 52f, t * 0.04f, Palette, 2.3f);
	}
}
