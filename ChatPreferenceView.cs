using PreferenceSystem;
using System;
using UnityEngine;

namespace KitchenInGameChat
{
    internal sealed class ChatPreferenceView : GenericWindowPreferenceView
    {
        public override string ModGUID => Main.MOD_GUID;
        public override string TargetWindowName => Main.CHAT_WINDOW_NAME;

        private static PreferenceSystemManager PrefManager;

        public const string CHAT_FONT_SIZE_ID = "chatFontSize";
        public const string CHAT_BACKGROUND_COLOR_ID = "chatBackgroundColor";
        public const string CHAT_NON_PLAYER_COLOR_ID = "chatNonPlayerColor";
        public const string CHAT_SELF_PLAYER_COLOR_ID = "chatSelfPlayerColor";
        public const string CHAT_OTHER_PLAYER_COLOR_ID = "chatOtherPlayerColor";
        public const string CHAT_ENABLED_ID = "chatHide";
        public const string CHAT_DO_FADE_ID = "chatDoFade";
        public const string CHAT_INACTIVITY_TIMEOUT = "chatInactivityTimeout";
        public const string CHAT_HIDE_HISTORY_WHEN_FADED_ID = "chatHideHistoryWhenFaded";
        public const string CHAT_HIDE_SCROLLBAR_WHEN_FADED_ID = "chatHideScrollbarWhenFaded";
        public const string CHAT_ACTIVE_OPACITY = "chatActiveOpacity";
        public const string CHAT_INACTIVE_OPACITY = "chatInactiveOpacity";
        public const string CHAT_WINDOW_WIDTH_PERCENT_ID = "chatWindowWidthPercent";
        public const string CHAT_WINDOW_HEIGHT_PERCENT_ID = "chatWindowHeightPercent";
        public const string CHAT_DRAG_HANDLE_HEIGHT_PERCENT_ID = "chatDragHandleHeightPercent";
        public const string CHAT_WINDOW_IS_FIXED_POS_ID = "chatWindowFixedPosition";
        public const string CHAT_WINDOW_FIXED_POS_X_ID = "chatWindowFixedPositionX";
        public const string CHAT_WINDOW_FIXED_POS_Y_ID = "chatWindowFixedPositionY";
        public const string CHAT_EMPTY_SEND_DEFOCUS_DELAY = "chatEmptySendDefocusDelay";
        public const string CHAT_SEND_MESSAGE_KEY_ID = "chatSendMessageKey";
        public const string CHAT_SEND_KEY_FOCUSES_TEXT_FIELD_ID = "chatSendKeyFocusesTextField";
        public const string CHAT_ALT_FOCUS_TEXT_FIELD_KEY_ID = "chatAltFocusTextFieldKey";
        public const string CHAT_DEFOCUS_TEXT_FIELD_KEY_ID = "chatDefocusTextFieldKey";

