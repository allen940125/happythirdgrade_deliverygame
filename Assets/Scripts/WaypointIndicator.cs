using UnityEngine;
using Gamemanager; 
using System;

public class WaypointIndicator : MonoBehaviour
{
    private Transform _target;
    private Transform _player; 
    private SimpleCarController _carController; 
    
    // --- 配置參數 ---
    [Header("位置設定")]
    [SerializeField] private Vector3 LocalOffset = new Vector3(0, 3f, 0); 
    
    [Header("追蹤阻尼設定 (消除抖動的最佳選擇)")]
    [Tooltip("將指標速度追蹤到目標速度所需的時間。數值越小，反應越快（越硬）。推薦 0.1 - 0.5。")]
    [SerializeField] private float SmoothDampTime = 0.2f; 

    [Header("遠距離動態追逐 (適應車速)")]
    [Tooltip("指標追趕玩家的速度乘數。設為 1.2 表示指標速度比玩家快 20%。")]
    [SerializeField] private float PlayerSpeedMultiplier = 1.2f; 
    
    [Tooltip("最低追趕速度。即使玩家靜止 (0 m/s)，指標也會以該速度追趕。")]
    [SerializeField] private float MinCatchupSpeed = 5f; 

    [Header("旋轉速度設定")]
    [Tooltip("每秒旋轉到目標方向的速度。")]
    [SerializeField] private float RotationSpeed = 5f;

    // --- 內部緩衝變數 ---
    private Vector3 _velocity = Vector3.zero; // SmoothDamp 需要速度參考
    
    // --- 觀察用 ---
    [SerializeField] private float curMaxSpeed; // 觀察用的當前最大追趕速度


    public Vector3 GetInitialOffset() { return LocalOffset; }

    public void Track(Transform target)
    {
        _target = target;
        
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            _player = GameManager.Instance.Player.transform;
            _carController = GameManager.Instance.Player.GetComponent<SimpleCarController>();
            if (_carController == null)
            {
                Debug.LogError("[WaypointIndicator] 無法找到 SimpleCarController！追蹤速度將為 MinCatchupSpeed。");
            }
        }
    }

    // 將 LateUpdate 改為 Update 或 FixedUpdate。
    // 由於我們使用 SmoothDamp，且跟隨物理車輛，Update 是合理的選擇。
    private void FixedUpdate() 
    {
        if (_target == null || _player == null)
        {
            Destroy(gameObject); 
            return;
        }

        // 1. 計算理想的世界目標位置 (玩家位置 + 偏移)
        Vector3 targetWorldPosition = _player.position + LocalOffset;

        // --- 動態追蹤速度計算 ---
        float currentCarSpeed = 0f;
        if (_carController != null)
        {
            currentCarSpeed = _carController.currentSpeed_S; 
        }
        
        // 指標最大追趕速度 = Max(玩家速度 * 乘數, 最低速度)
        float baseCatchupSpeed = currentCarSpeed * PlayerSpeedMultiplier;
        float maxCatchupSpeed = Mathf.Max(baseCatchupSpeed, MinCatchupSpeed);
        
        curMaxSpeed = maxCatchupSpeed; // 觀察用
        
        // --- 使用 SmoothDamp 實現無抖動追蹤 ---
        
        // 核心邏輯：平滑地將當前位置追蹤到目標位置
        // ref _velocity 參數確保了平滑過渡，消除抖動和超調
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetWorldPosition, 
            ref _velocity, 
            SmoothDampTime, // 追蹤所需時間
            maxCatchupSpeed // 限制最大速度 (防止速度變化過大)
        );


        // 2. 平滑旋轉 (Smooth Rotation) - 保持不變
        
        Vector3 direction = _target.position - transform.position;
        direction.y = 0; 

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(direction);
            
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                desiredRotation, 
                Time.deltaTime * RotationSpeed
            );
        }
    }
}