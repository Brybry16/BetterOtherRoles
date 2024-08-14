using HarmonyLib;
using System;
using AmongUs.GameOptions;
using BetterOtherRoles;
using BetterOtherRoles.CustomGameModes;
using BetterOtherRoles.Modules;
using BetterOtherRoles.Players;
using BetterOtherRoles.Utilities;
using TMPro;
using UnityEngine;

namespace BetterOtherRoles.Patches
{
    [HarmonyPatch]
    public static class CredentialsPatch
    {
        public const string ColoredLogo = "<color=#fcba03>Better</color><color=#ff351f>OtherRoles</color>";
        public const string BasedCopyright = "Based on <color=#ff351f>TheOtherRoles</color>";
        public const string CreatorsCopyright = "Created by <color=#7897d6ff>EnoPM</color>";
        public const string DingusRelease = "<color=#f779efff><b>Dingus special edition</b></color>";
        public const string EndOfLine = "\n";

        public static string fullCredentialsVersion =
            $@"<size=100%>{ColoredLogo}</size> v{BetterOtherRolesPlugin.Version.ToString()}{(BetterOtherRolesPlugin.betaNum > 0 ? "-beta" + BetterOtherRolesPlugin.betaNum : "")}";

        public static string fullCredentials =
            $@"{(DevConfig.IsDingusRelease ? $"<size=70%>{DingusRelease}</size>{EndOfLine}" : string.Empty)}<size=70%>{BasedCopyright}</size>";

        public static string mainMenuCredentials =
            $@"{(DevConfig.IsDingusRelease ? $"<size=90%>{DingusRelease}</size>{EndOfLine}" : string.Empty)}{BasedCopyright}{EndOfLine}<b>{CreatorsCopyright}</b>";

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        internal static class PingTrackerPatch
        {
            

            static void Postfix(PingTracker __instance)
            {
                __instance.text.alignment = TextAlignmentOptions.Top;
                var position = __instance.GetComponent<AspectPosition>();
                position.Alignment = AspectPosition.EdgeAlignments.Top;
                if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                {
                    string gameModeText = $"";
                    if (HideNSeek.isHideNSeekGM) gameModeText = $"Hide 'N Seek";
                    else if (HandleGuesser.isGuesserGm) gameModeText = $"Guesser";
                    else if (PropHunt.isPropHuntGM) gameModeText = "Prop Hunt";
                    if (gameModeText != "") gameModeText = " - " + Helpers.cs(Color.yellow, gameModeText);
                    var needEol = gameModeText != string.Empty || DevConfig.IsDingusRelease;
                    __instance.text.text =
                        $"<size=130%>{ColoredLogo}</size> v{BetterOtherRolesPlugin.Version.ToString()}{(BetterOtherRolesPlugin.betaNum > 0 ? "-beta" + BetterOtherRolesPlugin.betaNum : "")}\n{(DevConfig.IsDingusRelease ? $"<size=70%>{DingusRelease}</size>" : string.Empty)}{gameModeText}{(needEol ? EndOfLine : string.Empty)}" +
                        __instance.text.text;
                    position.DistanceFromEdge = new Vector3(2.25f, 0.11f, 0);
                }
                else
                {
                    string gameModeText = $"";
                    if (TORMapOptions.gameMode == CustomGamemodes.HideNSeek) gameModeText = $"Hide 'N Seek";
                    else if (TORMapOptions.gameMode == CustomGamemodes.Guesser) gameModeText = $"Guesser";
                    else if (TORMapOptions.gameMode == CustomGamemodes.PropHunt) gameModeText = $"Prop Hunt";
                    if (gameModeText != "") gameModeText = Helpers.cs(Color.yellow, gameModeText);

                    __instance.text.text = $"{fullCredentialsVersion}\n  {gameModeText + fullCredentials}\n<size=70%>{__instance.text.text}</size>";
                    position.DistanceFromEdge = new Vector3(0f, 0.1f, 0);

                    try
                    {
                        var GameModeText = GameObject.Find("GameModeText")?.GetComponent<TextMeshPro>();
                        GameModeText.text = gameModeText == ""
                            ? (GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.HideNSeek
                                ? "Van. HideNSeek"
                                : "Classic")
                            : gameModeText;
                        var ModeLabel = GameObject.Find("ModeLabel")?.GetComponentInChildren<TextMeshPro>();
                        ModeLabel.text = "Game Mode";
                    }
                    catch
                    {
                    }
                }
                position.AdjustPosition();
            }
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        public static class LogoPatch
        {
            public static SpriteRenderer renderer;
            public static Sprite bannerSprite;
            public static Sprite horseBannerSprite;
            public static Sprite banner2Sprite;
            private static PingTracker instance;

            static void Postfix(PingTracker __instance)
            {
                var torLogo = new GameObject("bannerLogo_TOR");
                torLogo.transform.SetParent(GameObject.Find("RightPanel").transform, false);
                torLogo.transform.localPosition = new Vector3(-0.4f, 0.5f, 5f);

                renderer = torLogo.AddComponent<SpriteRenderer>();
                loadSprites();
                renderer.sprite = Helpers.loadSpriteFromResources("BetterOtherRoles.Resources.Banner.png", 150f);

                instance = __instance;
                loadSprites();
                // renderer.sprite = TORMapOptions.enableHorseMode ? horseBannerSprite : bannerSprite;
                renderer.sprite = EventUtility.isEnabled ? banner2Sprite : bannerSprite;
                var credentialObject = new GameObject("credentialsTOR");
                var credentials = credentialObject.AddComponent<TextMeshPro>();
                credentials.SetText(
                    $"v{BetterOtherRolesPlugin.Version.ToString()}{(BetterOtherRolesPlugin.betaNum > 0 ? "-beta" + BetterOtherRolesPlugin.betaNum : "")}\n<size=30f%>\n</size>{mainMenuCredentials}\n");
                credentials.alignment = TMPro.TextAlignmentOptions.Center;
                credentials.fontSize *= 0.05f;

                credentials.transform.SetParent(torLogo.transform);
                credentials.transform.localPosition = Vector3.down * 1.2f;
            }

            public static void loadSprites()
            {
                if (bannerSprite == null)
                    bannerSprite = Helpers.loadSpriteFromResources("BetterOtherRoles.Resources.Banner.png", 150f);
                if (banner2Sprite == null)
                    banner2Sprite = Helpers.loadSpriteFromResources("BetterOtherRoles.Resources.Banner2.png", 300f);
                if (horseBannerSprite == null)
                    horseBannerSprite =
                        Helpers.loadSpriteFromResources("BetterOtherRoles.Resources.bannerTheHorseRoles.png", 300f);
            }

            public static void updateSprite()
            {
                loadSprites();
                if (renderer != null)
                {
                    float fadeDuration = 1f;
                    instance.StartCoroutine(Effects.Lerp(fadeDuration, new Action<float>((p) =>
                    {
                        renderer.color = new Color(1, 1, 1, 1 - p);
                        if (p == 1)
                        {
                            renderer.sprite = TORMapOptions.enableHorseMode ? horseBannerSprite : bannerSprite;
                            instance.StartCoroutine(Effects.Lerp(fadeDuration,
                                new Action<float>((p) => { renderer.color = new Color(1, 1, 1, p); })));
                        }
                    })));
                }
            }
        }
    }
}