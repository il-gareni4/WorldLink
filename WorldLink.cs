using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace WorldLink;

public class WorldLink : Mod
{
    private Asset<Texture2D> _chainButtonTexture;
    private LocalizedText _linkText;
    private Asset<Texture2D> _noChainButtonTexture;
    private LocalizedText _noWorldText;
    private Asset<Texture2D> _playChainButtonTexture;
    private LocalizedText _playWorldText;
    private LocalizedText _unlinkText;

    public override void Load()
    {
        _noWorldText = Language.GetText("Mods.WorldLink.UI.CharacterListItem.NoWorld");
        _playWorldText = Language.GetText("Mods.WorldLink.UI.CharacterListItem.PlayWorld");
        _linkText = Language.GetText("Mods.WorldLink.UI.WorldListItem.Link");
        _unlinkText = Language.GetText("Mods.WorldLink.UI.WorldListItem.Unlink");

        _chainButtonTexture = Assets.Request<Texture2D>("Assets/ChainButton");
        _noChainButtonTexture = Assets.Request<Texture2D>("Assets/NoChainButton");
        _playChainButtonTexture = Assets.Request<Texture2D>("Assets/PlayChainButton");

        IL_UICharacterListItem.ctor += AddCharacterLinkButton;
        IL_UIWorldListItem.ctor += AddWorldLinkButton;
    }

    public override void Unload()
    {
        _noWorldText = null;
        _playWorldText = null;
        _linkText = null;
        _unlinkText = null;

        _chainButtonTexture = null;
        _noChainButtonTexture = null;
        _playChainButtonTexture = null;

        IL_UICharacterListItem.ctor -= AddCharacterLinkButton;
        IL_UIWorldListItem.ctor -= AddWorldLinkButton;
    }

    private static WorldFileData? GetWorldDataById(string worldId)
    {
        if (Main.WorldList.Count == 0) Main.LoadWorlds();
        return Main.WorldList.FirstOrDefault(worldData => worldData!.Id() == worldId, null);
    }

