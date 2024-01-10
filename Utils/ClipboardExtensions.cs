using UnityEngine;

namespace KitchenInGameChat.Utils
{
    public static class ClipboardExtensions
    {
        public static void CopyToClipboard(this object o)
        {
            GUIUtility.systemCopyBuffer = o.ToString();
        }
    }
}
