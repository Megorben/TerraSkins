using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;
using TerraSkins.Common.Configs;

namespace TerraSkins.Common.UI;

/// <summary>
/// A button among the builder toggles (top left of the inventory screen) that opens the wardrobe.
/// It behaves as a button: clicking never changes its state.
/// </summary>
public class WardrobeBuilderToggle : BuilderToggle
{
	public override string Texture => "TerraSkins/Assets/Textures/UI/WardrobeToggle";

	public override string HoverTexture => "TerraSkins/Assets/Textures/UI/WardrobeToggle_Hover";

	public override bool Active() => TerraSkinsClientConfig.Instance?.ShowWardrobeButton ?? true;

	public override int NumberOfStates => 1;

	public override string DisplayValue() => Language.GetTextValue("Mods.TerraSkins.UI.OpenWardrobe");

	public override bool OnLeftClick(ref SoundStyle? sound)
	{
		sound = null;
		WardrobeUISystem.Toggle();
		return false;
	}
}
