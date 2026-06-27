using UnityEngine;

/// <summary>
/// 手动 AABB 碰撞体组件，配合人物移动使用。
/// 挂到任何需要阻挡角色移动的物体上。
/// </summary>
public class BlockObject : MonoBehaviour
{
    [Tooltip("碰撞体尺寸（本地空间，不含 scale）")]
    public Vector2 size = Vector2.one;

    [Tooltip("碰撞体中心偏移（本地空间，不含 scale）")]
    public Vector2 offset = Vector2.zero;

    public Vector2 GetWorldCenter()
    {
        float s = Mathf.Abs(transform.localScale.x);
        return (Vector2)transform.position + offset * s;
    }

    public Vector2 GetWorldSize()
    {
        float s = Mathf.Abs(transform.localScale.x);
        return size * s;
    }
}
