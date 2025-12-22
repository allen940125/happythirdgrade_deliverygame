using UnityEngine;
using System.Collections.Generic;

public class FadeObject : MonoBehaviour
{
    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    
    // 【注意】請確認你的 Shader Graph 裡的 Reference 名稱
    // 如果你上一動作改成 _Opacity 了，這裡要改成 "_Opacity"
    // 如果還是 _Cutoff，就維持 _Cutoff
    private static readonly int CutoffPropID = Shader.PropertyToID("_Opacity"); 

    [Header("透明度設定")]
    [Tooltip("依照你的 Shader: 0 = 消失, -1 = 顯示。建議設為 -0.1 或 -0.2 (半透明)")]
    public float targetValue = -0.1f; // 改名比較直覺，這是「遮擋時」的數值
    public float fadeSpeed = 5f;

    private bool isFading = false;
    
    // 【修改點 1】初始值設為 -1 (完全顯示)
    private float currentValue = -1.0f; 

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        
        // 一開始先強制更新一次，確保它顯示出來
        UpdateMaterial(-1.0f);
    }

    public bool DoFadeUpdate(float deltaTime)
    {
        // 【修改點 2】邏輯修正
        // 如果 isFading (遮擋中) -> 往 targetValue (接近0) 跑
        // 如果沒遮擋 -> 往 -1.0f (完全顯示) 跑
        float target = isFading ? targetValue : -1.0f;

        if (Mathf.Approximately(currentValue, target))
            return false;

        currentValue = Mathf.MoveTowards(currentValue, target, deltaTime * fadeSpeed);

        UpdateMaterial(currentValue);

        return true;
    }

    private void UpdateMaterial(float value)
    {
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetFloat(CutoffPropID, value); // 設定新的值
            r.SetPropertyBlock(propBlock);
        }
    }

    public void StartFade() => isFading = true;
    public void StopFade() => isFading = false;
}