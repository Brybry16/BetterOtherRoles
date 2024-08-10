using BetterOtherRoles.Modules;
using HarmonyLib;

namespace BetterOtherRoles.Patches;

public class EndGameNavigationPatch
{
    [HarmonyPatch(typeof(EndGameNavigation), nameof(EndGameNavigation.NextGame))]
    class EndGameNavigationNextGamePatch {
        
        [HarmonyPostfix]
        public static void Postfix(EndGameManager __instance)
        {
            GameEvents.TriggerPlayAgain();
        }
    }
    
    [HarmonyPatch(typeof(EndGameNavigation), nameof(EndGameNavigation.Exit))]
    class EndGameNavigationExitPatch {
        
        [HarmonyPostfix]
        public static void Postfix(EndGameManager __instance)
        {
            GameEvents.TriggerExitToMainMenu();
        }
    }
}