using UnityEngine;
using System.Collections.Generic;

public class FadeObject : MonoBehaviour
{
    // 儲存原始的 Material 和 Renderers，方便還原
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
    private Renderer[] renderers;

    [Header("透明度設定")]
    [Tooltip("變透明的目標值 (0.0 = 完全隱藏)")]
    public float targetTransparency = 0.2f;
    public float fadeSpeed = 5f;

    private bool isFading = false;
    private float currentAlpha = 1.0f;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            // 複製一份材質 (重要!)，避免修改到共享的 Material Asset
            r.material = new Material(r.material);
            originalMaterials.Add(r, r.material);
        }
    }

    void Update()
    {
        // 根據是否需要淡化來調整透明度
        if (isFading)
        {
            currentAlpha = Mathf.Lerp(currentAlpha, targetTransparency, Time.deltaTime * fadeSpeed);
        }
        else
        {
            currentAlpha = Mathf.Lerp(currentAlpha, 1.0f, Time.deltaTime * fadeSpeed);
        }

        // 應用新的 Alpha 值
        UpdateMaterialAlpha();
    }

    private void UpdateMaterialAlpha()
    {
        // URP Lit Shader 的基礎顏色屬性名稱固定為 _BaseColor
        const string URP_COLOR_PROPERTY = "_BaseColor"; 

        foreach (var pair in originalMaterials)
        {
            Material mat = pair.Value;
        
            // 確保材質是 URP Lit 或支援 _BaseColor
            if (mat.HasProperty(URP_COLOR_PROPERTY))
            {
                Color color = mat.GetColor(URP_COLOR_PROPERTY);
                color.a = currentAlpha;
                mat.SetColor(URP_COLOR_PROPERTY, color);

                // 處理渲染模式切換 (必須，透明度才能生效)
                if (currentAlpha < 1.0f)
                {
                    // 進入透明模式 (RenderQueue = Transparent)
                    SetMaterialMode(mat, true);
                }
                else
                {
                    // 恢復不透明模式 (RenderQueue = Opaque)
                    SetMaterialMode(mat, false);
                }
            }
            else
            {
                // 理論上，如果所有材質都轉換為 URP Lit，這裡不應觸發
                Debug.LogWarning($"Shader '{mat.shader.name}' 缺少 URP 標準屬性 '{URP_COLOR_PROPERTY}'。請確認是否已完全轉換為 URP Lit。");
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
    
    // 處理標準 Unity Shaders 的渲染模式切換
    // FadeObject.cs

    private void SetMaterialMode(Material material, bool transparent)
    {
        // URP Lit Shader 使用 _Surface 屬性來控制渲染模式 (0=Opaque, 1=Transparent)
        const string SURFACE_MODE_PROPERTY = "_Surface";
    
        if (transparent)
        {
            // 設置為 Transparent Mode
            material.SetFloat(SURFACE_MODE_PROPERTY, 1f); // 1 = Transparent
            material.SetOverrideTag("RenderType", "Transparent");
        
            // 設置混合模式：SrcAlpha OneMinusSrcAlpha
            material.SetInt("_Blend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0); // 關閉深度寫入
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            // 設置為 Opaque Mode
            material.SetFloat(SURFACE_MODE_PROPERTY, 0f); // 0 = Opaque
            material.SetOverrideTag("RenderType", "Opaque");
        
            // 設置混合模式：Opaque
            material.SetInt("_Blend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1); // 開啟深度寫入
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
    
        // 刷新材質關鍵字以確保渲染管線正確處理
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }
}