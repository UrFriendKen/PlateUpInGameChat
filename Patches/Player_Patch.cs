using HarmonyLib;
using Kitchen;
using KitchenInGameChat.MessageWindow;

namespace KitchenInGameChat.Patches
{
    [HarmonyPatch]
    static class Player_Patch
    {
        [HarmonyPatch(typeof(Player), nameof(Player.ReportNewInput))]
        [HarmonyPrefix]
        static bool ReportNewInput_Prefix(ref Player __instance)
        {
            if (MessageWindowView.ShouldBlockInputForPlayer(__instance.Identifier.PlayerID))
            {
                __instance.RefreshLiveness();
                return false;
            }
            return true;
        }
    }
}
