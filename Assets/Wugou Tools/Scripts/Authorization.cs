using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Wugou
{
    public class Authorization
    {
        public enum Role
        {
            kRoleAdmin=0,
            kRolePlayer,
        }
        public class User
        {
            public string name;
            public Role role;
        }

        private static async Task<bool> Post(string url, WWWForm data, System.Action<JToken> onSuccess, System.Action<string> onError = null)
        {
            Logger.Info($"post {url}");
            UnityWebRequest request = UnityWebRequest.Post(url, data);
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

        public static User Find(string username, string password)
        {
            return new User() { name = username, role = Role.kRoleAdmin };
        //string sql = string.Format("SELECT person.name,person.key FROM gun_train.person WHERE person.phone='{0}' ", user);
        //string result = DataBase.CheckPerson(sql, key);
            //if (username == "001" && password == "001")
            //{
            //    return new User() { name = username, role = Role.kRoleAdmin };
            //}
            //else if (username == password)
            //{
            //    return new User() { name = username, role = Role.kRolePlayer };
            //}

            //return null;
        }
    }
}

