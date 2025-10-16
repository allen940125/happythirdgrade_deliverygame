using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.Actors
{
    public class CameraZoom : MonoBehaviour
    {
        [Header("Distance Settings")]
        [SerializeField] [Range(0f, 12f)] private float defaultDistance = 6f;
        [SerializeField] [Range(0f, 12f)] private float minimumDistance = 1f;
        [SerializeField] [Range(0f, 12f)] private float maximumDistance = 12f;

        [Header("Zoom Settings")]
        [SerializeField] [Range(0f, 20f)] private float smoothing = 4f;
        [SerializeField] [Range(0f, 20f)] private float zoomSensitivity = 1f;

        [Header("Input")]
        [SerializeField] private InputActionReference zoomInputAction; // 滚轮输入

        private CinemachinePositionComposer cinemachinePositionComposer;
        private float currentTargetDistance;

        private void Awake()
        {
            cinemachinePositionComposer = GetComponent<CinemachinePositionComposer>();
            currentTargetDistance = defaultDistance;

            Debug.Log(zoomInputAction + "跟" + zoomInputAction.action);
            // 启用输入
            if (zoomInputAction != null && zoomInputAction.action != null)
            {
                Debug.Log(zoomInputAction.action.name + "輸入啟動");
                zoomInputAction.action.Enable();
            }
        }

        private void Update()
        {
            Zoom();
        }

        private void Zoom()
        {
            // 检查输入是否为空
            if (zoomInputAction == null || zoomInputAction.action == null)
                return;

            //Debug.Log(zoomInputAction.action.ReadValue<float>());
            
            // 使用 float 类型读取滚轮输入值
            float zoomValue = -zoomInputAction.action.ReadValue<float>() * zoomSensitivity;

            // 更新目标距离
            currentTargetDistance = Mathf.Clamp(currentTargetDistance - zoomValue, minimumDistance, maximumDistance);

            // 当前距离
            float currentDistance = cinemachinePositionComposer.CameraDistance;

            // 平滑过渡
            if (Mathf.Abs(currentDistance - currentTargetDistance) > 0.01f)
            {
                float lerpedZoomValue = Mathf.Lerp(currentDistance, currentTargetDistance, smoothing * Time.deltaTime);
                cinemachinePositionComposer.CameraDistance = lerpedZoomValue;
            }
        }


        private void OnDisable()
        {
            // 禁用输入
            if (zoomInputAction != null && zoomInputAction.action != null)
            {
                zoomInputAction.action.Disable();
            }
        }
    }
}
