using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace TerraSkins.Common.UI.Elements;

/// <summary> A rounded text button. The text and the "active" state are pulled every frame so they never go stale. </summary>
public class UIWardrobeButton : UIElement
{
	private readonly Func<string> text;
	private readonly Func<bool> isActive;
	private readonly Func<bool> isEnabled;
	private readonly Func<string> tooltip;

	public UIWardrobeButton(Func<string> text, Func<bool> isActive = null, Func<bool> isEnabled = null, Func<string> tooltip = null)
	{
		this.text = text;
		this.isActive = isActive;
		this.isEnabled = isEnabled;
		this.tooltip = tooltip;
	}

	public float TextScale { get; set; } = 0.85f;

	public bool Enabled => isEnabled?.Invoke() ?? true;

	public override void LeftClick(UIMouseEvent evt)
	{
		if (!Enabled)
			return;

		SoundEngine.PlaySound(SoundID.MenuTick);
		base.LeftClick(evt);
	}

	public override void MouseOver(UIMouseEvent evt)
	{
		base.MouseOver(evt);
		if (Enabled)
			SoundEngine.PlaySound(SoundID.MenuTick);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		bool active = isActive?.Invoke() ?? false;
		bool enabled = Enabled;
		Color color = !enabled ? UIStyle.Panel * 0.6f : active ? UIStyle.PanelActive : IsMouseHovering ? UIStyle.PanelHover : UIStyle.Panel;
		UIStyle.DrawPanel(spriteBatch, area, color);

		string label = UIStyle.Fit(text(), area.Width - 10, TextScale);
		Color textColor = !enabled ? UIStyle.TextDim * 0.7f : active ? UIStyle.Gold : UIStyle.Text;
		UIStyle.DrawText(spriteBatch, label, area.Center.ToVector2() + new Vector2(0f, 3f), textColor, TextScale, 0.5f, 0.5f);

		if (IsMouseHovering && tooltip != null)
			Main.hoverItemName = tooltip();
	}
}
