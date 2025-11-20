using Gamemanager;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class GameHUD : BasePanel 
    {
        [Header("控制按鈕參照")]
        [SerializeField] private SimplePressButton leftButton;
        [SerializeField] private SimplePressButton rightButton;

        [Header("UI 顯示元件")]
        [SerializeField] private TextMeshProUGUI scoreText; // 新增分數顯示元件
        
        // 用來避免重複發送一樣的訊號，節省效能
        private Vector2 _lastSentInput = new Vector2(-999, -999); 

        protected override void Awake()
        {
            base.Awake();
            
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnMoneyChangedEvent, OnMoneyChangedEvent);
        }

        void Start()
        {
            // 隱藏游標
            GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = false });
            
            // 【關鍵步驟】: 解決先後註冊問題，立即查詢初始分數
            // 假設 GameScoreManager 已經初始化完成 (SessionSingleton 通常在場景載入時就緒)
            if (GameScoreManager.Instance != null)
            {
                UpdateScoreDisplay(GameScoreManager.Instance.CurrentMoney); // 假設您在 GameScoreManager 中暴露了 CurrentMoney
            }
        }

        void Update()
        {
            HandleMovementLogic();
            UpdateScoreDisplay(GameScoreManager.Instance.CurrentMoney);
        }

        private void HandleMovementLogic()
        {
            bool isLeft = leftButton.IsPressed;
            bool isRight = rightButton.IsPressed;

            Vector2 targetInput;

            // 1. 決定 X 軸 (Steer)
            float inputX = 0f;
            if (isLeft && !isRight)
            {
                inputX = -1f; // 左轉
            }
            else if (isRight && !isLeft)
            {
                inputX = 1f; // 右轉
            }
            // 雙按或都沒按，X 軸都是 0

            // 2. 決定 Y 軸 (Throttle/Brake)
            float inputY = 0f;
            if (isLeft && isRight)
            {
                // 【雙按】：後退 (Y=-1)
                inputY = -1f;
            }
            else
            {
                // 【單按或都沒按】：前進 (Y=1)
                inputY = 1f;
            }

            // 組合最終訊號
            targetInput = new Vector2(inputX, inputY);

            // ... (後續的發送和檢查邏輯不變)
            if (targetInput != _lastSentInput)
            {
                SendMovementEvent(targetInput);
                _lastSentInput = targetInput;
            }
        }

        private void SendMovementEvent(Vector2 input)
        {
            // 這裡使用你 InputManager 裡定義的事件名稱：MovementKeyPressedEvent
            GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() 
            { 
                MoveInput = input 
            });
        }
        
        private void OnMoneyChangedEvent(MoneyChangedEvent cmd)
        {
            // 事件觸發時，更新 UI 顯示
            //Debug.Log("錢幣更新資訊");
            //UpdateScoreDisplay(cmd.CurrentTotalMoney);
        }

        private void UpdateScoreDisplay(int newMoney)
        {
            if (scoreText != null)
            {
                // 使用格式化字串顯示金錢，例如加上 "G" 或 "$", 並可加千分位符號
                // 這裡使用標準格式 {0:N0} 表示帶有千分位分隔符號的數字
                scoreText.text = $" {newMoney:N0} G"; 
                
                // 💡 可以添加動畫效果，例如放大或變色來強調分數變動。
            }
        }
        
        // 當 UI 被關閉時，為了安全起見，發送歸零訊號 (或是你可以選擇繼續跑)
        private void OnDisable()
        {
            // 移除訂閱，避免物件銷毀後，事件發出導致的錯誤
            GameManager.Instance.MainGameEvent.Unsubscribe<MoneyChangedEvent>(OnMoneyChangedEvent);
            
             // 這裡視你的需求而定。
             // 如果關閉 UI 車子要停，就傳 Vector2.zero。
             // 如果關閉 UI 車子要繼續往前跑，就不用傳。
             // SendMovementEvent(Vector2.zero); 
        }
    }
}