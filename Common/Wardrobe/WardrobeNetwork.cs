using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerraSkins.Common.Wardrobe;

public static class WardrobeNetwork
{
	private enum MessageType : byte
	{
		SyncLoadout,
	}

	public static void SendLoadout(Mod mod, int playerIndex, int toWho, int fromWho)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
			return;

		var wardrobe = Main.player[playerIndex].GetModPlayer<WardrobePlayer>();
		ModPacket packet = mod.GetPacket();
		packet.Write((byte)MessageType.SyncLoadout);
		packet.Write((byte)playerIndex);
		packet.Write(wardrobe.Enabled);
		wardrobe.Loadout.Write(packet);
		packet.Send(toWho, fromWho);
	}

	public static void HandlePacket(Mod mod, BinaryReader reader, int whoAmI)
	{
		var type = (MessageType)reader.ReadByte();
		switch (type) {
			case MessageType.SyncLoadout: {
				int playerIndex = reader.ReadByte();
				// Clients may only change their own look.
				if (Main.netMode == NetmodeID.Server)
					playerIndex = whoAmI;

				var wardrobe = Main.player[playerIndex].GetModPlayer<WardrobePlayer>();
				wardrobe.Enabled = reader.ReadBoolean();
				wardrobe.Loadout.Read(reader);

				if (Main.netMode == NetmodeID.Server)
					SendLoadout(mod, playerIndex, -1, whoAmI);
				break;
			}
			default:
				mod.Logger.Warn($"Unknown packet type {type}");
				break;
		}
	}
}
