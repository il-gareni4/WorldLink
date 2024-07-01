using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Terraria;
using Terraria.ModLoader;

namespace WorldLink;

public static class ModSave
{
    private static readonly string SaveDirPath = Path.Join(Main.SavePath, "WorldLink");
    private static readonly string SaveFilePath = Path.Join(SaveDirPath, "Links.json");

    private static Dictionary<string, string> _links = new();

    static ModSave()
    {
        if (!Directory.Exists(SaveDirPath))
            Directory.CreateDirectory(SaveDirPath);
        if (!File.Exists(SaveFilePath))
            File.WriteAllText(SaveFilePath, "{}");

        LoadLinks();
    }

    private static void LoadLinks()
    {
        try
        {
            _links = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllBytes(SaveFilePath))
                     ?? new Dictionary<string, string>();
        }
        catch (Exception e)
        {
            ModLoader.GetMod("WorldLink").Logger.Error($"Exception caught while loading links: {e}");
            _links.Clear();
        }
    }

    private static void SaveLinks()
    {
        try
        {
            File.WriteAllText(SaveFilePath, JsonSerializer.Serialize(_links));
        }
        catch (Exception e)
        {
            ModLoader.GetMod("WorldLink").Logger.Error($"Exception caught while saving links: {e}");
        }
    }

    public static string GetLink(string playerId) => _links[playerId];

    public static bool HasLink(string playerId) => _links.ContainsKey(playerId);

    public static bool TryGetLink(string playerId, [NotNullWhen(true)] out string? worldId) =>
        _links.TryGetValue(playerId, out worldId);

    public static void SetLink(string playerId, string worldId)
    {
        _links[playerId] = worldId;
        SaveLinks();
    }

    public static void RemoveLink(string playerId)
    {
        _links.Remove(playerId);
        SaveLinks();
    }
}