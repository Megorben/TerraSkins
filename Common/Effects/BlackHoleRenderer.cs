using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace TerraSkins.Common.Effects;

/// <summary>
/// Draws a stylised black hole: a tilted, Keplerian accretion disk, the lensed image of its far side wrapping around the
/// shadow, a photon ring, infalling streaks and optional relativistic jets. The far half of the disk is drawn by
/// <see cref="DrawBack"/>, the near half (which crosses in front of the shadow) by <see cref="DrawFront"/>.
/// </summary>
public static class BlackHoleRenderer
{
	public sealed record Settings(int Clumps = 46, int Infall = 14, bool Jets = false, float Tilt = 0.3f, float Angle = -0.14f);

	public static void DrawBack(in FxFrame f, Vector2 center, float radius, FxPalette palette, Settings settings)
	{
		float t = f.Time;
		float coreRadius = radius * 0.3f;

		// Gravity well haze.
		f.Bloom(center, palette.EdgeAt(t * 0.1f) * 0.32f, radius * 1.7f);
		f.Bloom(center, palette.PrimaryAt(t * 0.1f) * 0.18f, radius * 1.05f);

		if (settings.Jets)
			DrawJets(f, center, radius, palette, settings);

		// Far half of the disk.
		DrawDiskHalf(f, center, radius, palette, settings, front: false);
		DrawClumps(f, center, radius, palette, settings, front: false);

		// The far side of the disk, bent over and under the shadow by gravity.
		Texture2D ring = FxTextures.Ring.Value;
		float ringScale = radius * 0.5f / (ring.Width * 0.44f);
		float flicker = 0.85f + 0.15f * MathF.Sin(t * 5.3f);
		f.Glow(ring, center, palette.PrimaryAt(t * 0.1f) * 0.75f * flicker, settings.Angle, new Vector2(ringScale * 1.06f, ringScale));
		f.Glow(ring, center, palette.Core * 0.35f, settings.Angle, new Vector2(ringScale * 0.86f, ringScale * 0.82f));

		// Photon ring.
		Texture2D thin = FxTextures.RingThin.Value;
		float photonScale = coreRadius * 1.13f / (thin.Width * 0.47f);
		f.Glow(thin, center, palette.Core * 0.95f, 0f, photonScale);

		// Shadow. Drawn alpha blended so it actually blots out whatever is behind.
		Texture2D glow = FxTextures.Glow.Value;
		f.Draw(glow, center, Color.Black * 0.85f, 0f, new Vector2(coreRadius * 2.9f / glow.Width));
		Texture2D disc = FxTextures.Disc.Value;
		f.Draw(disc, center, Color.Black, 0f, new Vector2(coreRadius * 2f / disc.Width));
	}

	public static void DrawFront(in FxFrame f, Vector2 center, float radius, FxPalette palette, Settings settings)
	{
		DrawDiskHalf(f, center, radius, palette, settings, front: true);
		DrawClumps(f, center, radius, palette, settings, front: true);
		DrawInfall(f, center, radius, palette, settings);

		// Occasional glints riding the disk.
		float t = f.Time;
		for (int i = 0; i < 3; i++) {
			float age = FxRandom.Cycle(t, i, 3, 1.7f, out int gen);
			float angle = FxRandom.Float(gen, i, 7) * MathHelper.TwoPi + age * 2f;
			float r = radius * MathHelper.Lerp(0.45f, 0.9f, FxRandom.Float(gen, i, 8));
			Vector2 p = Disk(angle, r, settings, out float depth);
			if (depth < 0f)
				continue;
			float fade = MathF.Sin(age * MathF.PI);
			f.Sparkle(center + p, palette.Core * fade, 9f + 6f * fade, age * 2f);
		}
	}

	private static Vector2 Disk(float angle, float r, Settings settings, out float depth)
	{
		depth = MathF.Sin(angle);
		return new Vector2(MathF.Cos(angle) * r, depth * r * settings.Tilt).RotatedBy(settings.Angle);
	}

