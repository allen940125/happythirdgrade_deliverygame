using Gamemanager;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    /// <summary>
    /// 綁在方向按鈕上：按下時立刻發送 MovementKeyPressedEvent（帶 direction），放開或游標移出時發 (0,0)。
    /// 支援 Inspector 設定 direction。
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class MovementButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("設定此按鈕代表的移動方向 (例如 上 = (0,1) )")]
        public Vector2 direction = Vector2.zero;

        [Tooltip("是否在按下時仍持續發送（若 true，會每 FixedUpdate 發一次）。通常不需要，按下即發一次即可。")]
        public bool repeatWhileHeld = false;

        private bool _isHeld = false;

        void OnDisable()
        {
            // 如果物件被關掉時還在按著，保險起見把 input 歸零
            if (_isHeld)
            {
                SendZero();
                _isHeld = false;
            }
        }

        void Update()
        {
            // optional: 若你想在按住時每 Frame 發送，可在這處處理（目前沒開）
            if (repeatWhileHeld && _isHeld)
            {
                SendMovement(direction);
            }
        }

        void FixedUpdate()
        {
            // 如果你想每 FixedUpdate 發送（物理同步），把 repeatWhileHeld 設 true 並使用這裡。
            // 但通常按下即發一次已足夠。
            if (repeatWhileHeld && _isHeld)
            {
                SendMovement(direction);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isHeld = true;
            SendMovement(direction);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isHeld = false;
            SendZero();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 若游標離開按鈕區域，視為放開
            if (_isHeld)
            {
                _isHeld = false;
                SendZero();
            }
        }

        private void SendMovement(Vector2 dir)
        {
            // 傳遞 MovementKeyPressedEvent
            GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() { MoveInput = dir });
            Debug.Log($"[MovementButton] Send Movement: {dir} ({gameObject.name})");
        }

        private void SendZero()
        {
            GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() { MoveInput = Vector2.zero });
            Debug.Log($"[MovementButton] Send Movement: (0,0) release ({gameObject.name})");
        }
    }
}
