using UnityEngine;

public class InputManager : MonoBehaviour
{
    public enum InputMode
    {
        KeyboardMouse
    }

    [Header("입력 모드 선택")]
    [SerializeField] private InputMode currentInputMode = InputMode.KeyboardMouse;

    public InputMode CurrentInputMode => currentInputMode;
}
