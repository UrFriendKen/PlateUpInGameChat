using Controllers;

namespace KitchenInGameChat.Utils
{
    public static class PlayerUtils
    {
        public static ControllerType GetLocalPlayerControllerType(int playerId)
        {
            return InputSourceIdentifier.DefaultInputSource.GetCurrentController(playerId);
        }
    }
}
