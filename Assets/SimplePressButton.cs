using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    public class SimplePressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool IsPressed { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 手指滑出按鈕範圍視為放開
            IsPressed = false;
        }

        // 當物件被隱藏時，重置狀態，避免卡鍵
        private void OnDisable()
        {
            IsPressed = false;
        }
    }
}