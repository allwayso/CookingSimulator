using UnityEngine;

public class 冰箱动画 : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public Sprite[] frames = new Sprite[5];

    [Header("Settings")]
    [Tooltip("Distance at which the fridge starts opening")]
    public float triggerDistance = 4f;

    [Tooltip("Seconds per animation frame")]
    public float frameDuration = 0.12f;

    // visible fridge is on LEFT side of sprite; offset = -0.175 * localScale.x world units
    private Vector3 bodyCenter => transform.position + Vector3.left * (0.175f * Mathf.Abs(transform.localScale.x));

    private SpriteRenderer sr;
    private int currentFrame;
    private float timer;


    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (frames.Length > 0 && frames[0] != null)
            sr.sprite = frames[0];
    }


    void Update()
    {
        if (player == null || frames.Length < 2)
        {
            if (player == null) Debug.Log("[Fridge] player is NULL");
            if (frames.Length < 2) Debug.Log("[Fridge] frames missing, length=" + frames.Length);
            return;
        }

        float dist = Vector3.Distance(bodyCenter, player.position);
        bool inRange = dist < triggerDistance;
        Debug.Log("[Fridge] dist=" + dist.ToString("F2") + " trigger=" + triggerDistance + " inRange=" + inRange + " bodyCenter=" + bodyCenter + " player=" + player.position);

        if (inRange && currentFrame < frames.Length - 1)
        {
            timer += Time.deltaTime;
            if (timer >= frameDuration)
            {
                timer = 0f;
                currentFrame++;
                sr.sprite = frames[currentFrame];
            }
        }
        else if (!inRange && currentFrame > 0)
        {
            timer += Time.deltaTime;
            if (timer >= frameDuration)
            {
                timer = 0f;
                currentFrame--;
                sr.sprite = frames[currentFrame];
            }
        }
    }
}
