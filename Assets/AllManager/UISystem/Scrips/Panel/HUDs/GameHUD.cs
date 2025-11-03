using Gamemanager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class GameHUD : BasePanel
    {
        [Header("移動按鈕 (請在 Inspector 把 MovementButton 加到這些按鈕上)")]
        [SerializeField] private Button upButton;
        [SerializeField] private Button downButton;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        // 其他 UI 欄位略...（你現有的 UI 欄位可以保留）
        protected override void Awake()
        {
            base.Awake();

            // 檢查並初始化 direction（如果你想用程式設預設）
            EnsureMovementButton(upButton, Vector2.up);
            EnsureMovementButton(downButton, Vector2.down);
            EnsureMovementButton(leftButton, Vector2.left);
            EnsureMovementButton(rightButton, Vector2.right);
        }

        void Start()
        {
            // 隱藏游標（如你原本做法）
            GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = false });
        }

        /// <summary>
        /// 若按鈕沒有 MovementButton 組件，會自動新增並指定 direction。
        /// 若已經存在，會覆寫 direction（方便快速配置）。
        /// </summary>
        private void EnsureMovementButton(Button btn, Vector2 dir)
        {
            if (btn == null) return;

            var mb = btn.GetComponent<MovementButton>();
            if (mb == null)
            {
                mb = btn.gameObject.AddComponent<MovementButton>();
            }

            mb.direction = dir;
            // mb.repeatWhileHeld = true; // 若你想按住時持續傳送，可解除註解
        }
    }
}