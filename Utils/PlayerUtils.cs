using Controllers;
using Kitchen;

namespace KitchenInGameChat.Utils
{
    public static class PlayerUtils
    {
        public static ControllerType GetLocalPlayerControllerType(int playerId)
        {
            return InputSourceIdentifier.DefaultInputSource.GetCurrentController(playerId);
        }

        public static bool IsSessionSingleplayerOrLocalMultiplayer()
        {
            foreach (PlayerInfo player in Players.Main.All())
            {
                if (!player.IsLocalUser)
                    return false;
            }
            return true;
        }
    }
}
