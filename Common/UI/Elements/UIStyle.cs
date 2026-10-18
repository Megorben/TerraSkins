using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace TerraSkins.Common.UI.Elements;

/// <summary> Colors and shared textures of the wardrobe window. </summary>
public static class UIStyle
{
	public static readonly Color Window = new(28, 22, 52, 235);
	public static readonly Color Panel = new(44, 36, 82, 220);
	public static readonly Color PanelHover = new(70, 58, 122, 235);
	public static readonly Color PanelActive = new(118, 86, 190, 245);
	public static readonly Color Border = new(150, 120, 230);
	public static readonly Color Gold = new(255, 214, 120);
	public static readonly Color Text = new(235, 230, 255);
	public static readonly Color TextDim = new(160, 150, 195);
	public static readonly Color Accent = new(190, 140, 255);

	private const string Folder = "TerraSkins/Assets/Textures/UI/";

	private static Asset<Texture2D> slotIcons;
	private static Asset<Texture2D> cellIcons;
	private static Asset<Texture2D> previewBackground;

	/// <summary> 24x24 icons, one per tab: outfits first, then one per <see cref="Wardrobe.WardrobeSlot"/>. </summary>
	public static Texture2D SlotIcons => (slotIcons ??= ModContent.Request<Texture2D>(Folder + "SlotIcons")).Value;

	/// <summary> 32x32 icons for the special grid cells: own equipment, hidden, no dye, save outfit. </summary>
	public static Texture2D CellIcons => (cellIcons ??= ModContent.Request<Texture2D>(Folder + "CellIcons")).Value;

	public static Texture2D PreviewBackground => (previewBackground ??= ModContent.Request<Texture2D>(Folder + "PreviewBackground")).Value;

	internal static void Unload()
	{
		slotIcons = cellIcons = previewBackground = null;
	}

	/// <summary> A rounded panel like the vanilla inventory ones. </summary>
	public static void DrawPanel(SpriteBatch spriteBatch, Rectangle area, Color color)
	{
		Utils.DrawInvBG(spriteBatch, area, color);
	}

	public static void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1f, float anchorX = 0f, float anchorY = 0f)
	{
		Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
		Utils.DrawBorderString(spriteBatch, text, position - new Vector2(size.X * anchorX, size.Y * anchorY), color, scale);
	}

	/// <summary> Shortens a text with an ellipsis so that it fits the width. </summary>
	public static string Fit(string text, float maxWidth, float scale = 1f)
	{
		var font = FontAssets.MouseText.Value;
		if (font.MeasureString(text).X * scale <= maxWidth)
			return text;

		while (text.Length > 1 && font.MeasureString(text + "…").X * scale > maxWidth)
			text = text[..^1];
		return text + "…";
	}
}
