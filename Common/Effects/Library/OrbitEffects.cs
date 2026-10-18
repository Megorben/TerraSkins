using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.Effects.Library;

/// <summary>
/// Objects circling the player on a tilted ellipse. The half of the orbit that passes behind the player is drawn in
/// the back layer, the other half in the front layer, which gives a convincing 3D look.
/// </summary>
public abstract class OrbitEffect(string name, FxPalette palette, int count, float speed, float radiusX = 27f, float radiusY = 9f, float tilt = 0.22f)
	: SkinEffect(name, WardrobeSlot.Orbit, palette)
{
	protected int Count => count;

	/// <summary> Number of ghost copies drawn behind each object along its path. </summary>
	protected virtual int TrailLength => 6;

	protected virtual float TrailSpacing => 0.09f;

	public override void DrawBehind(in FxFrame f) => DrawAll(f, front: false);

	public override void DrawFront(in FxFrame f) => DrawAll(f, front: true);

	protected virtual Vector2 Position(in FxFrame f, int index, float angle, out float depth)
	{
		float facingTilt = tilt * f.Direction;
		return f.Local(0f, -2f) + Fx.Ellipse(angle, radiusX, radiusY * f.GravDir, facingTilt, out depth);
	}

	protected float Angle(in FxFrame f, int index) => f.Time * speed + index * MathHelper.TwoPi / count;

	private void DrawAll(in FxFrame f, bool front)
	{
		for (int i = 0; i < count; i++) {
			float angle = Angle(f, i);
			for (int k = TrailLength; k >= 1; k--) {
				float trailAngle = angle - k * TrailSpacing * Math.Sign(speed);
				Vector2 trailPos = Position(f, i, trailAngle, out float trailDepth);
				if (trailDepth >= 0f != front)
					continue;
				DrawTrail(f, i, trailPos, 1f - k / (float)(TrailLength + 1), trailDepth);
			}

			Vector2 p = Position(f, i, angle, out float depth);
			if (depth >= 0f != front)
				continue;
			DrawObject(f, i, p, angle, depth);
		}
	}

	protected virtual void DrawTrail(in FxFrame f, int index, Vector2 position, float strength, float depth)
	{
		f.Bloom(position, Palette.PrimaryAt(f.Time * 0.1f + index * 0.2f) * (0.22f * strength), 4f + 3f * strength);
	}

	protected abstract void DrawObject(in FxFrame f, int index, Vector2 position, float angle, float depth);

	/// <summary> Objects on the far side look smaller and dimmer. </summary>
	protected static float DepthScale(float depth) => 0.8f + 0.2f * depth;
}

public class StarOrbit(string name, FxPalette palette, int count = 3) : OrbitEffect(name, palette, count, 1.8f)
{
	protected override void DrawObject(in FxFrame f, int index, Vector2 position, float angle, float depth)
	{
		float scale = DepthScale(depth);
		Color color = Palette.PrimaryAt(f.Time * 0.1f + index / (float)Count);
		f.Bloom(position, color * 0.5f, 9f * scale);
		f.Sparkle(position, color, 15f * scale, f.Time * 2f + index);
		f.Sparkle(position, Palette.Core, 9f * scale, -f.Time * 1.3f);
	}
}

public class CrystalOrbit(string name, FxPalette palette) : OrbitEffect(name, palette, 4, 1.25f, 25f, 8f)
{
	protected override void DrawObject(in FxFrame f, int index, Vector2 position, float angle, float depth)
	{
		Texture2D crystal = FxTextures.Crystal.Value;
		float scale = DepthScale(depth) * 0.75f;
		float bob = MathF.Sin(f.Time * 2.5f + index * 1.4f) * 2f;
		position.Y += bob;
		float spin = f.Time * 1.6f + index;

		f.Bloom(position, Palette.PrimaryAt(f.Time * 0.1f) * 0.35f, 10f * scale);
		f.Draw(crystal, position, null, Color.Lerp(Palette.PrimaryAt(f.Time * 0.1f), Palette.Core, 0.25f), MathF.Sin(spin) * 0.35f, crystal.Size() * 0.5f, new Vector2(scale * MathF.Max(0.35f, MathF.Abs(MathF.Cos(spin))), scale));
		f.Glow(crystal, position, null, Palette.Core * 0.35f, MathF.Sin(spin) * 0.35f, crystal.Size() * 0.5f, new Vector2(scale * 0.6f, scale * 0.8f));

		// A glint every now and then.
		float glint = MathF.Max(0f, MathF.Sin(f.Time * 2f + index * 2.4f) - 0.92f) * 12f;
		if (glint > 0f)
			f.Sparkle(position + new Vector2(0f, -4f * scale), Palette.Core * glint, 12f * glint);
	}
}

