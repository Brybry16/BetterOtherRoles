using System;
using System.Runtime.CompilerServices;
using BetterOtherRoles.Players;
using HarmonyLib;
using Hazel;
using InnerNet;
using JetBrains.Annotations;

namespace BetterOtherRoles.Modules;

public static class GameEvents
{
    

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleMessage))]
    public static class InnerNetClientHandleMessagePatch
    {
        [HarmonyPrefix]
        public static void Prefix([HarmonyArgument(0)] MessageReader reader)
        {
            // Right before running CoStartGame Coroutine
            if (reader.Tag == 2)
            {
                TriggerGameStarting();
            }
        }
    }
    
    public static event GameStartingHandler? OnGameStarting;
    public delegate void GameStartingHandler();
    public static void TriggerGameStarting() => OnGameStarting?.Invoke();
    
    public static event GameStartedHandler? OnGameStarted;

    public delegate void GameStartedHandler();

    public static void TriggerGameStarted() => OnGameStarted?.Invoke();


    public static event TaskCompletedHandler? OnTaskCompleted;

    public delegate void TaskCompletedHandler(PlayerControl player, PlayerTask task);

    public static void TriggerTaskCompleted(PlayerControl player, PlayerTask task) =>
        OnTaskCompleted?.Invoke(player, task);

    public static event ExitToMainMenuHandler? OnExitToMainMenu;
    public delegate void ExitToMainMenuHandler();
    public static void TriggerExitToMainMenu() => OnExitToMainMenu?.Invoke();
    
    public static event PlayerLeftHandler? OnPlayerLeft;

    public delegate void PlayerLeftHandler(int ownerId);

    public static void TriggerPlayerLeft(int ownerId) => OnPlayerLeft?.Invoke(ownerId);


    public static event GameEndedHandler? OnGameEnded;

    public delegate void GameEndedHandler();

    public static void TriggerEndGame() => OnGameEnded?.Invoke();


    public static event PlayAgainHandler? OnPlayAgain;
    public delegate void PlayAgainHandler();
    public static void TriggerPlayAgain() => OnPlayAgain?.Invoke();
    
    public static event MeetingStarted? OnMeetingStarted;

    public delegate void MeetingStarted();

    public static void TriggerMeetingStarted() => OnMeetingStarted?.Invoke();


    public static event MeetingEndedHandler? OnMeetingEnded;

    public delegate void MeetingEndedHandler(CachedPlayer? playerExiled);

    public static void TriggerMeetingEnded(CachedPlayer? playerExiled) => OnMeetingEnded?.Invoke(playerExiled);

    public static event VotingCompleted? OnVotingCompleted;
    public delegate void VotingCompleted();

    public static void TriggerVotingCompleted() => OnVotingCompleted?.Invoke();
}