using System.Collections.Generic;
using UnityEngine;

namespace Cubevi_Swizzle
{
    public static class SwizzleLog
    {
        private static readonly Dictionary<string, string> LevelColors = new()
    {
        { "Info", "grey" },
        { "Warning", "orange" },
        { "Error", "magenta" },
        { "Important", "cyan"}
        //List of available colours at: https://docs.unity3d.com/Manual/StyledText.html
    };

    private static void LogMessage(string message, string level)
    {
        if (LevelColors.TryGetValue(level, out string color))
        {
            switch (level)
            {
                case "Info":
                    Debug.Log($"<color={color}>[Cubevi_Swizzle][{level}]: {message}</color>");
                    break;
                case "Warning":
                    Debug.LogWarning($"<color={color}>[Cubevi_Swizzle][{level}]: {message}</color>");
                    break;
                case "Error":
                    Debug.LogError($"<color={color}>[Cubevi_Swizzle][{level}]: {message}</color>");
                    break;
                case "Important":
                    Debug.Log($"<color={color}>[Cubevi_Swizzle][{level}]: {message}</color>");
                    break;
            }
        }
        else
        {
            Debug.LogError("[Cubevi_Swizzle] Log level not accessible");
        }
    }

    public static void LogInfo(string message)
    {
        LogMessage(message, "Info");
    }

    public static void LogWarning(string message)
    {
        LogMessage(message, "Warning");
    }

    public static void LogError(string message)
    {
        LogMessage(message, "Error");
    }

    public static void LogImportant(string message)
    {
        LogMessage(message, "Important");
    }
}}
