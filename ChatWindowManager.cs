using PreferenceSystem;
using System.Collections.Generic;
using UnityEngine;

namespace KitchenInGameChat
{
    internal sealed class ChatWindowManager : MonoBehaviour
    {
        private int WindowID;

        private MessageWindowView View;

        private static PreferenceSystemManager PrefManager;

        public List<Color> ColorList = new List<Color>()
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.white,
            Color.black,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            Color.gray
        };

        public const string CHAT_FONT_SIZE_ID = "chatFontSize";
        public const string CHAT_BACKGROUND_COLOR_ID = "chatBackgroundColor";
        public const string CHAT_NON_PLAYER_COLOR_ID = "chatNonPlayerColor";
        public const string CHAT_SELF_PLAYER_COLOR_ID = "chatSelfPlayerColor";
        public const string CHAT_OTHER_PLAYER_COLOR_ID = "chatOtherPlayerColor";
        public const string CHAT_ENABLED_ID = "chatHide";
        public const string CHAT_DO_FADE_ID = "chatDoFade";
        public const string CHAT_ACTIVE_OPACITY = "chatActiveOpacity";
        public const string CHAT_INACTIVE_OPACITY = "chatInactiveOpacity";
        public const string CHAT_WINDOW_WIDTH_PERCENT_ID = "chatWindowWidthPercent";
        public const string CHAT_WINDOW_HEIGHT_PERCENT_ID = "chatWindowHeightPercent";

        void Update()
        {
            if (!View && !TryGetMessageWindowView(WindowID, out View))
            {
                return;
            }
            View.DoNotDrawOverride = !PrefManager.Get<bool>(CHAT_ENABLED_ID);
            View.BackgroundColor = ColorList[PrefManager.Get<int>(CHAT_BACKGROUND_COLOR_ID)];
            View.NonPlayerColor = ColorList[PrefManager.Get<int>(CHAT_NON_PLAYER_COLOR_ID)];
            View.SelfPlayerColor = ColorList[PrefManager.Get<int>(CHAT_SELF_PLAYER_COLOR_ID)];
            View.OtherPlayerColor = ColorList[PrefManager.Get<int>(CHAT_OTHER_PLAYER_COLOR_ID)];
            View.FontSize = PrefManager.Get<int>(CHAT_FONT_SIZE_ID);
            View.DoFade = PrefManager.Get<bool>(CHAT_DO_FADE_ID);
            View.ActiveBackgroundOpacity = PrefManager.Get<float>(CHAT_ACTIVE_OPACITY);
            View.InactiveBackgroundOpacity = PrefManager.Get<float>(CHAT_INACTIVE_OPACITY);
            View.WindowWidthPercent = PrefManager.Get<float>(CHAT_WINDOW_WIDTH_PERCENT_ID);
            View.WindowHeightPercent = PrefManager.Get<float>(CHAT_WINDOW_HEIGHT_PERCENT_ID);
        }

        public static ChatWindowManager CreateChatWindowManager(int windowID, PreferenceSystemManager prefManager)
        {
            GameObject gameObject = new GameObject("Chat Window Manager");
            ChatWindowManager manager = gameObject.AddComponent<ChatWindowManager>();
            manager.WindowID = windowID;
            return manager;
        }

        private static bool TryGetMessageWindowView(int windowID, out MessageWindowView view)
        {
            foreach (MessageWindowView messageWindowView in Object.FindObjectsOfType<MessageWindowView>())
            {
                if (messageWindowView.ID != windowID)
                    continue;
                view = messageWindowView;
                return true;
            }
            view = null;
            return false;
        }

        public static void CreatePreferences(string modGUID, string modName)
        {
            int[] colorKeys = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            string[] colorStrings = new string[] { "Red", "Green", "Blue", "White", "Black", "Yellow", "Cyan", "Magenta", "Gray" };

            float[] percentKeys = new float[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f };
            string[] percentStrings = new string[] { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };

            float[] sizePercentKeys = new float[] { 0.5f, 0.1f, 0.15f, 0.2f, 0.25f, 0.3f, 0.35f, 0.4f, 0.45f, 0.5f };
            string[] sizePercentStrings = new string[] { "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%", "45%", "50%" };

            PrefManager = new PreferenceSystemManager(modGUID, modName);
            PrefManager
                .AddLabel("Chat Settings")
                .AddOption<bool>(
                    CHAT_ENABLED_ID,
                    true,
                    new bool[] { false, true },
                    new string[] { "Disabled", "Enabled" })
                .AddSubmenu("Size", "size")
                    .AddLabel("Font Size")
                    .AddOption<int>(
                        CHAT_FONT_SIZE_ID,
                        12,
                        new int[] { 8, 10, 12, 14, 16, 18, 20, 24, 28, 32 },
                        new string[] { "8", "10", "12", "14", "16", "18", "20", "24", "28", "32" })
                    .AddLabel("Window Width")
                    .AddOption<float>(
                        CHAT_WINDOW_WIDTH_PERCENT_ID,
                        0.2f,
                        sizePercentKeys,
                        sizePercentStrings)
                    .AddLabel("Window Height")
                    .AddOption<float>(
                        CHAT_WINDOW_HEIGHT_PERCENT_ID,
                        0.15f,
                        sizePercentKeys,
                        sizePercentStrings)
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSubmenu("Colors", "colors")
                    .AddLabel("Background Color")
                    .AddOption<int>(
                        CHAT_BACKGROUND_COLOR_ID,
                        8,
                        colorKeys,
                        colorStrings)
                    .AddLabel("Non-player Color")
                    .AddOption<int>(
                        CHAT_NON_PLAYER_COLOR_ID,
                        5,
                        colorKeys,
                        colorStrings)
                    .AddLabel("My Player Color")
                    .AddOption<int>(
                        CHAT_SELF_PLAYER_COLOR_ID,
                        0,
                        colorKeys,
                        colorStrings)
                    .AddLabel("Other Players Color")
                    .AddOption<int>(
                        CHAT_OTHER_PLAYER_COLOR_ID,
                        3,
                        colorKeys,
                        colorStrings)
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSubmenu("Behaviour", "behaviour")
                    .AddLabel("Change Opacity When Inactive")
                    .AddOption<bool>(
                        CHAT_DO_FADE_ID,
                        true,
                        new bool[] { false, true },
                        new string[] { "Disabled", "Enabled" })
                    .AddLabel("Active Opacity")
                    .AddOption<float>(
                        CHAT_ACTIVE_OPACITY,
                        0.8f,
                        percentKeys,
                        percentStrings)
                    .AddLabel("Inactive Opacity")
                    .AddOption<float>(
                        CHAT_INACTIVE_OPACITY,
                        0.2f,
                        percentKeys,
                        percentStrings)
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSpacer()
                .AddSpacer();

            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
        }
    }
}
