using Controllers;
using HarmonyLib;

using KitchenInGameChat.MessageWindow;

namespace KitchenInGameChat.Patches
{
    [HarmonyPatch]
    static class InputSource_Patch
    {
        [HarmonyPatch(typeof(InputSource), "SetInputUpdate")]
        [HarmonyPrefix]
        static void GetButtonState_Postfix(int player_id, ref InputState state)
        {
            if (MessageWindowView.ShouldBlockInputForPlayer(player_id))
                state = InputState.Neutral;
        }
    }

    //{
    //    [HarmonyPatch(typeof(InputSource), "GetButtonState")]
    //    [HarmonyPostfix]
    //    static void GetButtonState_Postfix(ref ButtonState __result, ButtonState old)
    //    {
    //        if (__result == ButtonState.Up || __result == ButtonState.Released)
    //            return;

    //        if (old == ButtonState.Consumed)
    //        {
    //            __result = ButtonState.Up;
    //            return;
    //        }

    //        if (old != ButtonState.Up && old != ButtonState.Released)
    //        {
    //            __result = ButtonState.Released;
    //            return;
    //        }
    //        __result = ButtonState.Up;
    //    }

    //    static ButtonState GetRejectedButtonState(ButtonState current)
    //    {
    //        switch (current)
    //        {
    //            case ButtonState.Consumed:
    //            case ButtonState.Pressed:
    //                return ButtonState.Up;
    //            case ButtonState.Held:
    //                return ButtonState.Released;
    //            case ButtonState.Released:
    //            case ButtonState.Up:
    //            default:
    //                return ButtonState.Up;
    //        }
    //    }

    //    static ButtonState GetRejectedButtonState(ButtonState current)
    //    {
    //        switch (current)
    //        {
    //            case ButtonState.Consumed:
    //            case ButtonState.Pressed:
    //                return ButtonState.Up;
    //            case ButtonState.Held:
    //                return ButtonState.Released;
    //            case ButtonState.Released:
    //            case ButtonState.Up:
    //            default:
    //                return ButtonState.Up;
    //        }
    //    }
    //}
}
