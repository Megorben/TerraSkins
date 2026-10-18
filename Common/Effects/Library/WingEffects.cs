using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.Effects.Library;

/// <summary>
/// Ethereal wings drawn by code. They only exist visually: worn in the wardrobe they never grant flight
/// (the wing slot of the player stays empty), so they give no advantage at all.
/// </summary>
public abstract class EtherealWings(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Wings, palette)
{
	public override int Rarity => Terraria.ID.ItemRarityID.Yellow;

	/// <summary> Root of both wings, on the player's upper back. </summary>
	protected static Vector2 Anchor(in FxFrame f) => f.Local(-3f * f.Direction, -7f);

	/// <summary> Flap angle in radians. Flaps harder while airborne. </summary>
	protected static float Flap(in FxFrame f, float idleSpeed = 2.2f)
	{
		bool airborne = f.Player.velocity.Y != 0f && !f.Player.mount.Active;
		float speed = airborne ? idleSpeed * 3.2f : idleSpeed;
		float amplitude = airborne ? 0.42f : 0.14f;
		return MathF.Sin(f.Time * speed) * amplitude + (airborne ? 0.05f : 0.12f);
	}

	public override void DrawWings(in FxFrame f)
	{
		float flap = Flap(f);
		// The wing on the side the player faces is further away from the viewer.
		DrawWing(f, side: -f.Direction, flap, near: true);
		DrawWing(f, side: f.Direction, flap, near: false);
	}

	/// <param name="side">1 for the wing spreading towards +X on screen, -1 for the other one.</param>
	protected abstract void DrawWing(in FxFrame f, int side, float flap, bool near);

	/// <summary> Rotates a point given in "right wing" space (+X outwards, -Y up) for the given side and flap. </summary>
	protected static Vector2 WingPoint(in FxFrame f, Vector2 local, int side, float flap)
	{
		var p = local.RotatedBy(-flap);
		return Anchor(f) + new Vector2(p.X * side, p.Y * f.GravDir);
	}

	/// <summary> Draws a wing texture whose root is at <paramref name="root"/> (in texture pixels) and that extends to the right. </summary>
	protected static void DrawWingTexture(in FxFrame f, Texture2D texture, Vector2 root, int side, float flap, float scale, Color color, bool additive)
	{
		var effects = SpriteEffects.None;
		Vector2 origin = root;
		float rotation = -flap * side;
		if (side < 0) {
			effects |= SpriteEffects.FlipHorizontally;
			origin.X = texture.Width - root.X;
		}
		if (f.GravDir < 0f) {
			effects |= SpriteEffects.FlipVertically;
			origin.Y = texture.Height - root.Y;
			rotation = -rotation;
		}

		if (additive)
			f.Glow(texture, Anchor(f), null, color, rotation, origin, new Vector2(scale), effects);
		else
			f.Draw(texture, Anchor(f), null, color, rotation, origin, new Vector2(scale), effects);
	}
}

/// <summary> Large feathered wings of light. </summary>
public class SeraphWings(string name, FxPalette palette) : EtherealWings(name, palette)
{
	private static readonly Vector2 Root = new(14f, 114f);

	protected override void DrawWing(in FxFrame f, int side, float flap, bool near)
	{
		Texture2D wing = FxTextures.WingFeathered.Value;
		float scale = near ? 0.46f : 0.4f;
		float alpha = near ? 1f : 0.75f;
		float t = f.Time;

		DrawWingTexture(f, wing, Root, side, flap, scale * 1.07f, Palette.PrimaryAt(t * 0.1f) * (0.35f * alpha), additive: true);
		DrawWingTexture(f, wing, Root, side, flap, scale, Color.Lerp(Palette.Core, Palette.PrimaryAt(t * 0.1f), 0.3f) * (0.82f * alpha), additive: false);
		DrawWingTexture(f, wing, Root, side, flap, scale, Palette.Core * (0.25f * alpha), additive: true);

		// Sparkles along the leading edge.
		for (int i = 0; i < 3; i++) {
			float k = 0.45f + i * 0.25f;
			Vector2 edge = WingPoint(f, new Vector2(104f, -100f) * k * scale, side, flap);
			float twinkle = 0.5f + 0.5f * MathF.Sin(t * 3f + i * 2f + side);
			f.Sparkle(edge, Palette.Core * (twinkle * alpha), 9f * twinkle);
		}

		if (near)
			DrawFallingFeathers(f, side, flap);
	}

