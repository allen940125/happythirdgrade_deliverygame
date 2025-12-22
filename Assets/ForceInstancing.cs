using UnityEngine;

[ExecuteAlways]
public class ForceGPUInstancing : MonoBehaviour
{
    // 移除那個有 race condition 的 static flag（根本不需要）

#if UNITY_EDITOR
    private void Reset() => ApplyInstancing();        // Prefab 第一次加上的時候也觸發
    private void OnValidate() => ApplyInstancing();   // 設計師改任何數值都立刻生效
#endif

    private void Awake()
    {
        if (Application.isPlaying)
        {
            // 關鍵：延遲到第一幀結束後再執行，保證材質都準備好了
            Invoke(nameof(ApplyInstancing), 0f);
        }
    }

    [ContextMenu("Force Apply Instancing")]
    private void ApplyInstancing()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;

            // 這兩行是最保險的寫法
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);   // 先拿當前的
            r.SetPropertyBlock(mpb);   // 再塞回去 → 強制開啟 Instancing 路徑
        }
    }
}