using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.Effects.Library;

/// <summary>
/// Trails leave particles in the world as the player moves. They keep state and are simulated every tick on every
/// client, for remote players too, so everyone sees the same trail.
/// </summary>
public abstract class TrailEffect(string name, FxPalette palette, int capacity = 140, int history = 2) : SkinEffect(name, WardrobeSlot.Trail, palette)
{
	protected virtual float Drag => 0.95f;

	protected virtual float Gravity => 0f;

	public override EffectState CreateState() => new TrailState(capacity, history);

	public override void Update(Player player, EffectState state)
	{
		var trail = (TrailState)state;
		trail.Particles.Update(Drag, Gravity);
		trail.PushHistory(HistoryPoint(player));

		trail.EmitBudget += EmissionRate(player.velocity.Length());
		while (trail.EmitBudget >= 1f) {
			trail.EmitBudget -= 1f;
			ref Particle p = ref trail.Particles.Spawn();
			p = default;
			p.Seed = Main.rand.NextFloat();
			Emit(player, ref p);
		}
	}

	protected virtual Vector2 HistoryPoint(Player player) => player.Center;

	/// <summary> Particles per tick for the given speed (pixels per tick). </summary>
	protected abstract float EmissionRate(float speed);

	protected abstract void Emit(Player player, ref Particle p);

	public override void DrawBehind(in FxFrame f)
	{
		if (!f.InWorld || f.State is not TrailState trail)
			return;

		for (int i = 0; i < trail.Particles.Count; i++)
			DrawParticle(f, ref trail.Particles[i], f.WorldToScreen(trail.Particles[i].Position));
	}

	protected abstract void DrawParticle(in FxFrame f, ref Particle p, Vector2 screenPosition);

	protected static Vector2 RandomPointIn(Player player, float shrink = 0.8f)
	{
		return player.Center + new Vector2(Main.rand.NextFloat(-1f, 1f) * player.width * 0.5f * shrink, Main.rand.NextFloat(-1f, 1f) * player.height * 0.5f * shrink);
	}

	protected static float Fade(in Particle p) => MathF.Sin(p.Progress * MathF.PI);
}

public class StardustTrail(string name, FxPalette palette) : TrailEffect(name, palette)
{
	protected override float EmissionRate(float speed) => speed > 1.5f ? 0.5f + speed * 0.12f : 0.06f;

	protected override void Emit(Player player, ref Particle p)
	{
		p.Position = RandomPointIn(player);
		p.Velocity = -player.velocity * 0.12f + Main.rand.NextVector2Circular(0.4f, 0.4f) + new Vector2(0f, -0.15f);
		p.MaxLife = p.Life = Main.rand.Next(40, 75);
		p.Scale = Main.rand.NextFloat(7f, 13f);
		p.Spin = Main.rand.NextFloat(-0.05f, 0.05f);
		p.Kind = (byte)Main.rand.Next(3);
	}

	protected override void DrawParticle(in FxFrame f, ref Particle p, Vector2 position)
	{
		float fade = Fade(p);
		Color color = p.Kind switch {
			0 => Palette.Core,
			1 => Palette.PrimaryAt(f.Time * 0.1f + p.Seed),
			_ => Palette.AccentAt(f.Time * 0.1f + p.Seed),
		};
		float twinkle = 0.7f + 0.3f * MathF.Sin(f.Time * 8f + p.Seed * 20f);
		f.Sparkle(position, color * fade, p.Scale * fade * twinkle, p.Rotation);
	}
}

/// <summary> A smooth ribbon of light following the player. </summary>
public class RibbonTrail(string name, FxPalette palette) : TrailEffect(name, palette, 40, 28)
{
	protected override Vector2 HistoryPoint(Player player) => player.Center + new Vector2(0f, 4f * player.gravDir);

	protected override float EmissionRate(float speed) => speed > 3f ? 0.25f : 0f;

	protected override void Emit(Player player, ref Particle p)
	{
		p.Position = player.Center + Main.rand.NextVector2Circular(6f, 10f);
		p.Velocity = Main.rand.NextVector2Circular(0.5f, 0.5f);
		p.MaxLife = p.Life = Main.rand.Next(30, 50);
		p.Scale = Main.rand.NextFloat(6f, 10f);
	}

