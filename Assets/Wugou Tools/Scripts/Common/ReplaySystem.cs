using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// ��¼�ط�ϵͳ
    /// </summary>
    public class ReplaySystem 
    {
        private static string replaySavePath_ = Path.Combine(Application.persistentDataPath,"replay");

        /// <summary>
        /// ��ʼ
        /// </summary>
        /// <returns></returns>
        public static bool Start()
        {
            return true;
        }

        public static void Stop()
        {

        }

        public static void SetReplaySavePath(string path)
        {
            replaySavePath_ = path;
        }

    }
}

