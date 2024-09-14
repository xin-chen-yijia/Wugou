using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Wugou.Verify
{
    public class RSADongle
    {
        public static bool Verify(string dlfFile, string rsaPrivateKeyFile)
        {
            if(!File.Exists(dlfFile) || !File.Exists(rsaPrivateKeyFile))
            {
                Logger.Error($"{dlfFile} or {rsaPrivateKeyFile} not exists...");
                return false;
            }

            string decryptStr = (RSA.Decrypt(File.ReadAllText(dlfFile), rsaPrivateKeyFile));
            JObject jo = JObject.Parse(decryptStr);

            string macAddr = jo["mac"].ToString();
            if(!Utils.GetAllMacAddress().Contains(macAddr))
            {
                return false;
            }

            // ÓÐÐ§ÆÚ
            DateTime validity = jo["validity"].ToObject<DateTime>();
            if (DateTime.Compare(validity,DateTime.Now) < 1)
            {
                return false;
            }

            return true;
        }
    }

}