	private void DrawFallingFeathers(in FxFrame f, int side, float flap)
	{
		Texture2D feather = FxTextures.Feather.Value;
		for (int i = 0; i < 3; i++) {
			float age = FxRandom.Cycle(f.Time, i, 3, 3.4f, out int gen);
			var start = new Vector2(MathHelper.Lerp(30f, 90f, FxRandom.Float(gen, i, side + 5)), -MathHelper.Lerp(10f, 60f, FxRandom.Float(gen, i, 9))) * 0.42f;
			Vector2 p = WingPoint(f, start, FxRandom.Float(gen, i, 3) < 0.5f ? -1 : 1, flap) + new Vector2(MathF.Sin(age * 6f + i) * 6f, age * 34f * f.GravDir);
			float alpha = MathF.Sin(age * MathF.PI) * 0.8f;
			f.Glow(feather, p, null, Palette.Core * alpha, MathF.Sin(age * 5f + i) * 0.8f, feather.Size() * 0.5f, new Vector2(0.45f));
		}
	}
}

/// <summary> Writhing tendrils of the void. </summary>
public class VoidTendrilWings(string name, FxPalette palette) : EtherealWings(name, palette)
{
	protected override void DrawWing(in FxFrame f, int side, float flap, bool near)
	{
		Texture2D disc = FxTextures.Disc.Value;
		Texture2D glow = FxTextures.GlowSharp.Value;
		Color dark = Color.Lerp(Palette.Edge, Color.Black, 0.65f) * (near ? 1f : 0.85f);
		const int tendrils = 5;
		const int segments = 10;

		for (int j = 0; j < tendrils; j++) {
			float baseAngle = MathHelper.Lerp(-0.25f, 1.15f, j / (float)(tendrils - 1));
			float length = (near ? 5.2f : 4.4f) * (1f - MathF.Abs(j - 2) * 0.08f);
			Vector2 local = Vector2.Zero;
			float angle = baseAngle;

			for (int k = 0; k < segments; k++) {
				angle += 0.06f + MathF.Sin(f.Time * 2.4f + j * 1.1f + k * 0.55f) * 0.11f;
				local += new Vector2(MathF.Cos(angle), -MathF.Sin(angle)) * length;
				Vector2 p = WingPoint(f, local, side, flap * 0.6f);
				float radius = MathHelper.Lerp(4.4f, 1.1f, k / (float)(segments - 1));
				f.Draw(disc, p, dark, 0f, new Vector2(radius * 2f / disc.Width));
				f.Glow(glow, p, Palette.PrimaryAt(f.Time * 0.1f + k * 0.03f) * (0.35f * (1f - k / (float)segments)), 0f, radius * 3f / glow.Width);
				if (k == segments - 1)
					f.Sparkle(p, Palette.AccentAt(f.Time * 0.1f) * (0.6f + 0.4f * MathF.Sin(f.Time * 5f + j)), 8f);
			}
		}
	}
}

/// <summary> Shimmering crystal butterfly wings. </summary>
public class CrystalWings(string name, FxPalette palette) : EtherealWings(name, palette)
{
	private static readonly Vector2 Root = new(10f, 64f);

	protected override void DrawWing(in FxFrame f, int side, float flap, bool near)
	{
		Texture2D wing = FxTextures.WingCrystal.Value;
		float scale = near ? 0.42f : 0.36f;
		float alpha = near ? 1f : 0.75f;
		float t = f.Time;
		float shimmer = 0.5f + 0.5f * MathF.Sin(t * 2.6f + side);
		flap *= 0.7f;

		DrawWingTexture(f, wing, Root, side, flap, scale * 1.06f, Palette.PrimaryAt(t * 0.15f) * (0.35f * alpha), additive: true);
		DrawWingTexture(f, wing, Root, side, flap, scale, Palette.PrimaryAt(t * 0.15f) * (0.7f * alpha), additive: false);
		DrawWingTexture(f, wing, Root, side, flap, scale, Palette.AccentAt(t * 0.15f + 0.3f) * (0.35f * shimmer * alpha), additive: true);

		for (int i = 0; i < 2; i++) {
			float glint = MathF.Max(0f, MathF.Sin(t * 1.7f + i * 2.5f + side * 1.3f) - 0.85f) * 6.6f;
			if (glint <= 0f)
				continue;
			Vector2 p = WingPoint(f, new Vector2(60f + i * 25f, -20f + i * 30f) * scale, side, flap);
			f.Sparkle(p, Palette.Core * glint, 14f * glint);
		}
	}
}