        protected override void UpdateWindowPreferences(MessageWindowView window)
        {
            window.DoNotDrawOverride = !PrefManager.Get<bool>(CHAT_ENABLED_ID);
            window.BackgroundColor = StandardColors[PrefManager.Get<int>(CHAT_BACKGROUND_COLOR_ID)];
            window.NonPlayerColor = StandardColors[PrefManager.Get<int>(CHAT_NON_PLAYER_COLOR_ID)];
            window.SelfPlayerColor = StandardColors[PrefManager.Get<int>(CHAT_SELF_PLAYER_COLOR_ID)];
            window.OtherPlayerColor = StandardColors[PrefManager.Get<int>(CHAT_OTHER_PLAYER_COLOR_ID)];
            window.FontSize = PrefManager.Get<int>(CHAT_FONT_SIZE_ID);
            window.DoFade = PrefManager.Get<bool>(CHAT_DO_FADE_ID);
            window.ActiveBackgroundOpacityTime = PrefManager.Get<float>(CHAT_INACTIVITY_TIMEOUT);

            window.HideHistoryWhenFaded = PrefManager.Get<bool>(CHAT_HIDE_HISTORY_WHEN_FADED_ID); ;
            window.HideScrollbarWhenFaded = PrefManager.Get<bool>(CHAT_HIDE_SCROLLBAR_WHEN_FADED_ID); ;

            window.ActiveBackgroundOpacity = PrefManager.Get<float>(CHAT_ACTIVE_OPACITY);
            window.InactiveBackgroundOpacity = PrefManager.Get<float>(CHAT_INACTIVE_OPACITY);
            window.WindowWidthPercent = PrefManager.Get<float>(CHAT_WINDOW_WIDTH_PERCENT_ID);
            window.WindowHeightPercent = PrefManager.Get<float>(CHAT_WINDOW_HEIGHT_PERCENT_ID);
            window.DragHandleHeightPercent = PrefManager.Get<float>(CHAT_DRAG_HANDLE_HEIGHT_PERCENT_ID);

            KeyCode keyCode;
            if (Enum.TryParse(PrefManager.Get<string>(CHAT_SEND_MESSAGE_KEY_ID), out keyCode))
                window.SendMessageKeyCode = keyCode;
            window.IsSendKeyFocusesTextField = PrefManager.Get<bool>(CHAT_SEND_KEY_FOCUSES_TEXT_FIELD_ID);
            if (Enum.TryParse(PrefManager.Get<string>(CHAT_ALT_FOCUS_TEXT_FIELD_KEY_ID), out keyCode))
                window.AltFocusTextFieldKeyCode = keyCode;
            if (Enum.TryParse(PrefManager.Get<string>(CHAT_DEFOCUS_TEXT_FIELD_KEY_ID), out keyCode))
                window.DefocusTextFieldKeyCode = keyCode;
            window.TextFieldDefocusBySendKeyDelay = PrefManager.Get<float>(CHAT_EMPTY_SEND_DEFOCUS_DELAY);

            window.PreventDragOverride = PrefManager.Get<bool>(CHAT_WINDOW_IS_FIXED_POS_ID);
            if (window.PreventDragOverride)
            {
                int fixedPosX = (int)(PrefManager.Get<float>(CHAT_WINDOW_FIXED_POS_X_ID) * (1f - window.WindowWidthPercent) * Screen.width);
                int fixedPosY = (int)((1f - PrefManager.Get<float>(CHAT_WINDOW_FIXED_POS_Y_ID)) * (1f - window.WindowHeightPercent) * Screen.height);
                Vector2 fixedPosition = new Vector2(fixedPosX, fixedPosY);
                if (fixedPosition != window.WindowPos)
                    window.MoveWindow(fixedPosition);
            }
        }

        public static void CreatePreferences(string modGUID, string modName)
        {
            int[] colorKeys = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            string[] colorStrings = new string[] { "Red", "Green", "Blue", "White", "Black", "Yellow", "Cyan", "Magenta", "Gray" };

            float[] percentKeys = new float[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f };
            string[] percentStrings = new string[] { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };

            float[] sizePercentKeys = new float[] { 0.05f, 0.1f, 0.15f, 0.2f, 0.25f, 0.3f, 0.35f, 0.4f, 0.45f, 0.5f };
            string[] sizePercentStrings = new string[] { "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%", "45%", "50%" };

            float[] dragHandleSizePercentKeys = new float[] { 0.02f, 0.025f, 0.03f, 0.035f, 0.04f, 0.045f, 0.05f, 0.055f, 0.06f, 0.065f, 0.07f, 0.075f, 0.08f, 0.085f, 0.09f, 0.095f, 0.1f };
            string[] dragHandleSizePercentStrings = new string[] { "2%", "2.5%", "3%", "3.5%", "4%", "4.5%", "5%", "5.5%", "6%", "6.5%", "7%", "7.5%", "8%", "8.5%", "9%", "9.5%", "10%" };

            float[] shortTimeSecondsKeys = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f, 1.6f, 1.7f, 1.8f, 1.9f, 2f };
            string[] shortTimeSecondsStrings = new string[] { "0.1", "0.2", "0.3", "0.4", "0.5", "0.6", "0.7", "0.8", "0.9", "1.0", "1.1", "1.2", "1.3", "1.4", "1.5", "1.6", "1.7", "1.8", "1.9", "2.0" };

            float[] longTimeSecondsKeys = new float[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f, 12f, 13f, 14f, 15f, 16f, 17f, 18f, 19f, 20f };
            string[] longTimeSecondsStrings = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" };

            string[] keyCodeStrings = Enum.GetNames(typeof(KeyCode));

