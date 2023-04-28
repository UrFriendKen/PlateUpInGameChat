using HarmonyLib;
using Kitchen.Modules;
using KitchenInGameChat.MessageWindow;

namespace KitchenInGameChat.Patches
{
    [HarmonyPatch]
    static class ConsentElement_Patch
    {
        [HarmonyPatch(typeof(ConsentElement), nameof(ConsentElement.SetConsent))]
        static void HandleInputState_Prefix(int i, ref bool value)
        {
            if (MessageWindowView.ShouldBlockInputForPlayer(i))
                value = false;
        }
    }
}
