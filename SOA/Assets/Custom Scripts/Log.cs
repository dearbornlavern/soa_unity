using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if (UNITY_STANDALONE)
using UnityEngine;
#endif

namespace soa
{
    class Log
    {

        public static void debug(String message)
        {
#if (UNITY_STANDALONE)
            Debug.Log(message);
#else
            Console.WriteLine(message);
#endif
        }

        public static void error(String message)
        {
#if (UNITY_STANDALONE)
            Debug.LogError(message);
#else
            Console.WriteLine("Error: " + message);
#endif
        }

        public static void warning(String message)
        {
#if (UNITY_STANDALONE)
            Debug.LogWarning(message);
#else
            Console.WriteLine("Warning: " + message);
#endif
        }

    }
}
