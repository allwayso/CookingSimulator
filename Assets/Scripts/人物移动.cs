using UnityEngine;

public class 人物移动 : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Vector2 characterSize = new Vector2(0.16f, 0.23f);

    [Header("Animation")]
    public Sprite[] walkFrames = new Sprite[4];
    public Sprite idleSprite;
    public float walkFrameDuration = 0.15f;

    private SpriteRenderer sr;
    private int currentWalkFrame;
    private float walkTimer;
    private bool wasMovingLastFrame;

    private BlockObject[] obstacles;

    void Start()
    {
        obstacles = FindObjectsOfType<BlockObject>();
        Debug.Log($"[Move] Found {obstacles.Length} block obstacles");

        sr = GetComponent<SpriteRenderer>();
        if (idleSprite == null && sr != null)
            idleSprite = sr.sprite;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, v, 0f).normalized;
        Vector3 move = dir * moveSpeed * Time.deltaTime;

        if (obstacles != null && obstacles.Length > 0)
        {
            Vector2 mySz = new Vector2(
                characterSize.x * Mathf.Abs(transform.localScale.x),
                characterSize.y * Mathf.Abs(transform.localScale.y));

            Vector2 newPos = transform.position + move;

            foreach (var obs in obstacles)
            {
                if (obs == null) continue;

                Vector2 obsPos = obs.GetWorldCenter();
                Vector2 obsSz = obs.GetWorldSize();

                if (AABB(newPos, mySz, obsPos, obsSz))
                {
                    // 分别尝试 X / Y 分量，允许滑墙
                    Vector2 xPos = (Vector2)transform.position + new Vector2(move.x, 0f);
                    if (AABB(xPos, mySz, obsPos, obsSz))
                        move.x = 0f;

                    Vector2 yPos = (Vector2)transform.position + new Vector2(0f, move.y);
                    if (AABB(yPos, mySz, obsPos, obsSz))
                        move.y = 0f;
                }
            }
        }

        if (move.x != 0f || move.y != 0f)
            transform.Translate(move, Space.World);

        // Walk animation — driven by final move vector (zeroed by collision = no walk)
        if (sr != null)
        {
            bool isMoving = (move.x != 0f || move.y != 0f);

            if (isMoving)
            {
                if (walkFrames != null && walkFrames.Length > 0 && walkFrames[0] != null)
                {
                    walkTimer += Time.deltaTime;
                    if (walkTimer >= walkFrameDuration)
                    {
                        walkTimer -= walkFrameDuration;
                        currentWalkFrame = (currentWalkFrame + 1) % walkFrames.Length;
                        sr.sprite = walkFrames[currentWalkFrame];
                    }

                    if (!wasMovingLastFrame)
                    {
                        walkTimer = 0f;
                        currentWalkFrame = 0;
                        sr.sprite = walkFrames[0];
                    }
                }
            }
            else
            {
                if (idleSprite != null && wasMovingLastFrame)
                    sr.sprite = idleSprite;
                walkTimer = 0f;
                currentWalkFrame = 0;
            }
        }

        wasMovingLastFrame = move.x != 0f || move.y != 0f;
    }

    bool AABB(Vector2 c1, Vector2 s1, Vector2 c2, Vector2 s2)
    {
        return c1.x - s1.x * 0.5f < c2.x + s2.x * 0.5f
            && c1.x + s1.x * 0.5f > c2.x - s2.x * 0.5f
            && c1.y - s1.y * 0.5f < c2.y + s2.y * 0.5f
            && c1.y + s1.y * 0.5f > c2.y - s2.y * 0.5f;
    }
}
