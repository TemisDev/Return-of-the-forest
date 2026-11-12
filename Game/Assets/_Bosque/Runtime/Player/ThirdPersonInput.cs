using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.Player
{
    [RequireComponent(typeof(ThirdPersonMotor))]
    public sealed class ThirdPersonInput : MonoBehaviour
    {
        [SerializeField] private ThirdPersonCamera cameraRig;
        private ThirdPersonMotor motor;
        public void Configure(ThirdPersonCamera rig) => cameraRig = rig;
        private void Awake() => motor = GetComponent<ThirdPersonMotor>();
        private void Start() => SetCursor(true);
        private void OnDisable() => SetCursor(false);
        private void OnApplicationFocus(bool focus) { if (!focus) SetCursor(false); }
        private static void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) SetCursor(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Application.isFocused)
                SetCursor(true);
            Vector2 move = Vector2.zero;
            bool jump = false;
            if (Application.isFocused && Cursor.lockState == CursorLockMode.Locked && keyboard != null)
            {
                move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                jump = keyboard.spaceKey.wasPressedThisFrame;
            }
            motor.Simulate(move, jump, cameraRig != null ? cameraRig.Yaw : 0, Time.deltaTime);
        }
    }
}
