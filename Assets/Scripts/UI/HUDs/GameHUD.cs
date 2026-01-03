using System.Collections;
using System.Collections.Generic; // 用於 List 或 Array
using Game.Audio;
using Gamemanager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public class GameHUD : BasePanel 
    {
        #region === 定義角色特效結構 ===
        [System.Serializable] // 讓這個結構可以在 Inspector 顯示
        public class CharacterEffectData
        {
            public string name = "角色名稱"; // 方便你在 Inspector 辨識
            public CanvasGroup hurtCG;      // 這個角色的受傷紅光
            public AudioData hurtAudio;     // 這個角色的受傷叫聲
        }
        #endregion

        [Header("--- 狀態群組 ---")]
        [SerializeField] private GameObject mainMenuGroup;
        [SerializeField] private GameObject gameplayGroup;

        [Header("--- 主選單按鈕 ---")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button storeButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button configButton;

        [Header("--- 遊戲中控制 ---")]
        [SerializeField] private SimplePressButton leftButton;
        [SerializeField] private SimplePressButton rightButton;

        [Header("--- 遊戲中顯示 ---")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI targetScoreText;

        // ★★★ 修改處：把原本單一變數拿掉，換成陣列 ★★★
        [Header("--- ★★★ 角色受傷設定 (3個角色) ★★★ ---")]
        [Tooltip("請設定 Size = 3，分別對應 ID 1, 2, 3")]
        [SerializeField] private CharacterEffectData[] characterEffects; 

        [Header("效果設定")]
        [SerializeField] private float fadeDuration = 0.5f;

        private Vector2 _lastSentInput = new Vector2(-999, -999); 
        private Coroutine _hurtCoroutine;
        private bool isGameRunning = false;

        protected override void Awake()
        {
            base.Awake();
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnMoneyChangedEvent, OnMoneyChangedEvent);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPlayerHurtPressedEvent, HandleHurtEvent);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ReturnToMainMenu();
            Debug.Log("HUD家仔");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.MainGameEvent.Unsubscribe<MoneyChangedEvent>(OnMoneyChangedEvent);
                GameManager.Instance.MainGameEvent.Unsubscribe<PlayerHurtPressedEvent>(HandleHurtEvent);
            }
            Debug.Log("HUD謝仔");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"場景 {scene.name} 加載完成，HUD 強制返回主選單");
            ReturnToMainMenu();
        }

        void Start()
        {
            if(startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);
            if(storeButton)     storeButton.onClick.AddListener(() => GameManager.Instance.UIManager.OpenPanel<StoreMenu>(UIType.StoreMenu));
            if(configButton)    configButton.onClick.AddListener(() => GameManager.Instance.UIManager.OpenPanel<StoreMenu>(UIType.BagMenu));

            ShowMainMenu(true);
            
            // ★ 初始化：把所有角色的受傷圖都隱藏
            ResetAllHurtEffects();

             UpdateScoreDisplayFromManager();
        }

        void Update()
        {
            if (isGameRunning)
            {
                HandleMovementLogic();
                UpdateScoreDisplayFromManager();
            }
        }

        private void ShowMainMenu(bool show)
        {
            isGameRunning = !show; 
            
            if(mainMenuGroup) mainMenuGroup.SetActive(show);
            if(gameplayGroup) gameplayGroup.SetActive(!show);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = show });
            }
        }

        private void OnStartGameClicked()
        {
            if (mainMenuGroup == null) { Debug.LogError("錯誤：Inspector 中的 'Main Menu Group' 忘記拉了！"); return; }

            ShowMainMenu(false); 
    
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetAllCarsDrivable(true);
                GameManager.Instance.MainGameEvent.Send(new GameStartedEvent());
            }
        }

        public void ReturnToMainMenu()
        {
            ShowMainMenu(true);

            // ★ 重置時，停止協程並隱藏所有特效
            if (_hurtCoroutine != null) StopCoroutine(_hurtCoroutine);
            ResetAllHurtEffects();
            
            SendMovementEvent(Vector2.zero); 
        }

        // ★ 新增：隱藏所有受傷圖的輔助函式
        private void ResetAllHurtEffects()
        {
            if (characterEffects == null) return;
            foreach (var effect in characterEffects)
            {
                if (effect.hurtCG != null)
                {
                    effect.hurtCG.alpha = 0;
                    effect.hurtCG.gameObject.SetActive(false);
                }
            }
        }

        private void HandleMovementLogic()
        {
            if (leftButton == null || rightButton == null) return;
            bool isLeft = leftButton.IsPressed;
            bool isRight = rightButton.IsPressed;
            float inputX = 0f;
            if (isLeft && !isRight) inputX = -1f;
            else if (isRight && !isLeft) inputX = 1f;
            float inputY = (isLeft && isRight) ? -1f : 1f; 
            Vector2 targetInput = new Vector2(inputX, inputY);
            if (targetInput != _lastSentInput)
            {
                SendMovementEvent(targetInput);
                _lastSentInput = targetInput;
            }
        }

        // ==========================================
        // ★★★ 修改處：根據角色 ID 播放對應特效 ★★★
        // ==========================================
        private void HandleHurtEvent(PlayerHurtPressedEvent cmd)
        {
            // 1. 取得當前角色 ID (假設是 1, 2, 3)
            int charId = GameManager.Instance.CurrentCharID;
            
            // 2. 轉換成陣列索引 (Index = ID - 1)
            int index = charId - 1;

            // 3. 防呆檢查
            if (characterEffects == null || index < 0 || index >= characterEffects.Length)
            {
                Debug.LogWarning($"找不到角色 ID {charId} 的特效設定！請檢查 HUD 的 Character Effects 陣列大小。");
                return;
            }

            // 4. 取得對應的資料
            CharacterEffectData currentEffect = characterEffects[index];

            // 5. 播放該角色的音效
            if (AudioManager.Instance != null && currentEffect.hurtAudio != null) 
            {
                AudioManager.Instance.PlaySFX(currentEffect.hurtAudio);
            }

            // 6. 播放該角色的受傷圖動畫
            if (_hurtCoroutine != null) StopCoroutine(_hurtCoroutine);
            
            // 這裡傳入這個角色專屬的 CanvasGroup
            if (currentEffect.hurtCG != null)
            {
                _hurtCoroutine = StartCoroutine(FadeHurtEffect(currentEffect.hurtCG));
            }
        }

        // ★ 修改協程：接收參數 (CanvasGroup targetCG)
        private IEnumerator FadeHurtEffect(CanvasGroup targetCG)
        {
            targetCG.gameObject.SetActive(true);
            float halfDuration = fadeDuration / 2;
            float timer = 0f;
            
            // 淡入
            while (timer < halfDuration) 
            { 
                timer += Time.deltaTime; 
                targetCG.alpha = Mathf.Lerp(0f, 1f, timer / halfDuration); 
                yield return null; 
            }
            targetCG.alpha = 1f;
            
            // 淡出
            timer = 0f;
            while (timer < halfDuration) 
            { 
                timer += Time.deltaTime; 
                targetCG.alpha = Mathf.Lerp(1f, 0f, timer / halfDuration); 
                yield return null; 
            }
            
            targetCG.alpha = 0f;
            targetCG.gameObject.SetActive(false);
        }

        private void SendMovementEvent(Vector2 input)
        {
            if(GameManager.Instance != null)
                GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() { MoveInput = input });
        }
        
        private void OnMoneyChangedEvent(MoneyChangedEvent cmd) { } 
        
        private void UpdateScoreDisplayFromManager()
        {
            if (InventoryManager.Instance != null)
            {
                int money = InventoryManager.Instance.GetInventoryData(100).quantity;
                if (scoreText != null) scoreText.text = $" {GameScoreManager.Instance.CurrentMoney:N0} G";
            }
            if (GameQuestManager.Instance != null && GameQuestManager.Instance.GetCurrentActiveQuest() != null) 
            {
                int target = GameQuestManager.Instance.GetCurrentActiveQuest().targetValue;
                if (targetScoreText != null) targetScoreText.text = $" {target:N0} G";
            }
        }
    }
}