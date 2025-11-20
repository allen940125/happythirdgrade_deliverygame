using UnityEngine;
using Gamemanager;
using Cysharp.Threading.Tasks;

// 假設您已經定義了 SessionSingleton<T>
public class WaypointManager : SessionSingleton<WaypointManager> 
{
    // 假設 WaypointIndicator 是您的實際指標Prefab（例如一個旋轉的箭頭）
    [SerializeField] private GameObject WaypointIndicatorPrefab; 
    
    // 用於追踪當前目標的變數
    private Transform _targetTransform;
    private GameObject _currentIndicatorInstance;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    // 【核心方法】: 設置或移除指標目標
    public void SetTarget(Transform target)
    {
        // 1. 清理舊指標
        if (_currentIndicatorInstance != null)
        {
            Destroy(_currentIndicatorInstance);
            _currentIndicatorInstance = null;
        }

        _targetTransform = target;

        if (_targetTransform != null && GameManager.Instance.Player != null)
        {
            // 2. 實例化新指標，設為 WaypointManager (this.transform) 的子物件
            _currentIndicatorInstance = Instantiate(WaypointIndicatorPrefab, this.transform); 
            
            // 【關鍵修改】：設置初始世界位置
            // 即使它是子物件，我們也可以設置它的世界位置
            _currentIndicatorInstance.transform.position = 
                GameManager.Instance.Player.transform.position + 
                _currentIndicatorInstance.GetComponent<WaypointIndicator>().GetInitialOffset(); // 獲取初始偏移
            
            // 3. 讓指標追蹤目標
            _currentIndicatorInstance.GetComponent<WaypointIndicator>().Track(_targetTransform);
        }
    }
}