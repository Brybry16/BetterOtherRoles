  
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using Hazel;
using System;
using BetterOtherRoles.Players;
using BetterOtherRoles.Utilities;
using System.Linq;
using BetterOtherRoles.Eno;

namespace BetterOtherRoles.Patches {
    public class GameStartManagerPatch  {
        public static float timer = 600f;
        private static float kickingTimer = 0f;
        private static bool versionSent = false;
        private static string lobbyCodeText = "";

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
        public class AmongUsClientOnPlayerJoinedPatch {
            public static void Postfix(AmongUsClient __instance) {
                if (CachedPlayer.LocalPlayer != null) {
                    VersionHandshake.Instance.Share();
                }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        public class GameStartManagerStartPatch {
            public static void Postfix(GameStartManager __instance) {
                // Trigger version refresh
                versionSent = false;
                // Reset lobby countdown timer
                timer = 600f; 
                // Reset kicking timer
                kickingTimer = 0f;
                // Copy lobby code
                VersionHandshake.Clear();
                string code = InnerNet.GameCode.IntToGameName(AmongUsClient.Instance.GameId);
                GUIUtility.systemCopyBuffer = code;
                lobbyCodeText = FastDestroyableSingleton<TranslationController>.Instance.GetString(StringNames.RoomCode, new Il2CppReferenceArray<Il2CppSystem.Object>(0)) + "\r\n" + code;
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        public class GameStartManagerUpdatePatch {
            public static float startingTimer = 0;
            private static bool update = false;
            private static string currentText = "";
            private static GameObject copiedStartButton;
        
            public static void Prefix(GameStartManager __instance) {
                if (!GameData.Instance ) return; // No instance
                __instance.MinPlayers = 1;
                update = GameData.Instance.PlayerCount != __instance.LastPlayerCount;
        
                if (__instance.startState == GameStartManager.StartingStates.Countdown)
                {
                    if (AmongUsClient.Instance.AmHost)
                    {
                        __instance.startLabelText.text = "Stop";
                        var pos = __instance.GameStartText.transform.localPosition;
                        pos.y = 0.6f;
                        __instance.GameStartText.transform.localPosition = pos;
                        __instance.StartButton.gameObject.SetActive(true);
                    }
                }
                else if (__instance.startState == GameStartManager.StartingStates.NotStarting)
                {
                    __instance.startLabelText.text =
                        DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.StartLabel);
                }
            }

            public static void Postfix(GameStartManager __instance) {
                // Send version as soon as CachedPlayer.LocalPlayer.PlayerControl exists
                if (PlayerControl.LocalPlayer != null && !versionSent) {
                    versionSent = true;
                    VersionHandshake.Instance.Share();
                }

                if (AmongUsClient.Instance.AmHost)
                {
                    __instance.StartButton.color = __instance.startLabelText.color = ((__instance.LastPlayerCount >= __instance.MinPlayers) ? Palette.EnabledColor : Palette.DisabledClear);
                    __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition;
                    
                    if (__instance.startState != GameStartManager.StartingStates.Countdown)
                        UnityEngine.Object.Destroy(copiedStartButton);
                    
                    // Make starting info available to clients:
                    if (startingTimer <= 0 && __instance.startState == GameStartManager.StartingStates.Countdown) {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetGameStarting, Hazel.SendOption.Reliable, -1);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.setGameStarting();

                        // Activate Stop-Button
                        copiedStartButton = GameObject.Instantiate(__instance.StartButton.gameObject, __instance.StartButton.gameObject.transform.parent);
                        copiedStartButton.transform.localPosition = __instance.StartButton.transform.localPosition;
                        copiedStartButton.GetComponent<SpriteRenderer>().sprite = Helpers.loadSpriteFromResources("BetterOtherRoles.Resources.StopClean.png", 180f);
                        copiedStartButton.SetActive(true);
                        var startButtonText = copiedStartButton.GetComponentInChildren<TMPro.TextMeshPro>();
                        startButtonText.text = "STOP";
                        startButtonText.fontSize *= 0.8f;
                        startButtonText.fontSizeMax = startButtonText.fontSize;
                        startButtonText.gameObject.transform.localPosition = Vector3.zero;
                        PassiveButton startButtonPassiveButton = copiedStartButton.GetComponent<PassiveButton>();

                        void StopStartFunc() {
                            __instance.ResetStartState();
                            UnityEngine.Object.Destroy(copiedStartButton);
                            startingTimer = 0;
                        }
                        startButtonPassiveButton.OnClick.AddListener((Action)(() => StopStartFunc()));
                        __instance.StartCoroutine(Effects.Lerp(.1f, new System.Action<float>((p) => {
                            startButtonText.text = "STOP";
                        })));

                    }
                    if (__instance.startState == GameStartManager.StartingStates.Countdown)
                        __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition + Vector3.up * 0.6f;
                }
                else
                {
                    __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition;
                    if (!__instance.GameStartText.text.StartsWith("Starting"))
                        __instance.GameStartText.text = String.Empty;
                    
                    if (!__instance.GameStartText.text.StartsWith("Starting") || !CustomOptionHolder.anyPlayerCanStopStart.getBool())
                        UnityEngine.Object.Destroy(copiedStartButton);
                
                    if (CustomOptionHolder.anyPlayerCanStopStart.getBool() && copiedStartButton == null && __instance.GameStartText.text.StartsWith("Starting")) {
                        // Activate Stop-Button
                        copiedStartButton = GameObject.Instantiate(__instance.StartButton.gameObject, __instance.StartButton.gameObject.transform.parent);
                        copiedStartButton.transform.localPosition = __instance.StartButton.transform.localPosition;
                        copiedStartButton.GetComponent<SpriteRenderer>().sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.StopClean.png", 180f);
                        copiedStartButton.SetActive(true);
                        
                        var startButtonText = copiedStartButton.GetComponentInChildren<TMPro.TextMeshPro>();
                        startButtonText.text = "STOP";
                        startButtonText.fontSize *= 0.8f;
                        startButtonText.fontSizeMax = startButtonText.fontSize;
                        startButtonText.gameObject.transform.localPosition = Vector3.zero;
                        PassiveButton startButtonPassiveButton = copiedStartButton.GetComponent<PassiveButton>();

                        void StopStartFunc()
                        {
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                                CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.StopStart,
                                Hazel.SendOption.Reliable, AmongUsClient.Instance.HostId);
                            writer.Write(PlayerControl.LocalPlayer.PlayerId);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            UnityEngine.Object.Destroy(copiedStartButton);
                            __instance.GameStartText.text = String.Empty;
                            startingTimer = 0;
                        }
                        
                        startButtonPassiveButton.OnClick.AddListener((Action)(() => StopStartFunc()));
                        __instance.StartCoroutine(Effects.Lerp(.1f, new System.Action<float>((p) => {
                            startButtonText.text = "STOP";
                        })));
                    }
                    
                    if (__instance.GameStartText.text.StartsWith("Starting") && CustomOptionHolder.anyPlayerCanStopStart.getBool())
                        __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition + Vector3.up * 0.6f;
                }
                
                // Start Timer
                if (startingTimer > 0) {
                    startingTimer -= Time.deltaTime;
                }
                // Lobby timer
                if (!GameData.Instance) return; // No instance

                if (update) currentText = __instance.PlayerCounter.text;

                timer = Mathf.Max(0f, timer -= Time.deltaTime);
                int minutes = (int)timer / 60;
                int seconds = (int)timer % 60;
                string suffix = $" ({minutes:00}:{seconds:00})";

                __instance.PlayerCounter.text = currentText + suffix;
                __instance.PlayerCounter.autoSizeTextContainer = true;

                if (AmongUsClient.Instance.AmHost) {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ShareGamemode, Hazel.SendOption.Reliable, -1);
                    writer.Write((byte) TORMapOptions.gameMode);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.shareGamemode((byte) TORMapOptions.gameMode);
                }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
        public class GameStartManagerBeginGame {
            public static bool Prefix(GameStartManager __instance) {
                // Block game start if not everyone has the same mod version
                bool continueStart = true;

                if (__instance.startState != GameStartManager.StartingStates.NotStarting)
                {
                    __instance.ResetStartState();
                    return false;
                }

                if (AmongUsClient.Instance.AmHost) {
                    foreach (InnerNet.ClientData client in AmongUsClient.Instance.allClients.GetFastEnumerator()) {
                        if (client.Character == null) continue;
                        var dummyComponent = client.Character.GetComponent<DummyBehaviour>();
                        if (dummyComponent != null && dummyComponent.enabled)
                            continue;
                        
                        if (!VersionHandshake.Has(client.Id)) {
                            continueStart = false;
                            break;
                        }
                        
                        var PV = VersionHandshake.Find(client.Id);
                        int diff = BetterOtherRolesPlugin.Version.CompareTo(PV.Version);
                        if (diff != 0 || !PV.GuidMatch()) {
                            continueStart = false;
                            break;
                        }
                    }
                    if (continueStart && (TORMapOptions.gameMode == CustomGamemodes.HideNSeek || TORMapOptions.gameMode == CustomGamemodes.PropHunt) && GameOptionsManager.Instance.CurrentGameOptions.MapId != 6) {
                        byte mapId = 0;
                        if (TORMapOptions.gameMode == CustomGamemodes.HideNSeek) mapId = (byte)CustomOptionHolder.hideNSeekMap.getSelection();
                        else if (TORMapOptions.gameMode == CustomGamemodes.PropHunt) mapId = (byte)CustomOptionHolder.propHuntMap.getSelection();
                        if (mapId >= 3) mapId++;
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.DynamicMapOption, Hazel.SendOption.Reliable, -1);
                        writer.Write(mapId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.dynamicMapOption(mapId);
                    }            
                    else if (CustomOptionHolder.dynamicMap.getBool() && continueStart) {
                        // 0 = Skeld
                        // 1 = Mira HQ
                        // 2 = Polus
                        // 3 = Dleks - deactivated
                        // 4 = Airship
                        // 5 = Submerged
                        byte chosenMapId = 0;
                        List<float> probabilities = new List<float>();
                        probabilities.Add(CustomOptionHolder.dynamicMapEnableSkeld.getFloat() / 100f);
                        probabilities.Add(CustomOptionHolder.dynamicMapEnableMira.getFloat() / 100f);
                        probabilities.Add(CustomOptionHolder.dynamicMapEnablePolus.getFloat() / 100f);
                        probabilities.Add(CustomOptionHolder.dynamicMapEnableAirShip.getFloat() / 100f);
                        probabilities.Add(CustomOptionHolder.dynamicMapEnableFungle.getFloat() / 100f);
                        probabilities.Add(CustomOptionHolder.dynamicMapEnableSubmerged.getFloat() / 100f);

                        // if any map is at 100%, remove all maps that are not!
                        if (probabilities.Contains(1.0f)) {
                            for (int i=0; i < probabilities.Count; i++) {
                                if (probabilities[i] != 1.0) probabilities[i] = 0;
                            }
                        }

                        float sum = probabilities.Sum();
                        if (sum == 0) return continueStart;  // All maps set to 0, why are you doing this???
                        for (int i = 0; i < probabilities.Count; i++) {  // Normalize to [0,1]
                            probabilities[i] /= sum;
                        }
                        float selection = (float)BetterOtherRoles.Rnd.NextDouble();
                        float cumsum = 0;
                        for (byte i = 0; i < probabilities.Count; i++) {
                            cumsum += probabilities[i];
                            if (cumsum > selection) {
                                chosenMapId = i;
                                break;
                            }
                        }

                        // Translate chosen map to presets page and use that maps random map preset page
                        if (CustomOptionHolder.dynamicMapSeparateSettings.getBool()) {
                            CustomOptionHolder.presetSelection.updateSelection(chosenMapId + 2);
                        }
                        if (chosenMapId >= 3) chosenMapId++;  // Skip dlekS
                                                              
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.DynamicMapOption, Hazel.SendOption.Reliable, -1);
                        writer.Write(chosenMapId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.dynamicMapOption(chosenMapId);
                    }
                }
                
                return continueStart;
            }
        }
        
        [HarmonyPatch(typeof(GameStartManager))]
        public class GameStartManagerPatches
        {
            [HarmonyPatch(nameof(GameStartManager.ReallyBegin))]
            [HarmonyPostfix]
            private static void ReallyBeginPostfix(GameStartManager __instance)
            {
                __instance.StartButton.gameObject.SetActive(true);
            }
        }
    }
}
