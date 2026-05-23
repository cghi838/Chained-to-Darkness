using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Current Input")]
    public Vector2 Move;
    public bool JumpPressedThisFrame;
    public bool JumpHeld;

    private void Update()
    {
        ReadMovement();
        ReadJump();
    }

    private void LateUpdate()
    {
        // Reset one-frame input after other scripts have had a chance to read it
        JumpPressedThisFrame = false;
    }

    private void ReadMovement()
    {
        Vector2 keyboardMove = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) keyboardMove.x -= 1f;
            if (Keyboard.current.dKey.isPressed) keyboardMove.x += 1f;
            if (Keyboard.current.sKey.isPressed) keyboardMove.y -= 1f;
            if (Keyboard.current.wKey.isPressed) keyboardMove.y += 1f;
        }

        Vector2 stickMove = Vector2.zero;
        Vector2 dpadMove = Vector2.zero;

        if (Gamepad.current != null)
        {
            stickMove = Gamepad.current.leftStick.ReadValue();
            dpadMove = Gamepad.current.dpad.ReadValue();
        }

        Vector2 combinedMove = keyboardMove + stickMove + dpadMove;

        Move = Vector2.ClampMagnitude(combinedMove, 1f);
    }

    private void ReadJump()
    {
        bool keyboardPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool controllerPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        bool keyboardHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        bool controllerHeld = Gamepad.current != null && Gamepad.current.buttonSouth.isPressed;

        if (keyboardPressed || controllerPressed)
            JumpPressedThisFrame = true;

        JumpHeld = keyboardHeld || controllerHeld;
    }
}