using UnityEngine;

public class GroundTrigger : MonoBehaviour
{
    [Header("地板的 Layer 設定")]
    [SerializeField] private LayerMask groundLayer;

    /// <summary>
    /// 是否踩在地面上
    /// </summary>
    public bool IsGrounded { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (IsInGroundLayer(other.gameObject))
        {
            IsGrounded = true;
            Debug.Log("玩家踩到地面");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsInGroundLayer(other.gameObject))
        {
            IsGrounded = false;
            Debug.Log("玩家離開地面");
        }
    }

    /// <summary>
    /// 判斷對象是否在地板 Layer
    /// </summary>
    private bool IsInGroundLayer(GameObject obj)
    {
        return (groundLayer.value & (1 << obj.layer)) != 0;
    }
}