	public override void DrawBehind(in FxFrame f)
	{
		if (!f.InWorld || f.State is not TrailState trail)
			return;

		int count = trail.HistoryCount;
		for (int i = 0; i < count - 1; i++) {
			Vector2 a = trail.GetHistory(i);
			Vector2 b = trail.GetHistory(i + 1);
			if (Vector2.DistanceSquared(a, b) > 160f * 160f)
				break; // Teleported.

			float k = i / (float)(count - 1);
			float width = MathHelper.Lerp(9f, 1.5f, k);
			Color color = Palette.PrimaryAt(f.Time * 0.4f - k * 0.9f) * (1f - k);
			f.Line(f.WorldToScreen(a), f.WorldToScreen(b), color * 0.6f, width);
			f.Line(f.WorldToScreen(a), f.WorldToScreen(b), Palette.Core * ((1f - k) * 0.6f), width * 0.35f);
		}

		base.DrawBehind(f);
	}

	protected override void DrawParticle(in FxFrame f, ref Particle p, Vector2 position)
	{
		f.Sparkle(position, Palette.PrimaryAt(f.Time * 0.4f + p.Seed) * Fade(p), p.Scale * Fade(p));
	}
}

public class FlameTrail(string name, FxPalette palette) : TrailEffect(name, palette)
{
	protected override float Drag => 0.94f;

	protected override float EmissionRate(float speed) => speed > 1f ? 0.6f + speed * 0.15f : 0.1f;

	protected override void Emit(Player player, ref Particle p)
	{
		p.Position = player.Bottom + new Vector2(Main.rand.NextFloat(-8f, 8f), -Main.rand.NextFloat(4f, 20f)) * new Vector2(1f, player.gravDir);
		p.Velocity = -player.velocity * 0.2f + new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.6f, 1.3f) * player.gravDir);
		p.MaxLife = p.Life = Main.rand.Next(26, 44);
		p.Scale = Main.rand.NextFloat(0.28f, 0.45f);
		p.Rotation = player.gravDir < 0f ? MathF.PI : 0f;
	}

	protected override void DrawParticle(in FxFrame f, ref Particle p, Vector2 position)
	{
		Texture2D flame = FxTextures.Flame.Value;
		float k = p.Progress;
		float size = p.Scale * MathF.Sin(MathF.Min(1f, k * 1.4f) * MathF.PI * 0.5f + 0.4f) * (1f - k * 0.5f);
		Color color = Palette.Ramp(k, f.Time * 0.2f + p.Seed) * (1f - k);
		f.Glow(flame, position, null, color, p.Rotation, new Vector2(flame.Width * 0.5f, flame.Height * 0.8f), new Vector2(size * 0.8f, size));
	}
}

public class VoidTrail(string name, FxPalette palette) : TrailEffect(name, palette)
{
	protected override float Drag => 0.93f;

	protected override float EmissionRate(float speed) => speed > 1f ? 0.45f + speed * 0.12f : 0.07f;

	protected override void Emit(Player player, ref Particle p)
	{
		p.Position = RandomPointIn(player);
		p.Velocity = -player.velocity * 0.1f + Main.rand.NextVector2Circular(0.6f, 0.6f);
		p.MaxLife = p.Life = Main.rand.Next(45, 80);
		p.Kind = (byte)(Main.rand.NextBool(3) ? 1 : 0);
		p.Scale = p.Kind == 0 ? Main.rand.NextFloat(0.35f, 0.6f) : Main.rand.NextFloat(4f, 7f);
		p.Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		p.Spin = Main.rand.NextFloat(-0.03f, 0.03f);
	}

	protected override void DrawParticle(in FxFrame f, ref Particle p, Vector2 position)
	{
		float fade = Fade(p);
		if (p.Kind == 1) {
			Texture2D glow = FxTextures.GlowSharp.Value;
			f.Glow(glow, position, Palette.AccentAt(f.Time * 0.1f) * fade, 0f, p.Scale / glow.Width * 2f);
			return;
		}

		Texture2D smoke = FxTextures.Smoke.Value;
		int frameWidth = smoke.Width / 4;
		var source = new Rectangle(frameWidth * (int)(p.Seed * 4f % 4f), 0, frameWidth, smoke.Height);
		float scale = p.Scale * (0.6f + p.Progress * 0.8f);
		var origin = new Vector2(frameWidth, smoke.Height) * 0.5f;
		f.Draw(smoke, position, source, Color.Lerp(Palette.Edge, Color.Black, 0.55f) * (fade * 0.6f), p.Rotation, origin, new Vector2(scale));
		f.Glow(smoke, position, source, Palette.PrimaryAt(f.Time * 0.1f) * (fade * 0.18f), p.Rotation, origin, new Vector2(scale * 0.7f));
	}
}

