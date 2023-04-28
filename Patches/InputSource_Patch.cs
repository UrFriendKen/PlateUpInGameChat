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
}
