using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TerraSkins.Common.Configs;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins.Common.Effects;

public static class EffectVisibility
{
	/// <summary> Whether the effects of this player should be drawn at all on this client. </summary>
	public static bool ShouldDraw(Player player)
	{
		if (Main.dedServ || player.dead || player.invis || player.ShouldNotDraw)
			return false;

		var config = TerraSkinsClientConfig.Instance;
		if (config == null)
			return true;

		var wardrobe = player.GetModPlayer<WardrobePlayer>();
		if (wardrobe.IsPreviewDummy)
			return config.ShowPreviewEffects;

		return player.whoAmI == Main.myPlayer ? config.ShowOwnEffects : config.ShowOtherPlayersEffects;
	}

	public static float Opacity(Player player)
	{
		float opacity = TerraSkinsClientConfig.Instance?.EffectOpacity ?? 1f;
		// Stealth (shroomite, vortex...) also fades the cosmetics, they must never give a player away.
		return opacity * Math.Clamp(player.stealth, 0f, 1f);
	}
}

/// <summary> Shared logic of the three effect layers. </summary>
public abstract class SkinEffectLayer : PlayerDrawLayer
{
	public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
	{
		return drawInfo.shadow == 0f && drawInfo.drawPlayer.GetModPlayer<WardrobePlayer>().HasAnyActiveEffect && EffectVisibility.ShouldDraw(drawInfo.drawPlayer);
	}

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		Player player = drawInfo.drawPlayer;
		var wardrobe = player.GetModPlayer<WardrobePlayer>();
		float opacity = EffectVisibility.Opacity(player);
		if (opacity <= 0f)
			return;

		var center = drawInfo.Center - Main.screenPosition;
		// Per player phase so that two players with the same effect do not animate in lockstep.
		float time = FxClock.Seconds + player.whoAmI * 7.31f;
		bool inWorld = !wardrobe.IsPreviewDummy && !Main.gameMenu;

		foreach (WardrobeSlot slot in WardrobeSlots.All) {
			if (wardrobe.GetActiveEffect(slot) is not { } effect)
				continue;

			var frame = new FxFrame(drawInfo.DrawDataCache, player, wardrobe.GetEffectState(slot), center, time, opacity, inWorld);
			DrawEffect(effect, frame);
		}
	}

	protected abstract void DrawEffect(SkinEffect effect, in FxFrame frame);
}

public class SkinEffectBackLayer : SkinEffectLayer
{
	public override Position GetDefaultPosition() => PlayerDrawLayers.BeforeFirstVanillaLayer;

	protected override void DrawEffect(SkinEffect effect, in FxFrame frame) => effect.DrawBehind(frame);
}

public class SkinEffectWingsLayer : SkinEffectLayer
{
	public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.HairBack);

	protected override void DrawEffect(SkinEffect effect, in FxFrame frame) => effect.DrawWings(frame);
}

public class SkinEffectFrontLayer : SkinEffectLayer
{
	public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;

	protected override void DrawEffect(SkinEffect effect, in FxFrame frame) => effect.DrawFront(frame);
}