public class MoonOrbit(string name, FxPalette palette) : OrbitEffect(name, palette, 2, 0.9f, 30f, 11f)
{
	protected override int TrailLength => 0;

	protected override Vector2 Position(in FxFrame f, int index, float angle, out float depth)
	{
		// Two moons on different orbits.
		float radius = index == 0 ? 30f : 22f;
		float speedFactor = index == 0 ? 1f : 1.6f;
		return f.Local(0f, -2f) + Fx.Ellipse(angle * speedFactor + index * 2.2f, radius, radius * 0.35f * f.GravDir, -0.25f * f.Direction, out depth);
	}

	protected override void DrawObject(in FxFrame f, int index, Vector2 position, float angle, float depth)
	{
		Texture2D planet = FxTextures.Planet.Value;
		float size = (index == 0 ? 11f : 7f) * DepthScale(depth);
		Color color = index == 0 ? Color.Lerp(Palette.Core, Palette.PrimaryAt(0f), 0.2f) : Palette.AccentAt(f.Time * 0.1f);
		f.Bloom(position, Palette.PrimaryAt(f.Time * 0.1f) * 0.3f, size * 1.3f);
		f.Draw(planet, position, null, color, f.Time * 0.2f + index, planet.Size() * 0.5f, new Vector2(size / planet.Width));
	}
}

public class WispOrbit(string name, FxPalette palette) : OrbitEffect(name, palette, 4, 1.5f, 24f, 12f)
{
	protected override int TrailLength => 12;

	protected override float TrailSpacing => 0.07f;

	protected override Vector2 Position(in FxFrame f, int index, float angle, out float depth)
	{
		// Wobbly, slightly chaotic paths.
		float radius = 22f + 6f * MathF.Sin(angle * 2f + index * 1.3f);
		float height = 5f * MathF.Sin(angle * 3f + index);
		return f.Local(0f, -4f + height) + Fx.Ellipse(angle, radius, radius * 0.45f * f.GravDir, 0.15f * f.Direction, out depth);
	}

	protected override void DrawTrail(in FxFrame f, int index, Vector2 position, float strength, float depth)
	{
		Texture2D glow = FxTextures.GlowSharp.Value;
		f.Glow(glow, position, Palette.PrimaryAt(f.Time * 0.1f + index * 0.25f) * (0.5f * strength), 0f, (2f + 5f * strength) / glow.Width);
	}

	protected override void DrawObject(in FxFrame f, int index, Vector2 position, float angle, float depth)
	{
		float scale = DepthScale(depth);
		f.Bloom(position, Palette.PrimaryAt(f.Time * 0.1f + index * 0.25f) * 0.55f, 10f * scale);
		f.Bloom(position, Palette.Core, 3.5f * scale);
	}
}

public class BladeOrbit(string name, FxPalette palette) : OrbitEffect(name, palette, 5, 1.4f, 28f, 10f)
{
	protected override int TrailLength => 5;

	protected override void DrawTrail(in FxFrame f, int index, Vector2 position, float strength, float depth)
	{
		Texture2D blade = FxTextures.Blade.Value;
		float angle = Angle(f, index) - (1f - strength) * 0.5f;
		f.Glow(blade, position, null, Palette.PrimaryAt(f.Time * 0.1f) * (0.25f * strength), angle + MathHelper.PiOver2, blade.Size() * 0.5f, new Vector2(0.6f * DepthScale(depth)));
	}

