using UnityEngine.InputSystem;

namespace CMDev.Networking.Lobby.Input
{
    public static class InputManager
    {
        public static InputDevice PrimaryInput { get; private set; }

        public static void SetPrimaryDevice(InputDevice device)
        {
            PrimaryInput = device;
        }
    }
}