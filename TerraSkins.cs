using System.IO;
using Terraria.ModLoader;
using TerraSkins.Common.Wardrobe;

namespace TerraSkins;

public class TerraSkins : Mod
{
	public override void HandlePacket(BinaryReader reader, int whoAmI) => WardrobeNetwork.HandlePacket(this, reader, whoAmI);
}
