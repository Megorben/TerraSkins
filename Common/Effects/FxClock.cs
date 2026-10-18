using System.Diagnostics;

namespace TerraSkins.Common.Effects;

/// <summary>
/// Animation clock of the effects. Unlike the game time it keeps running while the game is paused,
/// so the wardrobe preview stays alive with the inventory open in single player.
/// </summary>
public static class FxClock
{
	private static readonly Stopwatch watch = Stopwatch.StartNew();

	/// <summary> Seconds, wrapping every hour to keep float precision. </summary>
	public static float Seconds => (float)(watch.Elapsed.TotalSeconds % 3600.0);
}
