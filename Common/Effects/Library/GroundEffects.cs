using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.Effects.Library;

public static class GroundHelper
{
	/// <summary> 1 while standing on the ground, fading out quickly in the air. </summary>
	public static float Grounded(in FxFrame f) => f.Player.velocity.Y == 0f || !f.InWorld ? 1f : 0.35f;

	/// <summary> Draws a texture lying flat on the ground (squashed vertically). </summary>
	public static void Flat(in FxFrame f, Texture2D texture, Vector2 center, Color color, float radius, float squash = 0.28f, float textureRadius = 0.47f, bool additive = true)
	{
		var scale = new Vector2(radius, radius * squash) / (texture.Width * textureRadius);
		if (additive)
			f.Glow(texture, center, color, 0f, scale);
		else
			f.Draw(texture, center, color, 0f, scale);
	}
}

/// <summary> A glowing magic circle under the player's feet. </summary>
public class MagicCircleGround(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Ground, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		float strength = GroundHelper.Grounded(f);
		Vector2 center = f.Feet + new Vector2(0f, 1f * f.GravDir);
		float t = f.Time;
		float pulse = 0.8f + 0.2f * MathF.Sin(t * 2.5f);

		GroundHelper.Flat(f, FxTextures.Glow.Value, center, Palette.PrimaryAt(t * 0.1f) * (0.35f * strength), 34f, textureRadius: 0.5f);
		GroundHelper.Flat(f, FxTextures.RuneCircle.Value, center, Palette.PrimaryAt(t * 0.1f) * (0.85f * pulse * strength), 30f);
		GroundHelper.Flat(f, FxTextures.RingThin.Value, center, Palette.Core * (0.6f * strength), 34f);

		// Glyphs orbiting along the circle (a flat ellipse can't be rotated, so the motion comes from these).
		Texture2D runes = FxTextures.Runes.Value;
		int size = runes.Height;
		for (int i = 0; i < 10; i++) {
			float angle = i * MathHelper.TwoPi / 10f + t * 0.6f;
			Vector2 p = center + Fx.Ellipse(angle, 22f, 22f * 0.28f, 0f, out float depth);
			var source = new Rectangle(i % (runes.Width / size) * size, 0, size, size);
			f.Glow(runes, p, source, Palette.Core * ((0.55f + 0.3f * depth) * strength), 0f, new Vector2(size * 0.5f), new Vector2(0.4f, 0.28f));
		}

		// Motes rising out of the circle.
		Texture2D glow = FxTextures.GlowSharp.Value;
		for (int i = 0; i < 8; i++) {
			float age = FxRandom.Cycle(t, i, 8, 1.8f, out int gen);
			float angle = FxRandom.Float(gen, i) * MathHelper.TwoPi;
			Vector2 p = center + new Vector2(MathF.Cos(angle) * 26f, (MathF.Sin(angle) * 7f - age * 30f) * f.GravDir);
			f.Glow(glow, p, Palette.AccentAt(t * 0.1f) * (MathF.Sin(age * MathF.PI) * strength), 0f, 6f / glow.Width);
		}
	}
}

/// <summary> A pool of shadow with wisps rising from it. </summary>
public class ShadowPoolGround(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Ground, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		float strength = GroundHelper.Grounded(f);
		Vector2 center = f.Feet + new Vector2(0f, 1f * f.GravDir);
		float t = f.Time;

		GroundHelper.Flat(f, FxTextures.Glow.Value, center, Color.Black * (0.75f * strength), 30f, 0.3f, 0.5f, additive: false);
		GroundHelper.Flat(f, FxTextures.Ring.Value, center, Palette.PrimaryAt(t * 0.1f) * (0.5f * strength), 24f, 0.3f, 0.44f);

		Texture2D smoke = FxTextures.Smoke.Value;
		int frameWidth = smoke.Width / 4;
		for (int i = 0; i < 9; i++) {
			float age = FxRandom.Cycle(t, i, 9, 2.2f, out int gen);
			float x = FxRandom.Signed(gen, i) * 20f;
			Vector2 p = center + new Vector2(x + MathF.Sin(t * 2f + i) * 3f, -age * 34f * f.GravDir);
			var source = new Rectangle(frameWidth * (i % 4), 0, frameWidth, smoke.Height);
			float alpha = MathF.Sin(age * MathF.PI) * 0.5f * strength;
			f.Draw(smoke, p, source, Color.Lerp(Palette.Edge, Color.Black, 0.6f) * alpha, age * 2f + i, new Vector2(frameWidth, smoke.Height) * 0.5f, new Vector2(0.25f + age * 0.25f));
		}
		Fx.RisingEmbers(f, 6, Palette, 0.8f);
	}
}

/// <summary> A pillar of soft light rising from the ground. </summary>
public class SunbeamGround(string name, FxPalette palette) : SkinEffect(name, WardrobeSlot.Ground, palette)
{
	public override void DrawBehind(in FxFrame f)
	{
		float strength = GroundHelper.Grounded(f);
		Vector2 center = f.Feet;
		float t = f.Time;
		Texture2D ray = FxTextures.Ray.Value;
		var origin = new Vector2(ray.Width * 0.5f, ray.Height);
		float rotation = f.GravDir < 0f ? MathF.PI : 0f;

		GroundHelper.Flat(f, FxTextures.Glow.Value, center, Palette.PrimaryAt(t * 0.1f) * (0.5f * strength), 26f, 0.3f, 0.5f);
		for (int i = 0; i < 3; i++) {
			float pulse = 0.8f + 0.2f * MathF.Sin(t * 1.7f + i * 2f);
			f.Glow(ray, center, null, (i == 2 ? Palette.Core : Palette.PrimaryAt(t * 0.1f)) * (0.32f * pulse * strength), rotation, origin, new Vector2((26f - i * 7f) / ray.Width, 90f / ray.Height));
		}

		for (int i = 0; i < 10; i++) {
			float age = FxRandom.Cycle(t, i, 10, 2.4f, out int gen);
			Vector2 p = center + new Vector2(FxRandom.Signed(gen, i) * 12f, -age * 80f * f.GravDir);
			f.Sparkle(p, Palette.Core * (MathF.Sin(age * MathF.PI) * strength), 8f);
		}
	}
}

