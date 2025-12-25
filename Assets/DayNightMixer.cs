using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class DayNightController : MonoBehaviour
{
    [Header("=== 自動循環系統 ===")]
    public bool isAutoRun = true;          // 是否開啟自動時間流逝
    public float dayDurationMinutes = 10f; // 遊戲內一天等於現實幾分鐘? (預設10分鐘)
    
    [Header("=== 時間控制 (唯讀/手動) ===")]
    [Range(0f, 1f)] public float timeOfDay = 0.0f; // 0=白天, 1=晚上

    [Header("=== APV 設定 (名稱要跟 Lighting 面板一樣) ===")]
    public string dayScenarioName = "Day";
    public string nightScenarioName = "Night"; 
    
    [Header("=== 燈光物件 ===")]
    public Light sunLight;
    public Light moonLight;

    [Header("=== 環境光與強度 ===")]
    public float maxSunIntensity = 1500f;
    public float maxMoonIntensity = 500f;
    public Color dayAmbientColor = new Color(0.6f, 0.7f, 0.8f);
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.1f);

    // 內部變數
    private ProbeReferenceVolume probeVolume;
    private float timeProgress; // 用來計時的內部變數

    void Start()
    {
        // 只在遊戲執行時運作
        if (Application.isPlaying)
        {
            // 1. 計算一整天(日->夜->日)總共需要多少秒
            float totalCycleSeconds = dayDurationMinutes * 60f;

            // 2. 隨機決定我們現在位於這一天的「第幾秒」
            // 範圍從 0 到 600秒 (假設一天10分鐘)
            timeProgress = UnityEngine.Random.Range(0f, totalCycleSeconds);

            // 3. (選用) 為了讓編輯器能馬上看到結果，我們可以手動跑一次計算
            float halfCycleSeconds = totalCycleSeconds / 2f;
            timeOfDay = Mathf.PingPong(timeProgress / halfCycleSeconds, 1f);
        }
    }

    void Update()
    {
        // 0. 處理自動時間流逝 (只在 Play 模式下運作，避免編輯器亂閃)
        if (Application.isPlaying && isAutoRun)
        {
            CalculateTime();
        }

        // 1. 執行 APV 混合
        UpdateAPV_Official();

        // 2. 執行 燈光與環境色切換
        UpdateLightsAndAmbient();
    }

    void CalculateTime()
    {
        // 我們需要 0 -> 1 (5分鐘) 然後 1 -> 0 (5分鐘)
        // 總共 10 分鐘 (600秒)
        // 所以單趟 (0到1) 需要的時間是總時間的一半
        float halfCycleSeconds = (dayDurationMinutes * 60f) / 2f;

        // 累加時間
        timeProgress += Time.deltaTime;

        // 使用 PingPong 函數：讓數值在 0 到 1 之間來回彈跳
        // 當 timeProgress = 0, 結果 = 0
        // 當 timeProgress = 300 (5分鐘), 結果 = 1
        // 當 timeProgress = 600 (10分鐘), 結果 = 0
        timeOfDay = Mathf.PingPong(timeProgress / halfCycleSeconds, 1f);
    }

    void UpdateAPV_Official()
    {
        if (probeVolume == null) probeVolume = ProbeReferenceVolume.instance;

        if (probeVolume != null)
        {
            // 防止 timeOfDay 超出 0~1 範圍 (雖然 PingPong 已經限制了，但加個保險)
            float blendFactor = Mathf.Clamp01(timeOfDay);
            probeVolume.BlendLightingScenario(nightScenarioName, blendFactor);
        }
    }

    void UpdateLightsAndAmbient()
    {
        if (sunLight != null)
        {
            sunLight.intensity = Mathf.Lerp(maxSunIntensity, 0, timeOfDay);
            sunLight.gameObject.SetActive(sunLight.intensity > 0.1f);
        }

        if (moonLight != null)
        {
            moonLight.intensity = Mathf.Lerp(0, maxMoonIntensity, timeOfDay);
            moonLight.gameObject.SetActive(moonLight.intensity > 0.1f);
        }

        if (RenderSettings.ambientMode == AmbientMode.Flat || RenderSettings.ambientMode == AmbientMode.Trilight)
        {
            RenderSettings.ambientLight = Color.Lerp(dayAmbientColor, nightAmbientColor, timeOfDay);
        }
    }
}