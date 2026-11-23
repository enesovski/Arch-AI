using UnityEngine;

namespace Archura
{
    internal static class ArchuraLog
    {
        public static void Info(string message, Object context = null) => Debug.Log(message, context);
        public static void Warning(string message, Object context = null) => Debug.LogWarning(message, context);
        public static void Error(string message, Object context = null) => Debug.LogError(message, context);
    }

    internal static class UnityNull
    {
        /// <summary>
        /// Null check for interface references that may point at a destroyed
        /// <see cref="UnityEngine.Object"/>, where plain <c>== null</c> is not enough.
        /// </summary>
        public static bool IsNull(object value) => value is Object unityObject ? unityObject == null : value == null;
    }
}
