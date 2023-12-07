using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public enum LogLevel
    {
        kInfo=0,
        kWarning,
        kError
    }
    public class Logger 
    {
        public static LogLevel level = LogLevel.kInfo;
#if UNITY_EDITOR
        public static void Info(object message)
        {
            if(level <= LogLevel.kInfo)
            {
                Debug.Log(message);
            }
        }

        public static void Warning(object message)
        {
            if(level <= LogLevel.kWarning)
            {
                Debug.LogWarning(message);
            }
        }

        public static void Error(object message)
        {
            if(level <= LogLevel.kError)
            {
                Debug.LogError(message);
            }
        }

        public static void DebugInfo(object message)
        {
#if UNITY_EDITOR
            Info($"[Debug] {message}");
#endif
        }
#else
        public static void Info(object message)
        {
        }

        public static void Warning(object message)
        {
        }

        public static void Error(object message)
        {
        }

        public static void DebugInfo(object message)
        {
        }
#endif
    }

}