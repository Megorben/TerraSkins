using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace TerraSkins.Common.UI.Elements;

/// <summary> A square tab with an icon from <see cref="UIStyle.SlotIcons"/>. </summary>
public class UISlotTab : UIElement
{
	private readonly int iconIndex;
	private readonly Func<string> tooltip;
	private readonly Func<bool> isSelected;
	private readonly Func<bool> isMarked;

	/// <param name="iconIndex">Index of the 24x24 icon in the strip.</param>
	/// <param name="isMarked">Draws a small dot, used for slots that the wardrobe overrides.</param>
	public UISlotTab(int iconIndex, Func<string> tooltip, Func<bool> isSelected, Func<bool> isMarked)
	{
		this.iconIndex = iconIndex;
		this.tooltip = tooltip;
		this.isSelected = isSelected;
		this.isMarked = isMarked;
		Width.Set(30f, 0f);
		Height.Set(30f, 0f);
	}

	public override void LeftClick(UIMouseEvent evt)
	{
		SoundEngine.PlaySound(SoundID.MenuTick);
		base.LeftClick(evt);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		bool selected = isSelected();
		UIStyle.DrawPanel(spriteBatch, area, selected ? UIStyle.PanelActive : IsMouseHovering ? UIStyle.PanelHover : UIStyle.Panel);

		Texture2D icons = UIStyle.SlotIcons;
		const int size = 24;
		var source = new Rectangle(iconIndex * size, 0, size, size);
		Color color = selected ? Color.White : IsMouseHovering ? new Color(230, 225, 255) : new Color(190, 180, 220);
		spriteBatch.Draw(icons, area.Center.ToVector2(), source, color, 0f, new Vector2(size * 0.5f), 1f, SpriteEffects.None, 0f);

		if (isMarked()) {
			var dot = new Rectangle(area.Right - 8, area.Y + 4, 4, 4);
			spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, dot, UIStyle.Gold);
		}

		if (IsMouseHovering)
			Main.hoverItemName = tooltip();
	}
}

/// <summary> A small toggle chip with a text, used for the source filter. </summary>
public class UIChip : UIElement
{
	private readonly Func<string> text;
	private readonly Func<bool> isSelected;

	public UIChip(Func<string> text, Func<bool> isSelected)
	{
		this.text = text;
		this.isSelected = isSelected;
		Height.Set(24f, 0f);
	}

	/// <summary> Sizes the chip to its text. </summary>
	public void FitWidth()
	{
		float textWidth = Terraria.GameContent.FontAssets.MouseText.Value.MeasureString(text()).X * 0.75f;
		Width.Set(Math.Min(textWidth + 16f, 150f), 0f);
	}

	public override void LeftClick(UIMouseEvent evt)
	{
		SoundEngine.PlaySound(SoundID.MenuTick);
		base.LeftClick(evt);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		bool selected = isSelected();
		UIStyle.DrawPanel(spriteBatch, area, selected ? UIStyle.PanelActive : IsMouseHovering ? UIStyle.PanelHover : UIStyle.Panel);
		string label = UIStyle.Fit(text(), area.Width - 10, 0.75f);
		UIStyle.DrawText(spriteBatch, label, area.Center.ToVector2() + new Vector2(0f, 2f), selected ? UIStyle.Gold : UIStyle.Text, 0.75f, 0.5f, 0.5f);
	}
}
