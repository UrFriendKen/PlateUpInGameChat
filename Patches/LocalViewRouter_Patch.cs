using HarmonyLib;
using Kitchen;
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

            if (view_type == Main.MessageWindowViewType)
            {
                __result = messageWindowPrefab;
                return false;
            }
            return true;
        }

        //[HarmonyPatch(typeof(LocalViewRouter), "GetPrefab")]
        //[HarmonyPostfix]
        //static void GetPrefab_Postfix(ViewType view_type, ref GameObject __result)
        //{
        //    if (view_type == Main.MessageWindowViewType)
        //    {
        //        __result = inGameChatContainerPrefab;
        //    }
        //}
    }
}