public class PetalTrail(string name, FxPalette palette, int petalKind) : TrailEffect(name, palette)
{
	protected override float Drag => 0.98f;

	protected override float Gravity => 0.025f;

	protected override float EmissionRate(float speed) => speed > 1f ? 0.25f + speed * 0.07f : 0.04f;

	protected override void Emit(Player player, ref Particle p)
	{
		p.Position = RandomPointIn(player, 1f);
		p.Velocity = -player.velocity * 0.15f + new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(0.2f, 0.8f));
		p.MaxLife = p.Life = Main.rand.Next(80, 130);
		p.Scale = Main.rand.NextFloat(0.55f, 0.85f);
		p.Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		p.Spin = Main.rand.NextFloat(-0.08f, 0.08f);
	}

	protected override void DrawParticle(in FxFrame f, ref Particle p, Vector2 position)
	{
		Texture2D petals = FxTextures.Petals.Value;
		int size = petals.Height;
		float fade = MathF.Min(1f, (1f - p.Progress) * 4f) * MathF.Min(1f, p.Progress * 10f);
		position.X += MathF.Sin(f.Time * 2.5f + p.Seed * 10f) * 3f;
		float flip = MathF.Cos(f.Time * 4f + p.Seed * 13f);
		Color color = Fx.Lit(f, position, Color.Lerp(Palette.PrimaryAt(p.Seed), Palette.AccentAt(p.Seed), p.Seed * 0.6f));
		f.Draw(petals, position, new Rectangle(petalKind * size, 0, size, size), color * fade, p.Rotation, new Vector2(size * 0.5f), new Vector2(p.Scale * flip, p.Scale));
	}
}

public class LightningTrail(string name, FxPalette palette) : TrailEffect(name, palette, 60, 9)
{
	protected override float EmissionRate(float speed) => speed > 2f ? 0.3f + speed * 0.05f : 0f;

	protected override void Emit(Player player, ref Particle p)
	{
		p.Position = RandomPointIn(player);
		p.Velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
		p.MaxLife = p.Life = Main.rand.Next(10, 22);
		p.Scale = Main.rand.NextFloat(4f, 7f);
	}

	public override void DrawBehind(in FxFrame f)
	{
		if (!f.InWorld || f.State is not TrailState trail)
			return;

		int seed = (int)(f.Time * 20f);
		for (int i = 0; i < trail.HistoryCount - 1; i++) {
			Vector2 a = trail.GetHistory(i);
			Vector2 b = trail.GetHistory(i + 1);
			float distance = Vector2.Distance(a, b);
			if (distance < 1.5f || distance > 160f)
				continue;

			float strength = 1f - i / (float)trail.HistoryCount;
			Vector2 offset = new Vector2(FxRandom.Signed(seed, i, 1), FxRandom.Signed(seed, i, 2)) * 5f;
			Fx.Lightning(f, f.WorldToScreen(a), f.WorldToScreen(b) + offset, seed * 31 + i, Palette, strength);
		}

		base.DrawBehind(f);
	}

	protected override void DrawParticle(in FxFrame f, ref Particle p, Vector2 position)
	{
		f.Line(position, position - p.Velocity * 3f, Palette.Core * Fade(p), 1.5f);
		f.Bloom(position, Palette.PrimaryAt(f.Time * 0.1f) * (0.5f * Fade(p)), p.Scale);
	}
}

public class FrostTrail(string name, FxPalette palette) : TrailEffect(name, palette)
{
	protected override float Drag => 0.97f;

	protected override float Gravity => 0.04f;

	protected override float EmissionRate(float speed) => speed > 1f ? 0.35f + speed * 0.1f : 0.05f;

