using System.IO;
using Terraria.IO;

namespace WorldLink;

public static class ModUtils
{
    public static string Id(this PlayerFileData playerData) => GetPlayerId(playerData.Path);

    public static string GetPlayerId(string playerPath) => Path.GetFileNameWithoutExtension(playerPath);

    public static string Id(this WorldFileData worldData) => worldData.UniqueId.ToString();
}