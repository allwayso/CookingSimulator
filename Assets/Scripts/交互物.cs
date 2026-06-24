using UnityEngine;

/// <summary>
/// 可交互物体类型
/// </summary>
public enum InteractionType
{
    Fridge,
    Stove
}

/// <summary>
/// 挂载在冰箱、灶台等可交互物体上，定义触发距离、判定中心和提示文字。
/// 由 交互管理 每帧轮询来判断玩家是否在交互范围内。
/// </summary>
public class 交互物 : MonoBehaviour
{
    [Tooltip("玩家进入此距离内可触发交互")]
    public float triggerDistance = 4f;

    [Tooltip("碰撞判定中心相对于 transform.position 的偏移（世界坐标系未缩放前的本地偏移）")]
    public Vector3 bodyOffset = Vector3.zero;

    [Tooltip("玩家靠近时显示的提示文字")]
    public string promptMessage = "按F交互";

    [Tooltip("交互类型，用于区分回调")]
    public InteractionType interactionType;

    /// <summary>
    /// 获取世界坐标系下的交互判定中心。
    /// 公式与 冰箱动画.cs 的 bodyCenter 一致：
    ///   transform.position + bodyOffset * localScale
    /// </summary>
    public Vector3 GetBodyCenter()
    {
        return transform.position + new Vector3(
            bodyOffset.x * Mathf.Abs(transform.localScale.x),
            bodyOffset.y * Mathf.Abs(transform.localScale.y),
            bodyOffset.z * Mathf.Abs(transform.localScale.z));
    }

    /// <summary>
    /// 判断玩家是否在交互范围内
    /// </summary>
    public bool IsPlayerInRange(Transform player)
    {
        if (player == null) return false;
        return Vector3.Distance(GetBodyCenter(), player.position) < triggerDistance;
    }
}