    private static void PlayWorld(WorldFileData worldData)
    {
        UIWorldListItem worldUi = new(worldData, 0, true);
        var playButton = (UIImageButton)worldUi.Children.ToArray()[1];
        UIMouseEvent evt = new(playButton, new Vector2(Main.mouseX, Main.mouseY));

        worldUi.GetType()
            .GetMethod("PlayGame", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(worldUi, [evt, playButton]);
    }

    private static UIText? GetButtonLabel(UICharacterListItem listItem)
    {
        // If it's vanilla UI Element
        if (listItem.GetType() == typeof(UICharacterListItem))
        {
            return (UIText?)listItem.GetType()
                .GetField("_buttonLabel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(listItem);
        }

        return (UIText)listItem.Children.First(el => el is UIText && el.Left.Pixels > 0 && el.Top.Pixels < 0);
    }

    private static UIText? GetButtonLabel(UIWorldListItem listItem)
    {
        // If it's vanilla UI Element
        if (listItem.GetType() == typeof(UIWorldListItem))
        {
            return (UIText?)listItem.GetType()
                .GetField("_buttonLabel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(listItem);
        }

        return (UIText)listItem.Children.First(el => el is UIText && el.Left.Pixels > 0 && el.Top.Pixels < 0);
    }

    private void AddCharacterLinkButton(ILContext il)
    {
        try
        {
            ILCursor c = new(il)
            {
                Index = 140
            };

            c.EmitLdarg0();
            c.EmitLdarg1();
            c.EmitLdloca(0);
            c.EmitLdarg2();
            c.EmitDelegate((UICharacterListItem listItem, PlayerFileData playerData, ref float shift, int order) =>
            {
                string playerId = playerData.Id();

                if (ModSave.HasLink(playerId))
                {
                    WorldFileData? worldData = GetWorldDataById(ModSave.GetLink(playerId));
                    if (worldData is not { IsValid: true }) ModSave.RemoveLink(playerId);
                }

                if ((!ModSave.HasLink(playerId) && !LinkConfig.Instance.ShowNoWorldLinkedPlayButton) ||
                    (Main.menuMultiplayer && !Main.menuServer))
                    return;

                UIImageButton linkButton = new(_noChainButtonTexture)
                {
                    VAlign = 1f,
                    Left = StyleDimension.FromPixelsAndPercent(shift, 0f)
                };
                if (ModSave.HasLink(playerId)) linkButton.SetImage(_playChainButtonTexture);

                linkButton.OnLeftClick += (_, _) =>
                {
                    if (!ModSave.HasLink(playerId)) return;
                    WorldFileData? worldData = GetWorldDataById(ModSave.GetLink(playerId));
                    if (worldData is not { IsValid: true }) return;

                    playerData.SetAsActive();
                    PlayWorld(worldData);
                };

                linkButton.OnRightClick += (_, _) =>
                {
                    if (!ModSave.HasLink(playerId)) return;

                    ModSave.RemoveLink(playerId);
                    linkButton.SetImage(_noChainButtonTexture);
                };

                linkButton.OnMouseOver += (_, _) =>
                {
                    if (ModSave.HasLink(playerId))
                    {
                        WorldFileData? worldData = GetWorldDataById(ModSave.GetLink(playerId));
                        if (worldData is not { IsValid: true }) return;
                        GetButtonLabel(listItem)?.SetText(_playWorldText.Format(worldData.Name));
                    }
                    else GetButtonLabel(listItem)?.SetText(_noWorldText.Value);
                };
                MethodInfo? defaultOut = listItem.GetType()
                    .GetMethod("ButtonMouseOut", BindingFlags.Instance | BindingFlags.NonPublic);
                if (defaultOut is not null)
                    linkButton.OnMouseOut += defaultOut.CreateDelegate<UIElement.MouseEvent>(listItem);
                else
                    linkButton.OnMouseOut += (evt, el) => GetButtonLabel(listItem)?.SetText("");
                linkButton.SetSnapPoint("Link", order);
                listItem.Append(linkButton);

                shift += 24f;
            });
        }
        catch
        {
            MonoModHooks.DumpIL(ModContent.GetInstance<WorldLink>(), il);
        }
    }

    private void UpdateWorldListItemText(UIWorldListItem listItem, WorldFileData worldData)
    {
        GetButtonLabel(listItem)?.SetText(
            ModSave.TryGetLink(Main.ActivePlayerFileData.Id(), out string? worldId) &&
            worldId == worldData.UniqueId.ToString()
                ? _unlinkText.Format(Main.ActivePlayerFileData.Name)
                : _linkText.Format(Main.ActivePlayerFileData.Name)
        );
    }

    private void AddWorldLinkButton(ILContext il)
    {
        try
        {
            ILCursor c = new(il)
            {
                Index = 305
            };
            c.EmitLdarg0();
            c.EmitLdarg1();
            c.EmitLdloca(0);
            c.EmitLdarg2();
            c.EmitLdarg3();
            c.EmitDelegate((UIWorldListItem listItem, WorldFileData worldData, ref float shift, int order,
                bool canBePlayed) =>
            {
                if (!canBePlayed) return;
                string worldId = worldData.Id();

                UIImageButton linkButton = new(_chainButtonTexture)
                {
                    VAlign = 1f,
                    Left = StyleDimension.FromPixelsAndPercent(shift, 0f)
                };
                if (ModSave.HasLink(Main.ActivePlayerFileData.Id())) linkButton.SetImage(_noChainButtonTexture);

                linkButton.OnLeftClick += (_, _) =>
                {
                    string playerId = Main.ActivePlayerFileData.Id();
                    if (ModSave.TryGetLink(playerId, out string? linkedWorldId) &&
                        linkedWorldId == worldId)
                        ModSave.RemoveLink(playerId);
                    else ModSave.SetLink(playerId, worldId);

                    if (linkButton.IsMouseHovering) UpdateWorldListItemText(listItem, worldData);
                };
                linkButton.OnUpdate += _ =>
                {
                    if (ModSave.TryGetLink(Main.ActivePlayerFileData.Id(), out string? linkedWorldId) &&
                        linkedWorldId == worldData.Id())
                        linkButton.SetImage(_noChainButtonTexture);
                    else
                        linkButton.SetImage(_chainButtonTexture);
                };
                linkButton.OnMouseOver += (_, _) => UpdateWorldListItemText(listItem, worldData);
                MethodInfo? defaultOut = listItem.GetType()
                    .GetMethod("ButtonMouseOut", BindingFlags.Instance | BindingFlags.NonPublic);
                if (defaultOut != null)
                    linkButton.OnMouseOut += defaultOut.CreateDelegate<UIElement.MouseEvent>(listItem);
                else
                    linkButton.OnMouseOut += (_, _) => GetButtonLabel(listItem)?.SetText("");
                linkButton.SetSnapPoint("Link", order);
                listItem.Append(linkButton);

                shift += 24f;
            });
        }
        catch
        {
            MonoModHooks.DumpIL(ModContent.GetInstance<WorldLink>(), il);
        }
    }
}