            PrefManager = new PreferenceSystemManager(modGUID, modName);
            PrefManager
                .AddLabel("Chat Settings")
                .AddOption<bool>(
                    CHAT_ENABLED_ID,
                    true,
                    new bool[] { false, true },
                    new string[] { "Disabled", "Enabled" })
                .AddSpacer()
                .AddSubmenu("Size and Position", "sizeAndPosition")
                    .AddLabel("Window Width")
                    .AddOption<float>(
                        CHAT_WINDOW_WIDTH_PERCENT_ID,
                        0.35f,
                        sizePercentKeys,
                        sizePercentStrings)
                    .AddLabel("Window Height")
                    .AddOption<float>(
                        CHAT_WINDOW_HEIGHT_PERCENT_ID,
                        0.25f,
                        sizePercentKeys,
                        sizePercentStrings)
                    .AddLabel("Window Position")
                    .AddOption<bool>(
                        CHAT_WINDOW_IS_FIXED_POS_ID,
                        true,
                        new bool[] { false, true },
                        new string[] { "Draggable", "Fixed" })
                    .AddLabel("X Position (When Fixed)")
                    .AddOption<float>(
                        CHAT_WINDOW_FIXED_POS_X_ID,
                        0f,
                        percentKeys,
                        percentStrings)
                    .AddLabel("Y Position (When Fixed)")
                    .AddOption<float>(
                        CHAT_WINDOW_FIXED_POS_Y_ID,
                        0f,
                        percentKeys,
                        percentStrings)
                    .AddSpacer()
                    .AddLabel("Font Size")
                    .AddOption<int>(
                        CHAT_FONT_SIZE_ID,
                        18,
                        new int[] { 8, 10, 12, 14, 16, 18, 20, 24, 28, 32 },
                        new string[] { "8", "10", "12", "14", "16", "18", "20", "24", "28", "32" })
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
                        2,
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
                    .AddLabel("Inactivity Timeout (Seconds)")
                    .AddOption<float>(
                        CHAT_INACTIVITY_TIMEOUT,
                        5f,
                        longTimeSecondsKeys,
                        longTimeSecondsStrings)
                    .AddLabel("Active Opacity")
                    .AddOption<float>(
                        CHAT_ACTIVE_OPACITY,
                        1f,
                        percentKeys,
                        percentStrings)
                    .AddLabel("Inactive Opacity")
                    .AddOption<float>(
                        CHAT_INACTIVE_OPACITY,
                        0.1f,
                        percentKeys,
                        percentStrings)
                    .AddLabel("When Inactive Scrollbar Is")
                    .AddOption<bool>(
                        CHAT_HIDE_SCROLLBAR_WHEN_FADED_ID,
                        true,
                        new bool[] { true, false },
                        new string[] { "Hidden", "Visible" })
                    .AddLabel("When Inactive Messages Are")
                    .AddOption<bool>(
                        CHAT_HIDE_HISTORY_WHEN_FADED_ID,
                        false,
                        new bool[] { true, false },
                        new string[] { "Hidden", "Visible" })
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSubmenu("Advanced", "advanced")
                    .AddLabel("Drag Handle Height")
                    .AddOption<float>(
                        CHAT_DRAG_HANDLE_HEIGHT_PERCENT_ID,
                        0.03f,
                        dragHandleSizePercentKeys,
                        dragHandleSizePercentStrings)
                    .AddSpacer()
                    .AddLabel("Empty Send Defocus Delay (Seconds)")
                    .AddOption<float>(
                        CHAT_EMPTY_SEND_DEFOCUS_DELAY,
                        1f,
                        shortTimeSecondsKeys,
                        shortTimeSecondsStrings)
                    .AddLabel("Send Key Focuses Text Field")
                    .AddOption<bool>(
                        CHAT_SEND_KEY_FOCUSES_TEXT_FIELD_ID,
                        false,
                        new bool[] { false, true },
                        new string[] { "Disabled", "Enabled" })
                    .AddSpacer()
                    .AddLabel("Send Message Key Code")
                    .AddOption<string>(
                        CHAT_SEND_MESSAGE_KEY_ID,
                        "Return",
                        keyCodeStrings,
                        keyCodeStrings)
                    .AddLabel("Alternate Text Field Focus Key Code")
                    .AddOption<string>(
                        CHAT_ALT_FOCUS_TEXT_FIELD_KEY_ID,
                        "Slash",
                        keyCodeStrings,
                        keyCodeStrings)
                    .AddLabel("Defocus Text Field Key Code")
                    .AddOption<string>(
                        CHAT_DEFOCUS_TEXT_FIELD_KEY_ID,
                        "Escape",
                        keyCodeStrings,
                        keyCodeStrings)
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSpacer()
                .AddSpacer();

            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
        }
    }
}
