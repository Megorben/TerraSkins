using System;
using Microsoft.Xna.Framework;

namespace TerraSkins.Common.Effects;

/// <summary> A simple world-space particle used by trails. </summary>
public struct Particle
{
	public Vector2 Position;
	public Vector2 Velocity;
	public float Rotation;
	public float Spin;
	public float Scale;
	public int Life;
	public int MaxLife;
	public float Seed;
	public byte Kind;

	/// <summary> 0 when born, 1 when about to die. </summary>
	public readonly float Progress => 1f - Life / (float)MaxLife;
}

/// <summary> Fixed capacity pool of particles. Dead particles are compacted away so iteration is dense. </summary>
public sealed class ParticleBuffer
{
	private readonly Particle[] particles;

	public ParticleBuffer(int capacity) => particles = new Particle[capacity];

	public int Count { get; private set; }

	public ref Particle this[int index] => ref particles[index];

	/// <summary> Adds a particle, overwriting the oldest one when full. </summary>
	public ref Particle Spawn()
	{
		if (Count < particles.Length)
			return ref particles[Count++];

		// Full: recycle the particle closest to death.
		int oldest = 0;
		for (int i = 1; i < Count; i++) {
			if (particles[i].Life < particles[oldest].Life)
				oldest = i;
		}
		return ref particles[oldest];
	}

	/// <summary> Moves every particle, applies drag and gravity, and removes the dead ones. </summary>
	public void Update(float drag = 0.96f, float gravity = 0f)
	{
		int alive = 0;
		for (int i = 0; i < Count; i++) {
			ref Particle p = ref particles[i];
			if (--p.Life <= 0)
				continue;

			p.Velocity *= drag;
			p.Velocity.Y += gravity;
			p.Position += p.Velocity;
			p.Rotation += p.Spin;
			particles[alive++] = p;
		}
		Count = alive;
	}

	public void Clear() => Count = 0;
}

/// <summary> State of trail effects: particles plus a short history of where the player was. </summary>
public sealed class TrailState(int capacity, int historyLength) : EffectState
{
	public readonly ParticleBuffer Particles = new(capacity);

	private readonly Vector2[] history = new Vector2[historyLength];
	private int historyStart;

	public int HistoryCount { get; private set; }

	/// <summary> Fractional particle emission carried over between ticks. </summary>
	public float EmitBudget;

	/// <summary> Distance walked since the last footstep, for ground effects. </summary>
	public float StepDistance;

	public void PushHistory(Vector2 position)
	{
		historyStart = (historyStart + history.Length - 1) % history.Length;
		history[historyStart] = position;
		HistoryCount = Math.Min(HistoryCount + 1, history.Length);
	}

	/// <summary> 0 is the newest position. </summary>
	public Vector2 GetHistory(int age) => history[(historyStart + age) % history.Length];
}
