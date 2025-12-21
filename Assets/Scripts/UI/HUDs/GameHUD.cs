using System.Collections;
using Game.Audio;
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
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI targetScoreText;
        
        [SerializeField] private CanvasGroup characterHurtCG; 
        [SerializeField] private AudioData characterHurt; 
        
        [Header("效果設定")]
        [SerializeField] private float fadeDuration = 0.5f;

        private Vector2 _lastSentInput = new Vector2(-999, -999); 
        private Coroutine _hurtCoroutine;

        protected override void Awake()
        {
            base.Awake();
            
            // --- [Debug 1] 確認 Awake 有被執行 ---
            Debug.Log($"[GameHUD] Awake 被執行 (GameObject: {gameObject.name})");

            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnMoneyChangedEvent, OnMoneyChangedEvent);
            
            // --- [Debug 2] 確認註冊程式碼有跑到 ---
            Debug.Log("[GameHUD] 正在嘗試訂閱 OnPlayerHurtPressedEvent...");
            
            // 請確認這裡的語法是否正確，有些框架是 Subscribe<T> 而不是 SetSubscribe
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPlayerHurtPressedEvent, HandleHurtEvent);
        }

        void Start()
        {
            GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = false });
            
            if(characterHurtCG != null) 
            {
                characterHurtCG.alpha = 0;
                characterHurtCG.gameObject.SetActive(false);
            }

            if (GameScoreManager.Instance != null)
            {
                UpdateScoreDisplay(GameScoreManager.Instance.CurrentMoney);
                UpdateTargetScoreDisplay(GameQuestManager.Instance.GetCurrentActiveQuest().targetValue);
            }
        }

        void Update()
        {
            HandleMovementLogic();
            UpdateScoreDisplay(GameScoreManager.Instance.CurrentMoney);
            UpdateTargetScoreDisplay(GameQuestManager.Instance.GetCurrentActiveQuest().targetValue);
        }

        // 事件觸發的方法
        private void HandleHurtEvent(PlayerHurtPressedEvent cmd)
        {
            // --- [Debug 3] 確認收到訊號 ---
            Debug.Log($"[GameHUD] <color=red>收到受傷訊號!</color> 時間: {Time.time}");
            
            PlayHurtSound();

            if (_hurtCoroutine != null) StopCoroutine(_hurtCoroutine);
            _hurtCoroutine = StartCoroutine(FadeHurtEffect());
        }

        private IEnumerator FadeHurtEffect()
        {
            // --- [Debug 4] 確認協程開始跑 ---
            Debug.Log("[GameHUD] 開始執行淡入淡出協程");
            
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
            
             // --- [Debug 5] 確認協程結束 ---
             Debug.Log("[GameHUD] 淡入淡出結束");
        }

        private void PlayHurtSound()
        {
            if (AudioManager.Instance != null)
            {
                Debug.Log("[GameHUD] 呼叫播放音效");
                AudioManager.Instance.PlaySFX(characterHurt); 
            }
            else
            {
                Debug.LogError("[GameHUD] 找不到 AudioManager!");
            }
        }

        // ... (中間省略 HandleMovementLogic, SendMovementEvent, OnMoneyChangedEvent 等不變的代碼) ...
        
        private void HandleMovementLogic()
        {
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
        
        private void SendMovementEvent(Vector2 input)
        {
            GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() 
            { 
                MoveInput = input 
            });
        }
        
        private void OnMoneyChangedEvent(MoneyChangedEvent cmd)
        {
             //UpdateScoreDisplay(cmd.CurrentTotalMoney);
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
            // --- [Debug 6] 確認是否提早被取消註冊 ---
            Debug.Log($"[GameHUD] OnDisable 被呼叫，取消訂閱事件。 (GameObject: {gameObject.name})");

            // GameManager.Instance.MainGameEvent.Unsubscribe<MoneyChangedEvent>(OnMoneyChangedEvent);
            // GameManager.Instance.MainGameEvent.Unsubscribe<PlayerHurtPressedEvent>(HandleHurtEvent);
        }
    }
}