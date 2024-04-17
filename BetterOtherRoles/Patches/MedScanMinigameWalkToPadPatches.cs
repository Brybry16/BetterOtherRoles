﻿using System.Collections;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils;
using BetterOtherRoles.Modules;
using HarmonyLib;
using UnityEngine;

namespace BetterOtherRoles.Patches;

[HarmonyPatch(typeof(MedScanMinigame._WalkToPad_d__16))]
public static class MedScanMinigameWalkToPadPatches
{
    private static CustomOption RandomizeScanPlayerPosition => CustomOptionHolder.RandomizePositionDuringScan;
    
    [HarmonyPatch(nameof(MedScanMinigame._WalkToPad_d__16.MoveNext))]
    [HarmonyPrefix]
    private static bool MoveNextPrefix(MedScanMinigame._WalkToPad_d__16 __instance)
    {
        if (!RandomizeScanPlayerPosition.getBool() || !(Helpers.isPolus() || Helpers.isMira() || Helpers.isSkeld())) return true;
        var minigame = __instance.__4__this;
        minigame.StartCoroutine(WalkToPadEnumerator(minigame));
        
        return false;
    }
    
    private static IEnumerator WalkToPadEnumerator(MedScanMinigame minigame)
    {
        GameObject panel = null;
        if (Helpers.isPolus() || (ShipStatus.Instance && ShipStatus.Instance.Type == ShipStatus.MapType.Pb))
        {
            panel = Object.FindObjectsOfType<GameObject>()
                .FirstOrDefault(o => o.name == "panel_medplatform");
        }
        else if (Helpers.isSkeld() || Helpers.isMira())
        {
            panel = Object.FindObjectsOfType<GameObject>()
                .FirstOrDefault(o => o.name == "MedScanner");
        }

        if (panel == null || Camera.main == null) yield break;
                
        var panelSize = panel.GetComponent<SpriteRenderer>().bounds.size * 0.3f;
            
        minigame.state = MedScanMinigame.PositionState.WalkingToPad;
        var myPhysics = PlayerControl.LocalPlayer.MyPhysics;

        Vector2 worldPos = ShipStatus.Instance.MedScanner.Position;
        var xRange = UnityEngine.Random.Range(-panelSize.x, panelSize.x);
        var yRange = UnityEngine.Random.Range(-panelSize.y, 0f);
        worldPos += new Vector2(xRange, yRange);

        Camera.main.GetComponent<FollowerCamera>().Locked = false;
        yield return myPhysics.WalkPlayerTo(worldPos, 0.001f, 1f);
        yield return new WaitForSeconds(0.1f);
        Camera.main.GetComponent<FollowerCamera>().Locked = true;
        minigame.walking = null;
    }
}