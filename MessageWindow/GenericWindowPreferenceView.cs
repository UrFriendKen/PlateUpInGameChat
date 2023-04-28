using Kitchen;
using KitchenLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace KitchenInGameChat.MessageWindow
{
    public abstract class GenericWindowPreferenceView : GenericObjectView
    {
        public abstract string ModGUID { get; }
        public abstract string TargetWindowName { get; }

        private MessageWindowView _linkedWindowView;

        public ViewType Type => (ViewType)VariousUtils.GetID($"{ModGUID}:{GetType().Name}");

        protected virtual List<Color> StandardColors => new List<Color>()
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.white,
            Color.black,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            Color.gray
        };

        void Update()
        {
            if (!_linkedWindowView && !TryGetChatWindowView(out _linkedWindowView))
                return;
            if (!_linkedWindowView.IsInit)
                return;

            UpdateWindowPreferences(_linkedWindowView);
        }

        protected abstract void UpdateWindowPreferences(MessageWindowView window);

        private bool TryGetChatWindowView(out MessageWindowView view)
        {
            foreach (MessageWindowView messageWindowView in FindObjectsOfType<MessageWindowView>())
            {
                if (messageWindowView.ID != MessageWindowController.GetWindowID(ModGUID, TargetWindowName))
                    continue;
                view = messageWindowView;
                return true;
            }
            view = null;
            return false;
        }
    }
}