/// <summary> Wings of living fire. </summary>
public class PhoenixWings(string name, FxPalette palette) : EtherealWings(name, palette)
{
	private static readonly Vector2 Root = new(14f, 114f);

	protected override void DrawWing(in FxFrame f, int side, float flap, bool near)
	{
		Texture2D wing = FxTextures.WingFeathered.Value;
		Texture2D flame = FxTextures.Flame.Value;
		float scale = near ? 0.44f : 0.38f;
		float alpha = near ? 1f : 0.7f;
		float t = f.Time;
		float flicker = 0.85f + 0.15f * MathF.Sin(t * 9f + side);

		DrawWingTexture(f, wing, Root, side, flap, scale * 1.08f, Palette.EdgeAt(t * 0.1f) * (0.5f * alpha * flicker), additive: true);
		DrawWingTexture(f, wing, Root, side, flap, scale, Palette.PrimaryAt(t * 0.1f) * (0.75f * alpha * flicker), additive: true);
		DrawWingTexture(f, wing, Root, side, flap, scale * 0.8f, Palette.Core * (0.4f * alpha), additive: true);

		// Flames peeling off the wing.
		var origin = new Vector2(flame.Width * 0.5f, flame.Height * 0.8f);
		const int count = 9;
		for (int i = 0; i < count; i++) {
			float age = FxRandom.Cycle(t, i, count, 0.9f, out int gen);
			float along = FxRandom.Float(gen, i, side + 3);
			float bone = FxRandom.Float(gen, i, 7);
			var local = new Vector2(100f, -100f + bone * 70f) * along * scale;
			Vector2 p = WingPoint(f, local, side, flap) + new Vector2(0f, -age * 14f * f.GravDir);
			float size = MathF.Sin(age * MathF.PI) * 0.28f;
			f.Glow(flame, p, null, Palette.Ramp(age, t * 0.1f) * (alpha * (1f - age)), f.GravDir < 0f ? MathF.PI : 0f, origin, new Vector2(size * 0.8f, size));
		}
	}
}

/// <summary> Wings traced by stars and the lines between them. </summary>
public class ConstellationWings(string name, FxPalette palette) : EtherealWings(name, palette)
{
	private static readonly float[] BoneAngles = [0.25f, 0.7f, 1.15f];
	private static readonly float[] BoneLengths = [44f, 54f, 38f];

	protected override void DrawWing(in FxFrame f, int side, float flap, bool near)
	{
		float alpha = near ? 1f : 0.7f;
		float scale = near ? 1f : 0.88f;
		float t = f.Time;
		Span<Vector2> tips = stackalloc Vector2[BoneAngles.Length];

		for (int b = 0; b < BoneAngles.Length; b++) {
			Vector2 previous = WingPoint(f, Vector2.Zero, side, flap);
			for (int k = 1; k <= 3; k++) {
				float distance = BoneLengths[b] * scale * k / 3f;
				var local = new Vector2(MathF.Cos(BoneAngles[b]), -MathF.Sin(BoneAngles[b])) * distance;
				Vector2 p = WingPoint(f, local, side, flap);
				f.Line(previous, p, Palette.PrimaryAt(t * 0.1f) * (0.35f * alpha), 1.3f);
				float twinkle = 0.6f + 0.4f * MathF.Sin(t * 3f + b * 1.3f + k * 2.1f + side);
				f.Bloom(p, Palette.PrimaryAt(t * 0.1f) * (0.35f * alpha), 5f);
				f.Sparkle(p, Palette.Core * (alpha * twinkle), (7f + k * 2f) * twinkle);
				previous = p;
			}
			tips[b] = previous;
		}

		for (int b = 0; b < tips.Length - 1; b++)
			f.Line(tips[b], tips[b + 1], Palette.AccentAt(t * 0.1f) * (0.3f * alpha), 1f);
	}
}
