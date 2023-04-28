using HarmonyLib;
using Kitchen.Modules;
using KitchenInGameChat.MessageWindow;

namespace KitchenInGameChat.Patches
{
    [HarmonyPatch]
    static class ControlRebindElement_Patch
    {
        [HarmonyPatch(typeof(ControlRebindElement), nameof(ControlRebindElement.HandleInteraction))]
        [HarmonyPrefix]
        static bool HandleInteraction_Prefix(ref int ___PlayerID, ref bool __result)
        {
            if (MessageWindowView.ShouldBlockInputForPlayer(___PlayerID))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
