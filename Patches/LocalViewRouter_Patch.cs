using HarmonyLib;
using Kitchen;
using KitchenInGameChat.MessageWindow;
using UnityEngine;

namespace KitchenInGameChat.Patches
{
    [HarmonyPatch]
    static class LocalViewRouter_Patch
    {
        static GameObject messageWindowPrefab;

        [HarmonyPatch(typeof(LocalViewRouter), "GetPrefab")]
        [HarmonyPrefix]
        static bool GetPrefab_Prefix(ViewType view_type, ref GameObject __result)
        {
            if (messageWindowPrefab == null)
            {
                messageWindowPrefab = new GameObject("MessageWindow");
                messageWindowPrefab.AddComponent<MessageWindowView>();
            }

            if (view_type == MessageWindowController.MessageWindowViewType)
            {
                __result = messageWindowPrefab;
                return false;
            }
            else if (PreferenceViewRegistry.TryGetPrefab(view_type, out GameObject loadedPrefab))
            {
                Main.LogInfo($"{view_type}: {loadedPrefab}");
                __result = loadedPrefab;
                return false;
            }
            return true;
        }
    }
}