public enum FootstepStyle
{
	Flowers,
	Frost,
}

/// <summary> Flowers or ice crystals springing up where the player walks. </summary>
public class FootstepGround(string name, FxPalette palette, FootstepStyle style) : SkinEffect(name, WardrobeSlot.Ground, palette)
{
	private const int Lifetime = 110;

	public override EffectState CreateState() => new TrailState(48, 1);

	public override void Update(Player player, EffectState state)
	{
		var trail = (TrailState)state;
		trail.Particles.Update(1f);

		bool grounded = player.velocity.Y == 0f && !player.mount.Active;
		if (!grounded)
			return;

		trail.StepDistance += Math.Abs(player.velocity.X);
		bool idleBloom = Math.Abs(player.velocity.X) < 0.1f && Main.rand.NextBool(90);
		if (trail.StepDistance < 15f && !idleBloom)
			return;

		trail.StepDistance = 0f;
		ref Particle p = ref trail.Particles.Spawn();
		p = default;
		p.Position = player.Bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), -1f) * new Vector2(1f, player.gravDir);
		p.MaxLife = p.Life = Lifetime + Main.rand.Next(30);
		p.Scale = Main.rand.NextFloat(0.75f, 1.1f);
		p.Seed = Main.rand.NextFloat();
		p.Rotation = Main.rand.NextFloat(-0.3f, 0.3f);
	}

	public override void DrawBehind(in FxFrame f)
	{
		if (style == FootstepStyle.Frost) {
			float strength = GroundHelper.Grounded(f);
			GroundHelper.Flat(f, FxTextures.Glow.Value, f.Feet, Palette.PrimaryAt(f.Time * 0.1f) * (0.3f * strength), 24f, 0.25f, 0.5f);
		}

		if (!f.InWorld || f.State is not TrailState trail)
			return;

		for (int i = 0; i < trail.Particles.Count; i++) {
			ref Particle p = ref trail.Particles[i];
			Vector2 position = f.WorldToScreen(p.Position);
			// Grow fast, then wilt or melt.
			float grow = MathF.Min(1f, p.Progress * 7f);
			float fade = MathF.Min(1f, (1f - p.Progress) * 4f);
			if (style == FootstepStyle.Flowers)
				DrawFlower(f, position, p, grow, fade);
			else
				DrawIce(f, position, p, grow, fade);
		}
	}

	private void DrawFlower(in FxFrame f, Vector2 position, in Particle p, float grow, float fade)
	{
		Texture2D petals = FxTextures.Petals.Value;
		int size = petals.Height;
		var source = new Rectangle(0, 0, size, size);
		float scale = p.Scale * grow * 0.55f;
		Vector2 center = position + new Vector2(0f, -3f * scale * f.GravDir);
		Color petal = Fx.Lit(f, center, Color.Lerp(Palette.PrimaryAt(p.Seed), Palette.AccentAt(p.Seed), p.Seed)) * fade;
		for (int k = 0; k < 5; k++) {
			float angle = k * MathHelper.TwoPi / 5f + p.Rotation;
			Vector2 offset = angle.ToRotationVector2() * new Vector2(4f, 2.2f) * scale;
			f.Draw(petals, center + offset, source, petal, angle + MathHelper.PiOver2, new Vector2(size * 0.5f, size * 0.8f), new Vector2(scale * 0.8f, scale * 0.6f));
		}
		f.Draw(FxTextures.Disc.Value, center, Fx.Lit(f, center, Palette.Core) * fade, 0f, new Vector2(3.5f * scale / FxTextures.Disc.Value.Width));
		if (p.Progress < 0.15f)
			f.Sparkle(center, Palette.Core * (1f - p.Progress / 0.15f), 10f);
	}

	private void DrawIce(in FxFrame f, Vector2 position, in Particle p, float grow, float fade)
	{
		Texture2D crystal = FxTextures.Crystal.Value;
		var origin = new Vector2(crystal.Width * 0.5f, crystal.Height);
		for (int k = -1; k <= 1; k++) {
			float rotation = k * 0.45f + p.Rotation * 0.5f + (f.GravDir < 0f ? MathF.PI : 0f);
			float scale = p.Scale * grow * (k == 0 ? 0.5f : 0.35f);
			f.Draw(crystal, position, null, Fx.Lit(f, position, Color.Lerp(Palette.Core, Palette.PrimaryAt(0f), 0.4f)) * (fade * 0.9f), rotation, origin, new Vector2(scale));
			f.Glow(crystal, position, null, Palette.PrimaryAt(f.Time * 0.1f) * (fade * 0.35f), rotation, origin, new Vector2(scale));
		}
		if (p.Progress < 0.12f)
			f.Sparkle(position + new Vector2(0f, -6f * f.GravDir), Palette.Core * (1f - p.Progress / 0.12f), 12f);
	}
}
