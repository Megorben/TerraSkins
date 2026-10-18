using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace TerraSkins.Common.UI;

/// <summary> Owns the wardrobe window: opening, closing, updating and drawing it. </summary>
public class WardrobeUISystem : ModSystem
{
	public static ModKeybind OpenWardrobeKeybind { get; private set; }

	private static UserInterface userInterface;
	private static WardrobeState state;
	private GameTime lastGameTime;

	public static bool IsOpen => userInterface?.CurrentState != null;

	public override void Load()
	{
		OpenWardrobeKeybind = KeybindLoader.RegisterKeybind(Mod, "OpenWardrobe", "P");

		if (Main.dedServ)
			return;

		userInterface = new UserInterface();
	}

	public override void Unload()
	{
		OpenWardrobeKeybind = null;
		userInterface = null;
		state = null;
		Elements.UIStyle.Unload();
	}

	public static void Toggle()
	{
		if (IsOpen)
			Close();
		else
			Open();
	}

	public static void Open()
	{
		if (userInterface == null || Main.gameMenu)
			return;

		state ??= new WardrobeState();
		userInterface.SetState(state);
		SoundEngine.PlaySound(SoundID.MenuOpen);
	}

	public static void Close()
	{
		if (!IsOpen)
			return;

		userInterface.SetState(null);
		SoundEngine.PlaySound(SoundID.MenuClose);
	}

	public override void UpdateUI(GameTime gameTime)
	{
		lastGameTime = gameTime;
		if (!IsOpen)
			return;

		// Leaving the world or dying closes the window.
		if (Main.gameMenu || Main.LocalPlayer.dead) {
			userInterface.SetState(null);
			return;
		}

		userInterface.Update(gameTime);
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		if (index == -1)
			return;

		layers.Insert(index, new LegacyGameInterfaceLayer("TerraSkins: Wardrobe", () => {
			if (IsOpen)
				userInterface.Draw(Main.spriteBatch, lastGameTime ?? new GameTime());
			return true;
		}, InterfaceScaleType.UI));
	}

	public override void PreSaveAndQuit()
	{
		userInterface?.SetState(null);
		state = null;
	}
}

/// <summary> Opens the wardrobe with the keybind. </summary>
public class WardrobeKeybindPlayer : ModPlayer
{
	public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet)
	{
		if (WardrobeUISystem.OpenWardrobeKeybind?.JustPressed == true)
			WardrobeUISystem.Toggle();
	}
}
