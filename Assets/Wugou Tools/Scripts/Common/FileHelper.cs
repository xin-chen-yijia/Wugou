using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Wugou
{
    public static class FileHelper
    {
        public static async Task<string> ReadText(string filePath)
        {
            if (filePath.StartsWith("."))
            {
                filePath = Path.GetFullPath(filePath);
            }
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            await www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Logger.Error(www.error);
                return "";
            }
            else
            {
                // Show results as text
                return (www.downloadHandler.text);

                // Or retrieve results as binary data
                //byte[] results = www.downloadHandler.data;
            }

        }
    }
}
