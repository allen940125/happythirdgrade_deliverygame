using Gamemanager;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.Actors
{
    public class CameraCursor : MonoBehaviour
    {
        [SerializeField] private InputActionReference cameraToggleInputAction;
        [SerializeField] private bool startHidden;

        [SerializeField] private CinemachineInputAxisController cinemachineInputAxisController;
        [SerializeField] private bool disableCameraLookOnCursorVisible;
        [SerializeField] private bool disableCameraZoomOnCursorVisible;

        [Tooltip(
            "If you're using Cinemachine 2.8.4 or under, untick this option.\nIf unticked, both Look and Zoom will be disabled.")]
        [SerializeField]
        private bool fixedCinemachineVersion;

        [Header("Input References")] public InputActionReference lookAction; // 拖入 Inspector 中的 Player/Look Action
        
        private void Awake()
        {
            Debug.Log("Camera Cursor Awake");
            cameraToggleInputAction.action.started += OnCameraCursorToggled;

            if (startHidden)
            {
                ToggleCursor();
            }

            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnCursorToggledEvent, cmd => { ToggleCursor(cmd.ShowCursor ?? false); });
        }

        private void OnEnable()
        {
            Debug.Log("cameraToggleInputAction的Asset開啟");
            cameraToggleInputAction.asset.Enable();
        }

        private void OnDisable()
        {
            Debug.Log("cameraToggleInputAction的Asset關閉");
            cameraToggleInputAction.action.started -= OnCameraCursorToggled;
            cameraToggleInputAction.asset.Disable();
            GameManager.Instance.MainGameEvent.Unsubscribe<CursorToggledEvent>();
        }

        private void OnCameraCursorToggled(InputAction.CallbackContext context)
        {
            ToggleCursor();
        }

        private void ToggleCursor(bool? showCursor = null)
        {
            // 如果 showCursor 為 null，則執行切換邏輯
            if (showCursor == null)
            {
                Cursor.visible = !Cursor.visible;
            }
            else
            {
                Cursor.visible = showCursor.Value;
            }

            if (!Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Locked;

                if (!fixedCinemachineVersion)
                {
                    cinemachineInputAxisController.enabled = true;
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;

                if (!fixedCinemachineVersion)
                {
                    cinemachineInputAxisController.enabled = false;
                }
            }
        }
    }
}