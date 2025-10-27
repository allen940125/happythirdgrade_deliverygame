using Gamemanager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 掛在方向盤 UI（或 Canvas 的一個區域）上：
/// - 手指/滑鼠按下後 horizontal delta 決定 turnInput (-1..1)
/// - 放開時回到 0
/// - 同步旋轉一個 directionWheelImage（UI 圖片）
/// - 發送 MovementKeyPressedEvent with MoveInput = new Vector2(turnInput, 0)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SteeringWheelController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI 設定")]
    [Tooltip("可視化的方向盤 Image（會轉動顯示轉向）")]
    public Image directionWheelImage;

    [Tooltip("如果想顯示一個拖動區塊（選填），把該區域拖進來")]
    public RectTransform dragArea; // 可留空，預設使用本物件的 RectTransform

    [Header("輸入設定")]
    [Tooltip("水平最大拖動量（像素），拖滿為 1 或 -1")]
    public float maxDragDistance = 200f;

    [Tooltip("旋轉角度最大值（UI 方向盤顯示角度），例如 90 表示 -90..90 度")]
    public float maxWheelRotation = 90f;

    [Tooltip("平滑回正速度（>0，數字越大回正越快）")]
    public float returnSpeed = 8f;

    [Tooltip("是否在放開後讓 wheel 漸回 0（否則立即 0）")]
    public bool smoothReturn = true;

    // 內部狀態
    private RectTransform _rect;
    private Vector2 _pointerStartLocal; // 按下起點（local pos）
    private bool _isDragging = false;
    private float _targetInput = 0f; // 目標輸入（-1..1）
    private float _currentInput = 0f; // 當前輸入（用於平滑）

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (dragArea == null) dragArea = _rect;
    }

    void Update()
    {
        // 平滑插值 current 到 target（或直接指派）
        if (smoothReturn || _isDragging)
        {
            _currentInput = Mathf.Lerp(_currentInput, _targetInput, Time.deltaTime * returnSpeed);
        }
        else
        {
            _currentInput = _targetInput;
        }

        // 更新 UI 方向盤旋轉（負號是為了把向右 input 對應為 clockwise）
        if (directionWheelImage != null)
        {
            float rot = -_currentInput * maxWheelRotation;
            directionWheelImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
        }
    }

    // 每幀或每次 target 變化時要發送事件（只有當值改變時發）
    private float _lastSent = float.NaN;
    void LateUpdate()
    {
        if (!Mathf.Approximately(_lastSent, _currentInput))
        {
            _lastSent = _currentInput;
            SendMovement(_currentInput);
        }
    }

    // IPointerDownHandler
    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dragArea, eventData.position, eventData.pressEventCamera, out _pointerStartLocal);
        // 初始 target = 0
        _targetInput = 0f;
    }

    // IDragHandler
    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dragArea, eventData.position, eventData.pressEventCamera, out localPoint);
        float deltaX = localPoint.x - _pointerStartLocal.x;

        // 計算目標輸入
        float input = Mathf.Clamp(deltaX / maxDragDistance, -1f, 1f);
        _targetInput = input;
    }

    // IPointerUpHandler
    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
        // 放開時回到 0（可平滑）
        _targetInput = 0f;
    }

    // 發送 Movement Key 事件（x 分量代表左右）
    private void SendMovement(float x)
    {
        // 你現有的 MoveInput 之前是 Vector2 (x,y) 用法：
        Vector2 move = new Vector2(x, 0f);
        GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() { MoveInput = move });
        // Debug.Log($"[SteeringWheel] Send MoveInput {move}");
    }
}
