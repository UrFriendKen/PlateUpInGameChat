using HarmonyLib;
using Kitchen;
using KitchenInGameChat.MessageWindow;

namespace KitchenInGameChat.Patches
{
    [HarmonyPatch]
    static class PlayerPauseView_Patch
    {
        [HarmonyPatch(typeof(PlayerPauseView), "HandleInputState")]
        [HarmonyPrefix]
        static bool HandleInputState_Prefix(ref int ___ActivePlayer)
        {
            return !MessageWindowView.ShouldBlockInputForPlayer(___ActivePlayer);
        }
    }
}
