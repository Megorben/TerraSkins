using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;

namespace TerraSkins.Common.Effects;

/// <summary>
/// Everything an effect needs to draw one frame for one player, plus small drawing helpers.
/// All positions are in screen space.
/// </summary>
public readonly struct FxFrame
{
	public FxFrame(List<DrawData> cache, Player player, EffectState state, Vector2 center, float time, float opacity, bool inWorld)
	{
		Cache = cache;
		Player = player;
		State = state;
		Center = center;
		Time = time;
		Opacity = opacity;
		InWorld = inWorld;
		Direction = player.direction;
		GravDir = player.gravDir;
	}

	public List<DrawData> Cache { get; }

	public Player Player { get; }

	public EffectState State { get; }

	/// <summary> Center of the player's hitbox on screen. </summary>
	public Vector2 Center { get; }

	/// <summary> Seconds, offset per player so that two players never animate in lockstep. </summary>
	public float Time { get; }

	/// <summary> Global opacity of the effect (config, invisibility, afterimages). </summary>
	public float Opacity { get; }

	/// <summary> False while drawn in the wardrobe preview. World-space effects (trails) should not draw then. </summary>
	public bool InWorld { get; }

	public int Direction { get; }

	public float GravDir { get; }

	/// <summary> Top center of the player's head, accounting for gravity. </summary>
	public Vector2 Head => Center + new Vector2(0f, -Player.height * 0.5f * GravDir);

	/// <summary> Bottom center of the player (feet), accounting for gravity. </summary>
	public Vector2 Feet => Center + new Vector2(0f, Player.height * 0.5f * GravDir);

	/// <summary> Converts an offset where +Y is "down for the player" to screen space (flipped under reversed gravity). </summary>
	public Vector2 Local(float x, float y) => Center + new Vector2(x, y * GravDir);

	public Vector2 WorldToScreen(Vector2 world) => world - Main.screenPosition;

	/// <summary> Normal alpha-blended draw. </summary>
	public void Draw(Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects = SpriteEffects.None)
	{
		Cache.Add(new DrawData(texture, position, source, color * Opacity, rotation, origin, scale, effects));
	}

	/// <summary> Additive draw (premultiplied colors with zero alpha add up instead of covering). </summary>
	public void Glow(Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects = SpriteEffects.None)
	{
		Cache.Add(new DrawData(texture, position, source, Additive(color) * Opacity, rotation, origin, scale, effects));
	}

	/// <summary> Additive draw of a whole texture around its center. </summary>
	public void Glow(Texture2D texture, Vector2 position, Color color, float rotation, Vector2 scale)
	{
		Glow(texture, position, null, color, rotation, texture.Size() * 0.5f, scale);
	}

	public void Glow(Texture2D texture, Vector2 position, Color color, float rotation, float scale) => Glow(texture, position, color, rotation, new Vector2(scale));

	/// <summary> Alpha-blended draw of a whole texture around its center. </summary>
	public void Draw(Texture2D texture, Vector2 position, Color color, float rotation, Vector2 scale)
	{
		Draw(texture, position, null, color, rotation, texture.Size() * 0.5f, scale);
	}

	/// <summary> Soft additive light blob of the given radius in pixels. </summary>
	public void Bloom(Vector2 position, Color color, float radius)
	{
		Texture2D tex = FxTextures.Glow.Value;
		Glow(tex, position, color, 0f, radius * 2f / tex.Width);
	}

	/// <summary> A thin additive line between two points. </summary>
	public void Line(Vector2 from, Vector2 to, Color color, float thickness)
	{
		Vector2 delta = to - from;
		float length = delta.Length();
		if (length < 0.5f)
			return;

		Texture2D tex = FxTextures.Pixel.Value;
		Glow(tex, from, null, color, delta.ToRotation(), new Vector2(0f, tex.Height * 0.5f), new Vector2(length / tex.Width, thickness / tex.Height));
	}

	/// <summary> Four point sparkle with a soft glow, the bread and butter of every effect. </summary>
	public void Sparkle(Vector2 position, Color color, float size, float rotation = 0f)
	{
		Texture2D star = FxTextures.Star.Value;
		Glow(star, position, color, rotation, size / star.Width);
		Glow(star, position, Color.White * 0.8f, rotation, size * 0.45f / star.Width);
	}

	public static Color Additive(Color color) => new(color.R, color.G, color.B, 0);
}

/// <summary> Deterministic pseudo random numbers for stateless particle animation. </summary>
public static class FxRandom
{
	public static uint Hash(int a, int b = 0, int c = 0)
	{
		unchecked {
			uint h = (uint)a * 0x9E3779B1u ^ (uint)b * 0x85EBCA77u ^ (uint)c * 0xC2B2AE3Du;
			h ^= h >> 15;
			h *= 0x2C1B3C6Du;
			h ^= h >> 12;
			h *= 0x297A2D39u;
			h ^= h >> 15;
			return h;
		}
	}

	/// <summary> A value in [0, 1). </summary>
	public static float Float(int a, int b = 0, int c = 0) => (Hash(a, b, c) & 0xFFFFFF) / 16777216f;

	/// <summary> A value in [-1, 1). </summary>
	public static float Signed(int a, int b = 0, int c = 0) => Float(a, b, c) * 2f - 1f;

	/// <summary>
	/// Stateless particle helper: particle <paramref name="index"/> of a stream that emits one particle every
	/// <paramref name="lifetime"/>/<paramref name="count"/> seconds. Returns the normalized age in [0, 1) and the
	/// generation counter which changes every time the particle respawns (use it to seed randomness).
	/// </summary>
	public static float Cycle(float time, int index, int count, float lifetime, out int generation)
	{
		float t = time / lifetime + index / (float)count;
		float floor = MathF.Floor(t);
		generation = (int)floor;
		return t - floor;
	}
}
