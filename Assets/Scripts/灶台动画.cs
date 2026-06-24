using UnityEngine;

/// <summary>
/// 灶台视觉管理 —— MVP 阶段仅做静态精灵渲染。
/// 未来可扩展为玩家靠近时火焰动画、锅冒热气等。
/// </summary>
public class 灶台动画 : MonoBehaviour
{
    [Header("精灵")]
    [SerializeField] private Sprite stoveSprite;

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && stoveSprite != null)
        {
            sr.sprite = stoveSprite;
        }
    }
}
