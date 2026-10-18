using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace TerraSkins.Common.Effects;

/// <summary> Reusable building blocks for effects. </summary>
public static class Fx
{
	/// <summary>
	/// Tints a non-emissive particle (petals, bubbles...) with the world light at its position,
	/// so that it does not glow in the dark. Glowing particles should not use this.
	/// </summary>
	public static Color Lit(in FxFrame f, Vector2 screenPosition, Color color)
	{
		if (!f.InWorld)
			return color;

		Vector2 world = screenPosition + Main.screenPosition;
		Color light = Lighting.GetColor((int)(world.X / 16f), (int)(world.Y / 16f));
		// Keep a little of the base color so effects stay readable in dark places.
		return color.MultiplyRGB(Color.Lerp(light, Color.White, 0.25f));
	}

	/// <summary> Twinkling stars scattered in a disc, optionally rotating with it. </summary>
	public static void StarField(in FxFrame f, Vector2 center, int count, float radius, float rotation, FxPalette palette, float seed, float intensity = 1f)
	{
		int s = (int)(seed * 1000f);
		for (int i = 0; i < count; i++) {
			float angle = FxRandom.Float(s, i, 1) * MathHelper.TwoPi + rotation;
			float distance = MathF.Sqrt(FxRandom.Float(s, i, 2)) * radius;
			Vector2 p = center + angle.ToRotationVector2() * new Vector2(distance, distance * 0.85f);
			float twinkle = 0.5f + 0.5f * MathF.Sin(f.Time * MathHelper.Lerp(1.5f, 4f, FxRandom.Float(s, i, 3)) + i * 2.1f);
			Color color = FxRandom.Float(s, i, 4) < 0.3f ? palette.AccentAt(f.Time * 0.1f) : palette.Core;
			float size = MathHelper.Lerp(5f, 11f, FxRandom.Float(s, i, 5)) * (0.6f + 0.4f * twinkle);
			f.Sparkle(p, color * (twinkle * intensity), size);
		}
	}

	/// <summary> Small glowing motes drifting upwards around the body. </summary>
	public static void RisingEmbers(in FxFrame f, int count, FxPalette palette, float speed, bool spiral = false)
	{
		Texture2D glow = FxTextures.GlowSharp.Value;
		for (int i = 0; i < count; i++) {
			float age = FxRandom.Cycle(f.Time * speed, i, count, 2.2f, out int gen);
			float x = FxRandom.Signed(gen, i, 31) * 16f;
			float y = 18f - age * 50f;
			if (spiral) {
				float angle = FxRandom.Float(gen, i, 32) * MathHelper.TwoPi + age * 6f;
				x = MathF.Cos(angle) * (8f + age * 16f);
			}
			else {
				x += MathF.Sin(f.Time * 3f + i * 1.7f) * 3f * age;
			}

			Vector2 p = f.Local(x, y);
			float alpha = MathF.Sin(age * MathF.PI);
			Color color = Color.Lerp(palette.Core, palette.PrimaryAt(f.Time * 0.1f + i * 0.07f), age);
			float size = MathHelper.Lerp(5f, 9f, FxRandom.Float(gen, i, 33)) * (1f - age * 0.5f);
			f.Glow(glow, p, color * alpha, 0f, size / glow.Width);
		}
	}

	/// <summary> A jagged, branching lightning bolt between two points. </summary>
	public static void Lightning(in FxFrame f, Vector2 start, Vector2 end, int seed, FxPalette palette, float intensity)
	{
		const int segments = 7;
		Vector2 normal = Vector2.Normalize(end - start).RotatedBy(MathHelper.PiOver2);
		float length = Vector2.Distance(start, end);
		Vector2 previous = start;
		for (int i = 1; i <= segments; i++) {
			float k = i / (float)segments;
			float jitter = i == segments ? 0f : FxRandom.Signed(seed, i, 41) * length * 0.16f;
			Vector2 point = Vector2.Lerp(start, end, k) + normal * jitter;
			f.Line(previous, point, palette.PrimaryAt(f.Time * 0.2f) * (0.45f * intensity), 4f);
			f.Line(previous, point, palette.Core * intensity, 1.5f);

			if (i == 3 && FxRandom.Float(seed, i, 42) < 0.6f) {
				// One short branch.
				Vector2 branch = point + (end - start).RotatedBy(FxRandom.Signed(seed, i, 43) * 0.9f) * 0.35f;
				f.Line(point, branch, palette.PrimaryAt(f.Time * 0.2f) * (0.3f * intensity), 3f);
				f.Line(point, branch, palette.Core * (0.7f * intensity), 1f);
			}
			previous = point;
		}
		f.Bloom(end, palette.PrimaryAt(f.Time * 0.2f) * (0.5f * intensity), 7f);
	}

	/// <summary> Position on a tilted ellipse around a center. Depth is positive for the half that is closer to the viewer. </summary>
	public static Vector2 Ellipse(float angle, float radiusX, float radiusY, float tilt, out float depth)
	{
		depth = MathF.Sin(angle);
		return new Vector2(MathF.Cos(angle) * radiusX, depth * radiusY).RotatedBy(tilt);
	}
}
