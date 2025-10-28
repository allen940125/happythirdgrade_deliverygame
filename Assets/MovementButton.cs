using System.Collections.Generic;
using Gamemanager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 簡單單檔版（修改後）：
    /// - Buttons 只控制 Y 軸（上下）
    /// - X 軸由其他輸入（例如 SteeringWheelController）更新到 MovementInputState.CurrentX
    /// - 本類別計算被按下的 Y 合併值，更新 MovementInputState.CurrentY 並發送 (CurrentX, CurrentY)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MovementButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("此按鈕代表的方向 (例: 上 = 0,1)")]
        public Vector2 direction = Vector2.zero;

        [Tooltip("若 true，按住時每 FixedUpdate 會再次發送 (預設 false)")]
        public bool repeatWhileHeld = false;

        // 全域被按下的 direction 集合（靜態，跨所有 MovementButton）
        // 我們仍存 Vector2，但實際上只會用到 y 分量
        private static readonly HashSet<Vector2> pressed = new HashSet<Vector2>(new Vector2EqualityComparer());
        private bool _isHeld = false;

        void OnDisable()
        {
            if (_isHeld)
            {
                _isHeld = false;
                RemoveDirectionAndSend(direction);
            }
        }

        void FixedUpdate()
        {
            if (repeatWhileHeld && _isHeld)
            {
                // 若需要每 FixedUpdate 持續發送，可以打開此選項
                SendCombinedIfChanged(forceSend: true);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isHeld = true;
            AddDirectionAndSend(direction);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isHeld = false;
            RemoveDirectionAndSend(direction);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 游標移出視同放開（若仍被視為按下，則移除）
            if (_isHeld)
            {
                _isHeld = false;
                RemoveDirectionAndSend(direction);
            }
        }

        // Add direction to set and send event if combined changed
        private void AddDirectionAndSend(Vector2 dir)
        {
            var d = NormalizeAxis(dir);
            if (!pressed.Contains(d))
            {
                pressed.Add(d);
                SendCombinedIfChanged();
            }
        }

        // Remove direction from set and send
        private void RemoveDirectionAndSend(Vector2 dir)
        {
            var d = NormalizeAxis(dir);
            if (pressed.Remove(d))
            {
                SendCombinedIfChanged();
            }
        }

        // 計算集合合併向量並發送（只有變化才發送）
        private static Vector2 _lastSent = new Vector2(float.NaN, float.NaN);
        private void SendCombinedIfChanged(bool forceSend = false)
        {
            // 只累加 Y 分量（X 由 SteeringWheel 或其他輸入管理）
            float combinedY = 0f;
            foreach (var v in pressed) combinedY += v.y;

            // clamp Y 到 [-1,1]
            combinedY = Mathf.Clamp(combinedY, -1f, 1f);

            // 更新全域 Y（供其他系統查詢）
            MovementInputState.Y = combinedY;

            // 讀取目前的 X（可能由 SteeringWheelController 或其他地方更新）
            float currentX = MovementInputState.X;

            Vector2 toSend = new Vector2(currentX, combinedY);

            if (forceSend || !ApproximatelyEqual(toSend, _lastSent))
            {
                _lastSent = toSend;
                // 發送事件
                
                //GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() { MoveInput = toSend });
                Debug.Log($"[MovementButton] Sent MoveInput {toSend}");
            }
        }

        // 小工具：把方向壓到 {-1,0,1} 分量
        private Vector2 NormalizeAxis(Vector2 v)
        {
            float x = v.x > 0 ? 1f : (v.x < 0 ? -1f : 0f);
            float y = v.y > 0 ? 1f : (v.y < 0 ? -1f : 0f);
            return new Vector2(x, y);
        }

        private static bool ApproximatelyEqual(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }

        // comparer for HashSet
        private class Vector2EqualityComparer : IEqualityComparer<Vector2>
        {
            public bool Equals(Vector2 a, Vector2 b) => Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
            public int GetHashCode(Vector2 v) => v.x.GetHashCode() ^ (v.y.GetHashCode() << 2);
        }
    }
}
