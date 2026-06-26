using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 交互系统管理器：每帧检测玩家与所有交互物的距离，
/// 控制交互提示 UI 的显示/隐藏和定位，处理 F 键触发。
/// </summary>
public class 交互管理 : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Text promptText;

    [Header("设置")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    [Header("提示UI偏移")]
    [SerializeField] private Vector2 promptScreenOffset = new Vector2(80f, 40f);

    /// <summary>
    /// 玩家按下交互键时触发，参数为交互物类型
    /// </summary>
    public event Action<InteractionType> OnInteract;

    private 交互物[] interactables;
    private 交互物 currentTarget;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        interactables = FindObjectsOfType<交互物>();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (interactables.Length == 0)
            Debug.LogWarning("[交互管理] 场景中没有找到任何 交互物 组件");
        else
            Debug.Log($"[交互管理] 发现 {interactables.Length} 个交互物");
    }

    void Update()
    {
        if (player == null || interactionPrompt == null)
            return;

        // 找到玩家范围内最近的交互物
        交互物 closest = null;
        float closestDist = float.MaxValue;

        foreach (var item in interactables)
        {
            if (item == null || !item.isActiveAndEnabled)
                continue;

            if (item.IsPlayerInRange(player))
            {
                float dist = Vector3.Distance(item.GetBodyCenter(), player.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = item;
                }
            }
        }

        currentTarget = closest;

        if (currentTarget != null)
        {
            // 更新提示文字和位置
            if (promptText != null)
                promptText.text = currentTarget.promptMessage;

            UpdatePromptPosition();

            if (!interactionPrompt.activeSelf)
                interactionPrompt.SetActive(true);

            // F 键触发交互
            if (Input.GetKeyDown(interactKey))
            {
                OnInteract?.Invoke(currentTarget.interactionType);
            }
        }
        else
        {
            if (interactionPrompt.activeSelf)
                interactionPrompt.SetActive(false);
        }
    }

    private void UpdatePromptPosition()
    {
        if (mainCamera == null || player == null || interactionPrompt == null)
            return;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(player.position);
        var rect = interactionPrompt.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.position = screenPos + new Vector3(promptScreenOffset.x, promptScreenOffset.y, 0f);
        }
    }
}
