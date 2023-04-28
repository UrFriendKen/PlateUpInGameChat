using Controllers;
using HarmonyLib;
using Kitchen;
using KitchenInGameChat.MessageWindow;

namespace KitchenInGameChat.Patches
{
    [HarmonyPatch]
    static class ProfileEditorView_Patch
    {
        [HarmonyPatch(typeof(ProfileEditorView), nameof(ProfileEditorView.TakeInput))]
        [HarmonyPrefix]
        static bool TakeInput_Prefix(ref int ___PlayerID, ref InputConsumerState __result)
        {
            if (MessageWindowView.ShouldBlockInputForPlayer(___PlayerID))
            {
                __result = InputConsumerState.Consumed;
                return false;
            }
            return true;
        }
    }
}
