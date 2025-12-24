using UnityEngine;
using UnityEngine.UI; // 為了控制 UI
using Gamemanager; // 引用你的事件系統

public class PlayerArrestHandler : MonoBehaviour
{
    [Header("逮捕判定設定")]
    [Tooltip("玩家速度低於多少算「停駛」(km/h)")]
    public float arrestSpeedThreshold = 10f; 

    [Tooltip("警車進入多少半徑內開始計算逮捕")]
    public float detectionRadius = 8f;

    [Tooltip("需要持續在範圍內多久才會被抓 (秒)")]
    public float timeToBusted = 3f;

    [Header("圖層設定 (重要)")]
    [Tooltip("請設定 Enemy 的 Layer，這裡只偵測警車")]
    public LayerMask policeLayer;

    [Header("UI 連結")]
    [Tooltip("顯示逮捕進度的圖條 (Image Type 設為 Filled)")]
    public Image bustedBarUI;
    [Tooltip("逮捕中的文字提示 (例如 'BUSTING...')")]
    public GameObject bustingTextObj;

    // 內部變數
    private PlayerCarController playerController;
    private float currentArrestTimer = 0f;
    private bool isArrested = false;

    private void Awake()
    {
        playerController = GetComponent<PlayerCarController>();
    }

    private void Update()
    {
        if (isArrested) return; // 已經被抓就不跑了

        // 1. 檢查速度 (如果車速太快，直接重置計時)
        // 這裡我們取絕對值，因為倒車太快也不算被抓
        if (Mathf.Abs(playerController.CurrentSpeedKmH) > arrestSpeedThreshold)
        {
            ResetArrest();
            return;
        }

        // 2. 檢查周圍有沒有警車 (使用 Physics.OverlapSphere)
        // 這比去問每一台警車「你在哪」效率高很多
        bool isPoliceNearby = CheckForPolice();

        if (isPoliceNearby)
        {
            // 3. 累積逮捕值
            ProcessArrest();
        }
        else
        {
            // 沒警車在旁邊，慢慢恢復
            RecoverArrest();
        }

        // 更新 UI
        UpdateUI();
    }

    bool CheckForPolice()
    {
        // 在玩家位置畫一顆球，只偵測 policeLayer
        Collider[] cops = Physics.OverlapSphere(transform.position, detectionRadius, policeLayer);
        return cops.Length > 0;
    }

    void ProcessArrest()
    {
        currentArrestTimer += Time.deltaTime;

        if (currentArrestTimer >= timeToBusted)
        {
            TriggerBusted();
        }
    }

    void RecoverArrest()
    {
        if (currentArrestTimer > 0)
        {
            currentArrestTimer -= Time.deltaTime;
        }
    }

    void ResetArrest()
    {
        currentArrestTimer = 0f;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (bustedBarUI != null)
        {
            float progress = currentArrestTimer / timeToBusted;
            bustedBarUI.fillAmount = progress;
            
            // 只有在累積時才顯示 UI
            if (bustingTextObj != null)
                bustingTextObj.SetActive(progress > 0);
                
            bustedBarUI.gameObject.SetActive(progress > 0);
        }
    }

    void TriggerBusted()
    {
        isArrested = true;
        Debug.Log("<color=red>BUSTED! 你被逮捕了!</color>");

        // 1. 鎖住玩家操作
        playerController.SetDrivable(false);

        GameManager.Instance.MainGameEvent.Send(new GameOverEvent(GameOverReason.CarDestroyed));
        
        // 2. 發送遊戲結束事件
        // 假設你有定義這個事件，或直接呼叫 Manager
        // GameManager.Instance.MainGameEvent.Send(new PlayerBustedEvent());
        
        // 或是簡單的：
        // GameManager.Instance.TriggerGameOver("BUSTED");
    }

    // 畫出偵測範圍，方便在編輯器調整
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
    }
}