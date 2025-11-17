using Gamemanager; 
using UnityEngine;

namespace Game.UI
{
    public class GameHUD : BasePanel 
    {
        [Header("按鈕參照 (請將掛有 SimplePressButton 的物件拖入)")]
        [SerializeField] private SimplePressButton leftButton;
        [SerializeField] private SimplePressButton rightButton;

        // 用來避免重複發送一樣的訊號，節省效能
        private Vector2 _lastSentInput = new Vector2(-999, -999); 

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            // 隱藏游標 (照舊)
            GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = false });
        }

        void Update()
        {
            HandleMovementLogic();
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
        
        // 當 UI 被關閉時，為了安全起見，發送歸零訊號 (或是你可以選擇繼續跑)
        private void OnDisable()
        {
             // 這裡視你的需求而定。
             // 如果關閉 UI 車子要停，就傳 Vector2.zero。
             // 如果關閉 UI 車子要繼續往前跑，就不用傳。
             // SendMovementEvent(Vector2.zero); 
        }
    }
}