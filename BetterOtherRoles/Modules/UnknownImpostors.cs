using BetterOtherRoles.Players;
using BetterOtherRoles.Utilities.Attributes;
using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Diagnostics;
using UnityEngine;

namespace BetterOtherRoles.Modules;

[Autoload]
public static class UnknownImpostors
{
    private static readonly Stopwatch timer = new();

    public static bool IsEnabled => CustomOptionHolder.ImpostorsDontKnowTeammate.getBool() || IsBattleRoyale;
    public static bool CanImpostorsKillTeammate => CustomOptionHolder.ImpostorsCanKillTeammate.getBool() || IsBattleRoyale;
    public static bool IsBattleRoyale => CustomOptionHolder.ImpostorsBattleRoyale.getBool();
    public static bool IsOtherImpostorUnknown => IsEnabled && CachedPlayer.LocalPlayer.Data.Role.IsImpostor;

    public static bool IsHostDingusRelease = AmongUsClient.Instance && AmongUsClient.Instance.AmHost && DevConfig.
        IsDingusRelease;
    
    public static NetworkedPlayerInfo lastExiled;
    
    // public static DateTime prankexStartTime = new(2024, 8, 6, 20, 20, 0, DateTimeKind.Utc);
    // public static DateTime prankexEndTime = new(2024, 8, 7, 5, 0, 0, DateTimeKind.Utc);
    // public static bool IsBattleRoyale => IsHostDingusRelease &&
    //                                      DateTime.Compare(DateTime.Now, prankexStartTime) >= 0 &&
    //                                      DateTime.Compare(DateTime.Now, prankexEndTime) <= 0 &&
    //                                      IsEnabled && CanImpostorsKillTeammate &&
    //                                      !CustomOptionHolder.ImpostorsBattleRoyale.getBool();
    
    static UnknownImpostors()
    {
        GameEvents.OnGameStarted += () =>
        {
            lastExiled = null;
            timer.Start();
        };
        GameEvents.OnMeetingStarted += () => timer.Stop();
        GameEvents.OnMeetingEnded += _ => timer.Start();
        GameEvents.OnGameEnded += () =>
        {
            timer.Stop();
            timer.Reset();
        };
    }
    
    public static TimeSpan getElapsedTime()
    {
        return timer.Elapsed;
    }
}