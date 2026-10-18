using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace TerraSkins.Common.UI.Elements;

/// <summary> A one line text field. Click to type, click elsewhere, Enter or Escape to stop. </summary>
public class UISearchField : UIElement
{
	private readonly Func<string> hint;
	private int blinkCounter;

	public UISearchField(Func<string> hint)
	{
		this.hint = hint;
		Height.Set(24f, 0f);
	}

	public string Text { get; private set; } = string.Empty;

	public bool Focused { get; private set; }

	public event Action<string> OnTextChanged;

	public void SetText(string text)
	{
		text ??= string.Empty;
		if (text == Text)
			return;

		Text = text;
		OnTextChanged?.Invoke(Text);
	}

	public void Unfocus() => Focused = false;

	public override void LeftClick(UIMouseEvent evt)
	{
		base.LeftClick(evt);
		Main.clrInput();
		Focused = true;
	}

	public override void RightClick(UIMouseEvent evt)
	{
		base.RightClick(evt);
		SetText(string.Empty);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (Focused && Main.mouseLeft && !ContainsPoint(Main.MouseScreen))
			Focused = false;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		UIStyle.DrawPanel(spriteBatch, area, Focused ? UIStyle.PanelHover : UIStyle.Panel);

		if (Focused) {
			PlayerInput.WritingText = true;
			Main.instance.HandleIME();
			string input = Main.GetInputText(Text);
			if (Main.inputTextEnter || Main.inputTextEscape) {
				Main.inputTextEscape = false;
				Focused = false;
			}
			if (input.Length > 40)
				input = input[..40];
			SetText(input);
		}

		var textPosition = new Vector2(area.X + 8, area.Y + 5);
		if (Text.Length == 0 && !Focused) {
			UIStyle.DrawText(spriteBatch, hint(), textPosition, UIStyle.TextDim, 0.8f);
			return;
		}

		string shown = Text;
		if (Focused && ++blinkCounter / 25 % 2 == 0)
			shown += "|";
		UIStyle.DrawText(spriteBatch, UIStyle.Fit(shown, area.Width - 14, 0.8f), textPosition, UIStyle.Text, 0.8f);
	}
}
