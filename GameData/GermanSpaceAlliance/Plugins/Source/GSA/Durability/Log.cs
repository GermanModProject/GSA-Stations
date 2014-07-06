using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GSA.Durability
{
    static class Debug
    {
        public static bool debug = false;

        public static void Log(object message)
        {
            if (debug)
                UnityEngine.Debug.Log(message);
        }
        public static void Log(object message, UnityEngine.Object context)
        {
            if (debug)
                UnityEngine.Debug.Log(message, context);
        }

        public static void LogError(object message)
        {
            if (debug)
                UnityEngine.Debug.LogError(message);
        }
        public static void LogError(object message, UnityEngine.Object context)
        {
            if (debug)
                UnityEngine.Debug.LogError(message, context);
        }

        public static void LogWarning(object message)
        {
            if (debug)
                UnityEngine.Debug.LogWarning(message);
        }
        public static void LogWarning(object message, UnityEngine.Object context)
        {
            if (debug)
                UnityEngine.Debug.LogWarning(message, context);
        }

        public static void LogException(Exception exception)
        {
            if (debug)
                UnityEngine.Debug.LogException(exception);
        }
        public static void LogException(Exception exception, UnityEngine.Object context)
        {
            if (debug)
                UnityEngine.Debug.LogException(exception, context);
        }
    }
}
