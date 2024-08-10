using HarmonyLib;
using System;
using AmongUs.GameOptions;
using BetterOtherRoles.Modules;
using BetterOtherRoles.Utilities;
using UnityEngine;

namespace BetterOtherRoles {
    [HarmonyPatch]
    public static class TasksHandler {

        public static Tuple<int, int> taskInfo(GameData.PlayerInfo playerInfo) {
            int TotalTasks = 0;
            int CompletedTasks = 0;
            if (!playerInfo.Disconnected && playerInfo.Tasks != null &&
                playerInfo.Object &&
                playerInfo.Role && playerInfo.Role.TasksCountTowardProgress &&
                !playerInfo.Object.hasFakeTasks() && !playerInfo.Role.IsImpostor
                ) {
                foreach (var playerInfoTask in playerInfo.Tasks.GetFastEnumerator())
                {
                    if (playerInfoTask.Complete) CompletedTasks++;
                    TotalTasks++;
                }
            }
            return Tuple.Create(CompletedTasks, TotalTasks);
        }

        public static Tuple<int, int> prankexTaskInfo(GameData.PlayerInfo playerInfo, int PlayersLeft)
        {
            if (!UnknownImpostors.IsBattleRoyale || PlayersLeft <= 0)
            {
                return taskInfo(playerInfo);
            }

            int TotalTasks = 0;
            int CompletedTasks = 0;
            if (!playerInfo.Disconnected && playerInfo.Tasks != null &&
                playerInfo.Object &&
                playerInfo.Role)
            {
                TotalTasks = playerInfo.Tasks.Count;
                // 1 task par joueur toutes les 23 secondes environ (la moyenne est de 23.63 secondes par task)
                CompletedTasks += Mathf.Clamp((int) Math.Floor(UnknownImpostors.getElapsedTime().TotalSeconds/23.0), 0, TotalTasks);
            }

            return Tuple.Create(CompletedTasks, TotalTasks);
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
        private static class GameDataRecomputeTaskCountsPatch {
            private static bool Prefix(GameData __instance) {
                

                var totalTasks = 0;
                var completedTasks = 0;
                var nbPlayersLeft = GameData.Instance.PlayerCount -
                                    GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
                
                foreach (var playerInfo in GameData.Instance.AllPlayers.GetFastEnumerator())
                {
                    if (playerInfo.Object
                        && playerInfo.Object.hasAliveKillingLover() // Tasks do not count if a Crewmate has an alive killing Lover
                        || playerInfo.PlayerId == Lawyer.lawyer?.PlayerId // Tasks of the Lawyer do not count
                        || (playerInfo.PlayerId == Pursuer.pursuer?.PlayerId && Pursuer.pursuer.Data.IsDead) // Tasks of the Pursuer only count, if he's alive
                        || playerInfo.PlayerId == Thief.thief?.PlayerId // Thief's tasks only count after joining crew team as sheriff (and then the thief is not the thief anymore)
                       )
                        continue;
                    // PRANKEX :)
                    var (playerCompleted, playerTotal) = prankexTaskInfo(playerInfo, nbPlayersLeft--);
                    totalTasks += playerTotal;
                    completedTasks += playerCompleted;
                }
                
                // PRANKEX :)
                if (completedTasks == totalTasks && UnknownImpostors.IsBattleRoyale)
                {
                    completedTasks = totalTasks - 1;
                }
                
                __instance.TotalTasks = totalTasks;
                __instance.CompletedTasks = completedTasks;
                return false;
            }
        }
        
    }
}
