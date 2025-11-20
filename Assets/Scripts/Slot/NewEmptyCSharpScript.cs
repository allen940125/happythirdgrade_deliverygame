using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SlotCell : MonoBehaviour
{
    // 非必要：只是方便以後你呼叫 SetSprite
    public SpriteRenderer spriteRenderer;

    void Reset()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetSprite(Sprite s)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.sprite = s;
        spriteRenderer.enabled = (s != null);
    }
}