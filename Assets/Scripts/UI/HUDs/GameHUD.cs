using System.Collections;
using Game.Audio;
using Gamemanager;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class GameHUD : BasePanel 
    {
        [Header("--- 狀態群組 (請將UI拖入對應父物件) ---")]
        [SerializeField] private GameObject mainMenuGroup;
        [SerializeField] private GameObject gameplayGroup;

        [Header("--- 主選單按鈕 ---")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button storeButton;
        [SerializeField] private Button upgradeButton; // 車子升級按鈕
        [SerializeField] private Button configButton;

        [Header("--- 遊戲中控制 ---")]
        [SerializeField] private SimplePressButton leftButton;
        [SerializeField] private SimplePressButton rightButton;

        [Header("--- 遊戲中顯示 ---")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI targetScoreText;
        [SerializeField] private CanvasGroup characterHurtCG; 
        [SerializeField] private AudioData characterHurt; 

        [Header("效果設定")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("--- 升級系統設定 ---")]
        [SerializeField] private int maxCarLevel = 10;   // 最高等級
        [SerializeField] private int upgradeBaseCost = 500; // 基礎升級費用
        
        // 注意：實務上 currentCarLevel 應該要存檔 (SaveManager)，這裡先暫存在變數中
        private int _currentCarLevel = 1; 

        // 內部變數
        private Vector2 _lastSentInput = new Vector2(-999, -999); 
        private Coroutine _hurtCoroutine;
        private bool isGameRunning = false;

        protected override void Awake()
        {
            base.Awake();
            
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnMoneyChangedEvent, OnMoneyChangedEvent);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPlayerHurtPressedEvent, HandleHurtEvent);
        }

        void Start()
        {
            // 初始化按鈕監聽
            if(startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);
            
            // 其他按鈕保持原樣
            if(storeButton)     storeButton.onClick.AddListener(() => GameManager.Instance.UIManager.OpenPanel<StoreMenu>(UIType.StoreMenu));
            if(configButton)    configButton.onClick.AddListener(() => GameManager.Instance.UIManager.OpenPanel<StoreMenu>(UIType.StoreMenu));

            // ★★★ 修改這裡：升級按鈕 改為執行升級邏輯 ★★★
            if(upgradeButton)   upgradeButton.onClick.AddListener(OnUpgradeClicked);

            ShowMainMenu(true);
            
            GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = true });
            
            if(characterHurtCG != null) 
            {
                characterHurtCG.alpha = 0;
                characterHurtCG.gameObject.SetActive(false);
            }

            // 更新初始顯示
             UpdateScoreDisplayFromManager();
        }

        void Update()
        {
            if (isGameRunning)
            {
                HandleMovementLogic();
                // 持續更新分數顯示 (如果有 GameScoreManager)
                UpdateScoreDisplayFromManager();
            }
        }

        // ──────────────────────────────────────────────
        //  ★ 新增：車子升級邏輯
        // ──────────────────────────────────────────────
        private void OnUpgradeClicked()
        {
            // 1. 檢查是否已達最高等級
            if (_currentCarLevel >= maxCarLevel)
            {
                Debug.Log($"<color=yellow>升級失敗：已達到最高等級 ({maxCarLevel})</color>");
                return;
            }

            // 2. 計算升級費用 (這裡範例：費用 = 基礎費用 * 當前等級，等級越高越貴)
            // 你也可以改成固定費用，直接寫 int cost = upgradeBaseCost;
            int cost = upgradeBaseCost * _currentCarLevel; 

            // 3. 取得玩家目前持有的金錢 (ID 100)
            // InventoryManager.GetInventoryData 會確保回傳物件，即使沒錢也會回傳 quantity = 0
            InventoryItemRuntimeData moneyData = InventoryManager.Instance.GetInventoryData(100);
            int currentMoney = moneyData.quantity;

            // 4. 判斷錢夠不夠
            if (currentMoney >= cost)
            {
                // A. 扣除金錢
                InventoryManager.Instance.RemoveItem(100, cost);

                // B. 提升等級
                _currentCarLevel++;

                Debug.Log($"<color=green>升級成功！</color> 目前等級: {_currentCarLevel}, 花費: {cost}");

                // C. 如果需要同步更新 UI (因為 Update 裡通常只在遊戲進行時跑，所以在選單也要手動刷一次)
                UpdateScoreDisplayFromManager(); 
                
                // TODO: 記得在這裡呼叫存檔功能，例如 SaveManager.Instance.Save();
            }
            else
            {
                Debug.Log($"<color=red>金錢不足！</color> 需要: {cost}, 擁有: {currentMoney}");
            }
        }

        // ──────────────────────────────────────────────
        //  UI 狀態切換邏輯
        // ──────────────────────────────────────────────

        private void ShowMainMenu(bool show)
        {
            isGameRunning = !show; 
            if(mainMenuGroup) mainMenuGroup.SetActive(show);
            if(gameplayGroup) gameplayGroup.SetActive(!show);
            GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = show });
        }

        private void OnStartGameClicked()
        {
            Debug.Log("遊戲開始！");
            ShowMainMenu(false);
            GameManager.Instance.SetAllCarsDrivable(true);
            GameManager.Instance.MainGameEvent.Send(new GameStartedEvent());
            
            mainMenuGroup.SetActive(false);
        }

        // ──────────────────────────────────────────────
        //  既有的遊戲邏輯
        // ──────────────────────────────────────────────

        private void HandleMovementLogic()
        {
            if (leftButton == null || rightButton == null) return;

            bool isLeft = leftButton.IsPressed;
            bool isRight = rightButton.IsPressed;
            Vector2 targetInput;

            float inputX = 0f;
            if (isLeft && !isRight) inputX = -1f;
            else if (isRight && !isLeft) inputX = 1f;

            float inputY = 0f;
            if (isLeft && isRight) inputY = -1f;
            else inputY = 1f; 

            targetInput = new Vector2(inputX, inputY);

            if (targetInput != _lastSentInput)
            {
                SendMovementEvent(targetInput);
                _lastSentInput = targetInput;
            }
        }

        private void HandleHurtEvent(PlayerHurtPressedEvent cmd)
        {
            PlayHurtSound();
            if (_hurtCoroutine != null) StopCoroutine(_hurtCoroutine);
            _hurtCoroutine = StartCoroutine(FadeHurtEffect());
        }

        private IEnumerator FadeHurtEffect()
        {
            characterHurtCG.gameObject.SetActive(true);
            float halfDuration = fadeDuration / 2;
            float timer = 0f;

            // 淡入
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                characterHurtCG.alpha = Mathf.Lerp(0f, 1f, timer / halfDuration);
                yield return null; 
            }
            characterHurtCG.alpha = 1f;

            // 淡出
            timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                characterHurtCG.alpha = Mathf.Lerp(1f, 0f, timer / halfDuration);
                yield return null;
            }

            characterHurtCG.alpha = 0f;
            characterHurtCG.gameObject.SetActive(false);
        }

        private void PlayHurtSound()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(characterHurt); 
        }

        private void SendMovementEvent(Vector2 input)
        {
            GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() { MoveInput = input });
        }
        
        private void OnMoneyChangedEvent(MoneyChangedEvent cmd)
        {
             // 這裡可以選擇性更新
             // UpdateScoreDisplay(cmd.CurrentTotalMoney);
        }
        
        // 輔助函式：從 InventoryManager 或 ScoreManager 讀取分數並更新 UI
        private void UpdateScoreDisplayFromManager()
        {
            // 如果你的分數是以 Inventory ID 100 為準，就用這行：
            if (InventoryManager.Instance != null)
            {
                int money = InventoryManager.Instance.GetInventoryData(100).quantity;
                UpdateScoreDisplay(money);
            }
            // 如果是以 GameScoreManager 為準，請保留你原本的寫法：
            // if (GameScoreManager.Instance != null) UpdateScoreDisplay(GameScoreManager.Instance.CurrentMoney);
            
            if (GameQuestManager.Instance != null) UpdateTargetScoreDisplay(GameQuestManager.Instance.GetCurrentActiveQuest().targetValue);
        }
        
        private void UpdateScoreDisplay(int newMoney)
        {
            if (scoreText != null) scoreText.text = $" {newMoney:N0} G"; 
        }
        
        private void UpdateTargetScoreDisplay(int newMoney)
        {
            if (targetScoreText != null) targetScoreText.text = $" {newMoney:N0} G"; 
        }
        
        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.MainGameEvent.Unsubscribe<MoneyChangedEvent>(OnMoneyChangedEvent);
                GameManager.Instance.MainGameEvent.Unsubscribe<PlayerHurtPressedEvent>(HandleHurtEvent);
            }
        }
    }
}