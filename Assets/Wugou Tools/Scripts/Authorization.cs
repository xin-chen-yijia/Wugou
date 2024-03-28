using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Wugou
{
    public class Authorization
    {
        public class User
        {
            public string name;
            public string token;
        }

        public static User activeUser { get; private set; }

        private static async Task<bool> Post(string url, WWWForm data, System.Action<JToken> onSuccess, System.Action<string> onError = null)
        {
            Logger.Info($"post {url}");
            using (UnityWebRequest request = UnityWebRequest.Post(url, data))
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Logger.Info(request.downloadHandler.text);
                    JObject resObj = JObject.Parse(request.downloadHandler.text);
                    if (resObj["error"].Value<int>() == 0)
                    {
                        onSuccess?.Invoke(resObj["data"]);
                        return true;
                    }

                    onError?.Invoke(resObj["message"].Value<string>());
                }

                Logger.Error(request.error);
                onError?.Invoke(request.error);
                return true;
            }

        }

        /// <summary>
        /// ÓÃ»§ÃûÃÜÂëµÇÂ¼
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<bool> Login(string username, string password)
        {
            WWWForm form = new WWWForm();
            form.AddField("username", username);
            form.AddField("password", password);

            Debug.Log($"{Gameplay.settings.host}/api/signIn");
            using (UnityWebRequest webRequest = UnityWebRequest.Post($"{Gameplay.settings.host}/api/signIn", form))
            {
                // Request and wait for the desired page.
                await webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("Login Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("Login with HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        string data = (webRequest.downloadHandler.text);
                        if(!string.IsNullOrEmpty(data))
                        {
                            var jo = JObject.Parse(webRequest.downloadHandler.text);
                            if (jo["data"] == null || jo["data"]["token"] == null)
                            {
                                return false;
                            }

                            activeUser = new User()
                            {
                                name = username,
                                token = jo["data"]["token"].ToString(),
                            };
                            PlayerPrefs.SetString(Gameplay.kPREFS_KEY_AUTH, JsonConvert.SerializeObject(activeUser));

                            return true;
                        }

                        return false;
                    default: break;

                }
            }

            return false;

        }

        public static void Logout()
        {
            PlayerPrefs.DeleteKey(Gameplay.kPREFS_KEY_AUTH);
            activeUser = null;
        }

        public static bool LoadAuthCache()
        {
            var authInfo = PlayerPrefs.GetString(Gameplay.kPREFS_KEY_AUTH);
            if (string.IsNullOrEmpty(authInfo))
            {
                return false;
            }

            activeUser = JsonConvert.DeserializeObject<User>(authInfo);
            return true;
        }
    }
}

