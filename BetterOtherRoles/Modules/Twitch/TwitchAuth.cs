using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonyLib;
using Innersloth.IO;
using Twitch;

namespace BetterOtherRoles.Modules.Twitch;

public class TwitchAuth
{
    private string Token { get; set; }
    public string userId { get; private set; }
    public TwitchScope[] Scopes { get; private set; }
    public bool IsAuthenticated => Token != null && userId != null;

    public TwitchAuth()
    {
        // Convert enum to string scopes
        TwitchManager.Scopes = Enum.GetValues<TwitchScope>()
                                                  .Where(x => x != TwitchScope.None)
                                                  .Select(x => x.GetDescription())
                                                  .ToArray();
    }


    public void Reset()
    {
        Token = null;
        userId = null;
    }
    public async Task<bool> VerifyToken()
    {
        try
        {
            var path = Path.Combine(PlatformPaths.persistentDataPath, "twitch");
            if (!FileIO.Exists(path)) return false;
            Token = FileIO.ReadAllText(path);

            using HttpClient http = new();
            
            using HttpRequestMessage msg = new();
            msg.Method = HttpMethod.Get;
            msg.RequestUri = new Uri("https://id.twitch.tv/oauth2/validate");
            msg.Headers.TryAddWithoutValidation("Authorization", "Bearer " + Token);

            using HttpResponseMessage res = await http.SendAsync(msg);
            
            try
            {
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    BetterOtherRolesPlugin.Logger.LogMessage(res.Content.ReadAsStringAsync().Result);
                    var authData = JsonSerializer.Deserialize<VerifyObject>(res.Content.ReadAsStringAsync().Result);
                    userId = authData.user_id;
                    Scopes = authData.scopes
                        .Select(
                            x =>
                            {
                                Enum.TryParse(string.Join("",
                                    x.Split(":")
                                        .Select(word => word[0].ToString().ToUpper() + word.Substring(1))
                                ), out TwitchScope scope);
                                return scope;
                            }
                        ).ToArray();
                }
                else if (res.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // TODO: Else au cas où le token est expiré.
                    // Dans ce cas afficher popup avec un bouton qui simule bouton Twitch pour regénérer un token.
                }
                
                return res.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                BetterOtherRolesPlugin.Logger.LogError(e);
            }
        }
        catch (Exception e)
        {
            BetterOtherRolesPlugin.Logger.LogError(e);
        }

        return false;
    }

    public class VerifyObject
    {
        public string client_id { get; set; }
        public string login { get; set; }
        public string[] scopes { get; set; }
        public string user_id { get; set; }
        public int expires_in { get; set; }
    }
}