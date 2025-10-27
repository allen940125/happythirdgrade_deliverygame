using System.Collections.Generic;
using Gamemanager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 簡單單檔版：
    /// - 每個按鈕設 direction (例如 Up = (0,1))
    /// - 按下時把 direction 加入全域 pressed set、放開或離開時移除
    /// - 每次集合變動時計算合併向量並發送 MovementKeyPressedEvent
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MovementButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("此按鈕代表的方向 (例: 上 = 0,1)")]
        public Vector2 direction = Vector2.zero;

        [Tooltip("若 true，按住時每 FixedUpdate 會再次發送 (預設 false)")]
        public bool repeatWhileHeld = false;

        // 全域被按下的 direction 集合（靜態，跨所有 MovementButton）
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
        private static Vector2 _lastSent = Vector2.positiveInfinity;
        private void SendCombinedIfChanged(bool forceSend = false)
        {
            Vector2 combined = Vector2.zero;
            foreach (var v in pressed) combined += v;

            // 若你不想在對角線變得更快，請改用 normalized：
            // if (combined.magnitude > 1f) combined = combined.normalized;

            if (forceSend || !ApproximatelyEqual(combined, _lastSent))
            {
                _lastSent = combined;
                // 發送事件
                GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() { MoveInput = combined });
                Debug.Log($"[MovementButton] Sent MoveInput {combined}");
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
