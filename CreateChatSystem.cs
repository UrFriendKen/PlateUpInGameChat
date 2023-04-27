using Kitchen;
using KitchenLib.Utils;
using KitchenMods;
using Unity.Entities;

namespace KitchenInGameChat
{
    public struct SChatWindow : IComponentData, IModComponent
    {
        public int ID;
    }
    public struct SChatWindowPref : IComponentData, IModComponent
    {
        public int ID;
    }

    public sealed class CreateChatSystem : GameSystemBase
    {
        protected override void OnUpdate()
        {
            if (!Has<SChatWindow>())
            {
                SChatWindow chatWindow = new SChatWindow()
                {
                    // Instead of doing this, store a list of registered windows in MessageWindowController. Then in OnUpdate() check if the window still exists by id? Or does CDoNotPersist take care of that?
                    ID = MessageWindowController.CreateMessageWindow(Main.MOD_GUID, Main.CHAT_WINDOW_NAME, hideName: true, isReadOnly: false, maxMessageCount: 50, callback: ChatMessageCallback)
                };
                Entity singleton = Set(chatWindow);
                Set<CPersistThroughSceneChanges>(singleton);
                Set<CDoNotPersist>(singleton);
            }

            if (!Has<SChatWindowPref>())
            {
                if (Main.ChatPrefViewType.HasValue)
                {
                    SChatWindowPref chatWindowPref = new SChatWindowPref();
                    Entity chatWindowPrefSingleton = Set(chatWindowPref);
                    Set(chatWindowPrefSingleton, new CRequiresView()
                    {
                        Type = Main.ChatPrefViewType.Value,
                        PhysicsDriven = false,
                        ViewMode = ViewMode.World
                    });
                    Set<CPersistThroughSceneChanges>(chatWindowPrefSingleton);
                    Set<CDoNotPersist>(chatWindowPrefSingleton);
                }
            }
        }

        private void ChatMessageCallback(int messageWindowID, StaticMessageRequest messageRequest)
        {
            if (!messageRequest.IsPlayerMessage)
                return;
            if (!messageRequest.Text.StartsWith("/"))
                return;

            HandleCommand(messageRequest, out string statusMessage);
            if (!statusMessage.IsNullOrEmpty())
            {
                MessageWindowController.SendMessage(messageWindowID, "Command Handler", statusMessage);
            }
        }

        private bool HandleCommand(StaticMessageRequest messageRequest, out string statusMessage)
        {
            statusMessage = string.Empty;
            if (!TryParseCommandAndParameters(messageRequest, out string[] tokens))
            {
                statusMessage = "Invalid Command!";
                return false;
            }
            bool success = false;

            // Try find registered command handler (WIP) and attempt command. Command handler should return whether the command was successfully executed and a status message to display, if any. (Otherwise empty string)

            return success;
        }

        private bool TryParseCommandAndParameters(StaticMessageRequest messageRequest, out string[] tokens)
        {
            // To implement command system
            tokens = new string[] { };
            return false;
        }
    }
}
