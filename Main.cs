using Kitchen;
using KitchenInGameChat.MessageWindow;
using KitchenLib;
using KitchenLib.Event;
using System.Reflection;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenInGameChat
{
    public class Main : BaseMod
    {
        // GUID must be unique and is recommended to be in reverse domain name notation
        // Mod Name is displayed to the player and listed in the mods menu
        // Mod Version must follow semver notation e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.InGameChat";
        public const string MOD_NAME = "In-Game Chat";
        public const string MOD_VERSION = "0.1.0";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.5";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.3" current and all future
        // e.g. ">=1.1.3 <=1.2.3" for all from/until

        public const string CHAT_WINDOW_NAME = "Chat";
        public static ViewType? ChatPrefViewType;
        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnInitialise()
        {
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
            ChatPreferenceView.CreatePreferences(MOD_GUID, MOD_NAME);

            Events.BuildGameDataPostViewInitEvent += delegate (object _, BuildGameDataEventArgs args)
            {
                Main.LogInfo("BuildGameDataPostViewInitEvent");
                PreferenceViewRegistry.TryRegister<ChatPreferenceView>(out ViewType prefViewType);
                ChatPrefViewType = prefViewType;
            };

        }

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
