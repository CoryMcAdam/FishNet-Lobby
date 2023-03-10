using UnityEngine.InputSystem;

namespace CMDev.Networking.Lobby.Input
{
    /// <summary>
    /// A static class to help manage the primary input when connecting to a lobby.
    /// </summary>
    public static class InputManager
    {
        /// <summary>
        /// The primary input device for the local client.
        /// </summary>
        public static InputDevice PrimaryInput { get; private set; }

        /// <summary>
        /// Set the primary input device for the local client.
        /// </summary>
        /// <param name="device">The new primary input device.</param>
        public static void SetPrimaryDevice(InputDevice device)
        {
            PrimaryInput = device;
        }
    }
}