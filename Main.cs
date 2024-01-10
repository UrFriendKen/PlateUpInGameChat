using HarmonyLib;
using Kitchen;
using KitchenInGameChat.MessageWindow;
using KitchenMods;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenInGameChat
{
    public class Main : IModInitializer
    {
        public const string MOD_GUID = "IcedMilo.PlateUp.InGameChat";
        public const string MOD_NAME = "In-Game Chat";
        public const string MOD_VERSION = "0.1.4";

        public const string CHAT_WINDOW_NAME = "Chat";
        public static ViewType? ChatPrefViewType;

        Harmony harmony;
        static List<Assembly> PatchedAssemblies = new List<Assembly>();

        public Main()
        {
            if (harmony == null)
                harmony = new Harmony(MOD_GUID);
            Assembly assembly = Assembly.GetExecutingAssembly();
            if (assembly != null && !PatchedAssemblies.Contains(assembly))
            {
                harmony.PatchAll(assembly);
                PatchedAssemblies.Add(assembly);
            }
        }

        public void PostActivate(KitchenMods.Mod mod)
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
            ChatPreferenceView.CreatePreferences(MOD_GUID, MOD_NAME);
        }

        public void PreInject()
        {
            PreferenceViewRegistry.TryRegister<ChatPreferenceView>(out ViewType prefViewType);
            ChatPrefViewType = prefViewType;
        }

        public void PostInject() { }

        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
