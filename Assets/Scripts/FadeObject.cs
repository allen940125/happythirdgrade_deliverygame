using UnityEngine;
using System.Collections.Generic;

public class FadeObject : MonoBehaviour
{
    // 儲存原始的 Material 和 Renderers
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
    private Renderer[] renderers;

    [Header("透明度設定")]
    [Tooltip("變透明的目標值 (0.0 = 完全隱藏)")]
    public float targetTransparency = 0.2f;
    public float fadeSpeed = 5f;

    private bool isFading = false;
    private float currentAlpha = 1.0f;
    
    // 關鍵優化：追蹤當前的渲染模式
    private bool isCurrentlyTransparent = false;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            // 複製一份材質 (重要!)
            r.material = new Material(r.material);
            originalMaterials.Add(r, r.material);
        }
        
        // 初始狀態為不透明
        UpdateMaterialAlpha(1.0f);
    }

    // [Update() 已經被移除]

    /// <summary>
    /// 由管理器呼叫，執行一幀的淡入淡出邏輯
    /// </summary>
    /// <returns>如果還在 Fading 中，返回 true；如果已達到目標 Alpha，返回 false</returns>
    public bool DoFadeUpdate(float deltaTime)
    {
        float targetAlpha = isFading ? targetTransparency : 1.0f;

        // 如果已經達到目標，就不用更新了
        if (Mathf.Approximately(currentAlpha, targetAlpha))
        {
            return false;
        }

        // 執行 Lerp
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, deltaTime * fadeSpeed);
        
        // 應用新的 Alpha 值
        UpdateMaterialAlpha(currentAlpha);
        
        // 檢查是否已非常接近目標值
        if (Mathf.Abs(currentAlpha - targetAlpha) < 0.01f)
        {
            currentAlpha = targetAlpha;
            UpdateMaterialAlpha(currentAlpha); // 最後再設定一次確保精確
            return false; // 停止更新
        }

        return true; // 需要繼續更新
    }

    private void UpdateMaterialAlpha(float alpha)
    {
        // URP Lit Shader 的基礎顏色屬性名稱
        const string URP_COLOR_PROPERTY = "_BaseColor"; 

        // 決定是否需要切換渲染模式
        bool needsTransparentMode = alpha < 1.0f;
        
        // *** 核心優化 ***
        // 只有在 "需要切換" 的時候才呼叫 SetMaterialMode
        if (needsTransparentMode && !isCurrentlyTransparent)
        {
            // Opaque -> Transparent
            SetAllMaterialsMode(true);
            isCurrentlyTransparent = true;
        }
        else if (!needsTransparentMode && isCurrentlyTransparent)
        {
            // Transparent -> Opaque
            SetAllMaterialsMode(false);
            isCurrentlyTransparent = false;
        }

        // 只更新顏色屬性 (這比較便宜)
        foreach (var pair in originalMaterials)
        {
            Material mat = pair.Value;
            if (mat.HasProperty(URP_COLOR_PROPERTY))
            {
                Color color = mat.GetColor(URP_COLOR_PROPERTY);
                color.a = alpha;
                mat.SetColor(URP_COLOR_PROPERTY, color);
            }
        }
    }

    // 外部呼叫：開始淡化
    public void StartFade()
    {
        isFading = true;
    }

    // 外部呼叫：停止淡化 (恢復不透明)
    public void StopFade()
    {
        isFading = false;
    }
    
    // 將 SetMaterialMode 邏輯抽出來，一次設定所有材質
    private void SetAllMaterialsMode(bool transparent)
    {
        foreach (var pair in originalMaterials)
        {
            SetMaterialMode(pair.Value, transparent);
        }
    }

    // 處理 URP Lit Shader 的渲染模式切換
    private void SetMaterialMode(Material material, bool transparent)
    {
        const string SURFACE_MODE_PROPERTY = "_Surface";
    
        if (transparent)
        {
            material.SetFloat(SURFACE_MODE_PROPERTY, 1f); // 1 = Transparent
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_Blend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            material.SetFloat(SURFACE_MODE_PROPERTY, 0f); // 0 = Opaque
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetInt("_Blend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
    
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }
}