	protected override void DrawObject(in FxFrame f, int index, Vector2 position, float angle, float depth)
	{
		Texture2D blade = FxTextures.Blade.Value;
		float scale = 0.6f * DepthScale(depth);
		// Blades point along their path like a spinning sword ring.
		float rotation = angle + MathHelper.PiOver2;
		f.Bloom(position, Palette.PrimaryAt(f.Time * 0.1f) * 0.3f, 12f * scale);
		f.Draw(blade, position, null, Color.Lerp(Palette.Core, Palette.PrimaryAt(f.Time * 0.1f), 0.35f) * 0.9f, rotation, blade.Size() * 0.5f, new Vector2(scale));
		f.Glow(blade, position, null, Palette.PrimaryAt(f.Time * 0.1f) * 0.6f, rotation, blade.Size() * 0.5f, new Vector2(scale * 1.1f));
	}
}

public class AtomOrbit(string name, FxPalette palette) : OrbitEffect(name, palette, 3, 3.2f, 26f, 8f)
{
	protected override int TrailLength => 8;

	protected override float TrailSpacing => 0.12f;

	private static float Plane(int index) => index * MathHelper.Pi / 3f + 0.3f;

	protected override Vector2 Position(in FxFrame f, int index, float angle, out float depth)
	{
		return f.Local(0f, -2f) + Fx.Ellipse(angle, 27f, 8f, Plane(index), out depth);
	}

	public override void DrawBehind(in FxFrame f)
	{
		// Faint orbit paths.
		for (int i = 0; i < Count; i++) {
			for (int k = 0; k < 20; k++) {
				Vector2 p = Position(f, i, k * MathHelper.TwoPi / 20f, out _);
				f.Bloom(p, Palette.PrimaryAt(f.Time * 0.1f) * 0.08f, 2.5f);
			}
		}
		f.Bloom(f.Local(0f, -2f), Palette.PrimaryAt(f.Time * 0.1f) * 0.15f, 18f);
		base.DrawBehind(f);
	}

	protected override void DrawObject(in FxFrame f, int index, Vector2 position, float angle, float depth)
	{
		float scale = DepthScale(depth);
		f.Bloom(position, Palette.PrimaryAt(f.Time * 0.1f) * 0.6f, 7f * scale);
		f.Sparkle(position, Palette.Core, 10f * scale, f.Time * 3f);
	}
}

public class ButterflyOrbit(string name, FxPalette palette) : OrbitEffect(name, palette, 5, 0.8f, 26f, 14f)
{
	protected override int TrailLength => 4;

	protected override float TrailSpacing => 0.15f;

	protected override Vector2 Position(in FxFrame f, int index, float angle, out float depth)
	{
		// Lissajous wandering.
		float a = angle + index * 0.9f;
		depth = MathF.Sin(a);
		return f.Local(MathF.Cos(a) * (20f + 8f * MathF.Sin(a * 0.5f + index)), MathF.Sin(a * 2f + index) * 14f - 6f);
	}

	protected override void DrawTrail(in FxFrame f, int index, Vector2 position, float strength, float depth)
	{
		Texture2D glow = FxTextures.GlowSharp.Value;
		f.Glow(glow, position, Palette.AccentAt(f.Time * 0.1f) * (0.35f * strength), 0f, 3f / glow.Width);
	}

	protected override void DrawObject(in FxFrame f, int index, Vector2 position, float angle, float depth)
	{
		Texture2D butterfly = FxTextures.Butterfly.Value;
		int size = butterfly.Height;
		bool open = MathF.Sin(f.Time * 18f + index * 2f) > 0f;
		var source = new Rectangle(open ? 0 : size, 0, size, size);
		float scale = 0.8f * DepthScale(depth);
		Color color = Fx.Lit(f, position, Palette.PrimaryAt(f.Time * 0.1f + index * 0.2f));
		var effects = MathF.Cos(angle + index * 0.9f) < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		f.Draw(butterfly, position, source, color, 0f, new Vector2(size * 0.5f), new Vector2(scale), effects);
		f.Glow(butterfly, position, source, Palette.Core * 0.2f, 0f, new Vector2(size * 0.5f), new Vector2(scale), effects);
	}
}