	protected override void Emit(Player player, ref Particle p)
	{
		p.Position = RandomPointIn(player);
		p.Velocity = -player.velocity * 0.1f + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(0.3f, 1f));
		p.MaxLife = p.Life = Main.rand.Next(50, 85);
		p.Kind = (byte)(Main.rand.NextBool(3) ? 1 : 0);
		p.Scale = p.Kind == 0 ? Main.rand.NextFloat(0.18f, 0.32f) : Main.rand.NextFloat(0.3f, 0.45f);
		p.Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		p.Spin = Main.rand.NextFloat(-0.06f, 0.06f);
	}

	protected override void DrawParticle(in FxFrame f, ref Particle p, Vector2 position)
	{
		float fade = Fade(p);
		if (p.Kind == 0) {
			Texture2D flake = FxTextures.Snowflake.Value;
			f.Glow(flake, position, null, Palette.Core * fade, p.Rotation, flake.Size() * 0.5f, new Vector2(p.Scale));
		}
		else {
			Texture2D crystal = FxTextures.Crystal.Value;
			f.Draw(crystal, position, null, Fx.Lit(f, position, Palette.PrimaryAt(0f)) * (fade * 0.9f), p.Rotation, crystal.Size() * 0.5f, new Vector2(p.Scale));
			f.Glow(crystal, position, null, Palette.Core * (fade * 0.3f), p.Rotation, crystal.Size() * 0.5f, new Vector2(p.Scale));
		}
	}
}

public class BubbleTrail(string name, FxPalette palette) : TrailEffect(name, palette)
{
	protected override float Drag => 0.97f;

	protected override float Gravity => -0.02f;

	protected override float EmissionRate(float speed) => speed > 1f ? 0.25f + speed * 0.08f : 0.05f;

	protected override void Emit(Player player, ref Particle p)
	{
		p.Position = RandomPointIn(player);
		p.Velocity = -player.velocity * 0.12f + new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.1f, 0.5f));
		p.MaxLife = p.Life = Main.rand.Next(60, 100);
		p.Scale = Main.rand.NextFloat(0.15f, 0.38f);
	}

	protected override void DrawParticle(in FxFrame f, ref Particle p, Vector2 position)
	{
		Texture2D bubble = FxTextures.Bubble.Value;
		float k = p.Progress;
		float pop = k > 0.9f ? 1f + (k - 0.9f) * 6f : 1f;
		float alpha = MathF.Min(1f, k * 10f) * (k > 0.9f ? (1f - k) * 10f : 1f);
		position.X += MathF.Sin(f.Time * 3f + p.Seed * 9f) * 2f;
		f.Draw(bubble, position, null, Fx.Lit(f, position, Palette.Core) * (alpha * 0.85f), 0f, bubble.Size() * 0.5f, new Vector2(p.Scale * pop));
		f.Glow(bubble, position, null, Palette.PrimaryAt(f.Time * 0.1f) * (alpha * 0.2f), 0f, bubble.Size() * 0.5f, new Vector2(p.Scale * pop * 1.2f));
	}
}

/// <summary> Tinted, fading copies of the player following them like a phantom. </summary>
public class AfterimageTrail(string name, FxPalette palette) : TrailEffect(name, palette, 1, 16)
{
	private static readonly int[] copyAges = [12, 8, 4];

	protected override float EmissionRate(float speed) => 0f;

	protected override void Emit(Player player, ref Particle p)
	{
	}

	protected override void DrawParticle(in FxFrame f, ref Particle p, Vector2 screenPosition)
	{
	}

	public override void DrawPlayerCopies(Player player, Camera camera, EffectState state)
	{
		if (state is not TrailState trail || trail.HistoryCount < copyAges[0] + 1)
			return;

		for (int i = 0; i < copyAges.Length; i++) {
			Vector2 center = trail.GetHistory(copyAges[i]);
			// Only show copies while actually moving.
			if (Vector2.DistanceSquared(center, player.Center) < 6f * 6f || Vector2.DistanceSquared(center, player.Center) > 400f * 400f)
				continue;

			// Older copies are fainter.
			float shadow = 0.65f - i * 0.1f;
			Main.PlayerRenderer.DrawPlayer(camera, player, center - player.Size * 0.5f, player.fullRotation, player.fullRotationOrigin, shadow);
		}
	}

	public override void TransformCopy(ref PlayerDrawSet drawInfo)
	{
		Color tint = FxFrame.Additive(Palette.PrimaryAt(Main.GlobalTimeWrappedHourly * 0.3f));
		var cache = drawInfo.DrawDataCache;
		for (int i = 0; i < cache.Count; i++) {
			DrawData data = cache[i];
			float alpha = data.color.A / 255f;
			data.color = tint * (alpha * 0.85f);
			cache[i] = data;
		}
	}
}