	private static void DrawDiskHalf(in FxFrame f, Vector2 center, float radius, FxPalette palette, Settings settings, bool front)
	{
		Texture2D disk = FxTextures.Disk.Value;
		int half = disk.Height / 2;
		var source = front ? new Rectangle(0, half, disk.Width, half) : new Rectangle(0, 0, disk.Width, half);
		var origin = front ? new Vector2(disk.Width * 0.5f, 0f) : new Vector2(disk.Width * 0.5f, half);
		float scaleX = radius * 2f / disk.Width;
		var scale = new Vector2(scaleX, scaleX * settings.Tilt);
		float t = f.Time;

		float pulse = 0.9f + 0.1f * MathF.Sin(t * 2.1f);
		f.Glow(disk, center, source, palette.PrimaryAt(t * 0.1f) * 0.85f * pulse, settings.Angle, origin, scale);
		f.Glow(disk, center, source, palette.Core * 0.55f, settings.Angle, origin, scale * 0.7f);
		f.Glow(disk, center, source, palette.EdgeAt(t * 0.1f) * 0.5f, settings.Angle, origin, scale * 1.18f);
	}

	private static void DrawClumps(in FxFrame f, Vector2 center, float radius, FxPalette palette, Settings settings, bool front)
	{
		Texture2D streak = FxTextures.Streak.Value;
		Vector2 streakOrigin = streak.Size() * 0.5f;
		float t = f.Time;

		for (int i = 0; i < settings.Clumps; i++) {
			float u = FxRandom.Float(i, 11);
			float r = radius * MathHelper.Lerp(0.36f, 0.98f, MathF.Sqrt(u));
			// Keplerian: inner material orbits faster.
			float omega = 1.1f * MathF.Pow(radius / r, 1.5f);
			float angle = FxRandom.Float(i, 12) * MathHelper.TwoPi + t * omega;
			Vector2 p = Disk(angle, r, settings, out float depth);
			if (depth >= 0f != front)
				continue;

			float heat = (r / radius - 0.36f) / 0.62f;
			// Doppler beaming: the side moving towards the viewer is brighter.
			float beaming = 1f + 0.55f * MathF.Cos(angle);
			Color color = palette.Ramp(heat * 0.9f + 0.05f, t * 0.1f + u) * (0.55f * beaming);

			Vector2 tangent = new Vector2(-MathF.Sin(angle), MathF.Cos(angle) * settings.Tilt).RotatedBy(settings.Angle);
			float length = (0.06f + 0.05f * omega) * r / streak.Width * 3f;
			float size = MathHelper.Lerp(2.2f, 4.5f, FxRandom.Float(i, 13)) / streak.Height;
			f.Glow(streak, center + p, null, color, tangent.ToRotation(), streakOrigin, new Vector2(length, size * 2.2f));
		}
	}

	private static void DrawInfall(in FxFrame f, Vector2 center, float radius, FxPalette palette, Settings settings)
	{
		Texture2D streak = FxTextures.Streak.Value;
		Vector2 origin = streak.Size() * 0.5f;
		float t = f.Time;

		for (int i = 0; i < settings.Infall; i++) {
			float age = FxRandom.Cycle(t, i, settings.Infall, 2.4f, out int gen);
			float r = radius * MathHelper.Lerp(1.55f, 0.33f, age * age);
			float angle = FxRandom.Float(gen, i, 21) * MathHelper.TwoPi + age * 4.2f;
			Vector2 p = Disk(angle, r, settings, out _);
			Vector2 prev = Disk(angle - 0.18f, r * 1.05f, settings, out _);
			Vector2 dir = p - prev;
			float alpha = MathF.Sin(age * MathF.PI) * 0.8f;
			Color color = Color.Lerp(palette.AccentAt(t * 0.1f), palette.Core, age) * alpha;
			f.Glow(streak, center + p, null, color, dir.ToRotation(), origin, new Vector2(dir.Length() * 2.2f / streak.Width + 0.05f, 1.6f / streak.Height));
		}
	}

	private static void DrawJets(in FxFrame f, Vector2 center, float radius, FxPalette palette, Settings settings)
	{
		Texture2D ray = FxTextures.Ray.Value;
		var origin = new Vector2(ray.Width * 0.5f, ray.Height);
		float t = f.Time;
		float axis = settings.Angle - MathHelper.PiOver2;

		for (int side = 0; side < 2; side++) {
			float rotation = axis + side * MathF.PI + MathHelper.PiOver2;
			for (int layer = 0; layer < 3; layer++) {
				float pulse = 0.75f + 0.25f * MathF.Sin(t * 7f + layer * 2.1f + side);
				float length = radius * (1.9f - layer * 0.35f) * pulse / ray.Height;
				float width = radius * (0.22f - layer * 0.05f) / ray.Width;
				Color color = (layer == 2 ? palette.Core : palette.AccentAt(t * 0.1f + layer * 0.1f)) * (0.55f - layer * 0.1f);
				f.Glow(ray, center, null, color, rotation, origin, new Vector2(width, length));
			}
		}
	}
}
