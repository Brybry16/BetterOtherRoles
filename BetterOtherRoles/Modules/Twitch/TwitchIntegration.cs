using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BetterOtherRoles.Utilities.Attributes;
using HarmonyLib;
using Innersloth.IO;
using Twitch;
using UnityEngine;

namespace BetterOtherRoles.Modules.Twitch;

//[Autoload]
public static class TwitchIntegration
{
    private static readonly TwitchAuth auth;
    private static readonly HttpClient client;
    
    private const int tokenFetchTimeoutSec = 90;

    private static readonly Action SetStartMarker = () => createMarker(MarkerType.GameStart);
    private static readonly Action SetEndMarker = () => createMarker(MarkerType.GameEnd);
    //TODO: Marqueur quand déconnexion + voir si possible de fusionner les GameEnd ensemble

    static TwitchIntegration()
    {
        client = new HttpClient();
        auth = new TwitchAuth();
        Init();
    }

    private static void Init(bool resetAuth = false)
    {
        if (resetAuth)
        {
            auth.Reset();
        }
        
        if (!(auth.IsAuthenticated || auth.VerifyToken().GetAwaiter().GetResult()))
        {
            //TODO: Afficher une popup pour lancer l'authentification
            BetterOtherRolesPlugin.Logger.LogError("Failed to get Twitch User ID, please reauthenticate your Twitch account in the game settings.");
            return;
        }
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TwitchManager.Instance.Token);
        client.DefaultRequestHeaders.Add("Client-Id", TwitchManager.ClientId);
        
        GameEvents.OnGameStarting += new GameEvents.GameStartingHandler(SetStartMarker);
        GameEvents.OnPlayAgain += new GameEvents.PlayAgainHandler(SetEndMarker);
        GameEvents.OnExitToMainMenu += new GameEvents.ExitToMainMenuHandler(SetEndMarker);
        
        BetterOtherRolesPlugin.Logger.LogMessage("Twitch Integration loaded");
    }

    private static void Unload()
    {
        GameEvents.OnGameStarting -= new GameEvents.GameStartingHandler(SetStartMarker);
        GameEvents.OnPlayAgain -= new GameEvents.PlayAgainHandler(SetEndMarker);
        GameEvents.OnExitToMainMenu -= new GameEvents.ExitToMainMenuHandler(SetEndMarker);
    }

    [HarmonyPatch(typeof(TwitchManager), nameof(TwitchManager.FetchNewToken))]
    public static class FetchNewTokenPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(TwitchManager __instance)
        {
            __instance.Token = null;
            System.Random random = new System.Random();
            __instance.verify = random.Next(0, int.MaxValue).ToString() + random.Next(0, int.MaxValue).ToString();
            FileIO.WriteAllText(Path.Combine(PlatformPaths.persistentDataPath, "twitch_verify"), __instance.verify);
            
            string tokenCache = Path.Combine(PlatformPaths.persistentDataPath, "twitch");
            FileIO.Delete(tokenCache);
            
            Application.OpenURL($"https://id.twitch.tv/oauth2/authorize?client_id={TwitchManager.ClientId}&redirect_uri={TwitchManager.RedirectUri}&response_type=token&scope=" + string.Join(" ", TwitchManager.Scopes) + "&state=" + __instance.verify);
            
            for (var i = 0; !FileIO.Exists(tokenCache) || __instance.Token != null; i++)
            {
                Task.Delay(100).GetAwaiter().GetResult();

                // ~1 min before the request expires
                if (i <= tokenFetchTimeoutSec*10) continue;
                BetterOtherRolesPlugin.Logger.LogError("Request to get Twitch Token expired");
                return false;
            }

            if (__instance.Token == null)
                __instance.Token = FileIO.ReadAllText(tokenCache);
            try
            {
                FileIO.WriteAllText(tokenCache, __instance.Token);
            }
            catch (Exception ex)
            {
                BetterOtherRolesPlugin.Logger.LogError(ex);
            }

            Init(resetAuth: true);
            return false;
        }
    }

    [HarmonyPatch(typeof(TwitchManager), nameof(TwitchManager.LaunchImplicitAuthAsync))]
    public static class LaunchImplicitAuthAsyncPatch
    {
        private static bool isAuthBefore;

        [HarmonyPrefix]
        public static void Prefix()
        {
            if (auth.IsAuthenticated)
            {
                isAuthBefore = true;
            }
        }
        
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!isAuthBefore) return;
            
            BetterOtherRolesPlugin.Logger.LogMessage("Refreshing Twitch Token");
            Unload();
            TwitchManager.Instance.FetchNewToken();
        }
    }

    private enum MarkerType
    {
        GameStart,
        GameEnd
    }

    private static void createMarker(MarkerType type)
    {
        if (!auth.Scopes.Contains(TwitchScope.ChannelManageBroadcast))
        {
            BetterOtherRolesPlugin.Logger.LogError($"Twitch: scopes don't contain {TwitchScope.ChannelManageBroadcast.GetDescription()}, can't create marker");
            return;
        }
        
        string descr;
        switch (type)
        {
            case MarkerType.GameStart:
                descr = "Game Start";
                break;
            case MarkerType.GameEnd:
                descr = "Game End";
                break;
            default:
                return;
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.twitch.tv/helix/streams/markers");
        requestMessage.Content = new StringContent("{\"user_id\":" + int.Parse(auth.userId) + ",\"description\":\"" + descr + "\"}");
        requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        client.SendAsync(requestMessage).ContinueWith(responseTask =>
        {
            if (responseTask.Result.IsSuccessStatusCode)
            {
                BetterOtherRolesPlugin.Logger.LogMessage("Successfully created Marker, response: " +
                                                         responseTask.Result.Content.ReadAsStringAsync().Result);
            }
            else
            {
                BetterOtherRolesPlugin.Logger.LogMessage("Failed to create marker, response: " +
                                                         responseTask.Result.Content.ReadAsStringAsync().Result);
            }
        });
    }
}