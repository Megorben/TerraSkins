using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace TerraSkins.Common.Configs;

public class TerraSkinsClientConfig : ModConfig
{
	public static TerraSkinsClientConfig Instance;

	public override ConfigScope Mode => ConfigScope.ClientSide;

	[Header("Effects")]
	[DefaultValue(true)]
	public bool ShowOwnEffects;

	[DefaultValue(true)]
	public bool ShowOtherPlayersEffects;

	[Range(0.1f, 1f)]
	[Increment(0.05f)]
	[DefaultValue(1f)]
	public float EffectOpacity;

	[Header("Interface")]
	[DefaultValue(true)]
	public bool ShowWardrobeButton;

	[DefaultValue(true)]
	public bool ShowPreviewEffects;
}
