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

    // obstacle — sprite is 74px wide, PPU 100, pivot center at 37px
    // visible fridge body is ~39px on LEFT side, its center ~19.5px from left edge
    // offset from sprite center = 19.5 - 37 = -17.5px = -0.175 world units (at scale 1)
    private Transform obstacleTransform;
    private Vector2 obstacleSize   = new Vector2(0.39f, 0.76f);
    private Vector2 obstacleOffset = new Vector2(-0.175f, 0f);

    void Start()
    {
        // find fridge by name
        GameObject fridge = GameObject.Find("Fridge 1 _0");
        if (fridge != null)
        {
            obstacleTransform = fridge.transform;
            Debug.Log("[Move] Found obstacle: " + fridge.name + " at " + obstacleTransform.position);
        }
        else
        {
            Debug.LogError("[Move] Fridge not found! Make sure GameObject is named 'Fridge 1 _0'");
        }

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

        if (obstacleTransform != null)
        {
            Vector2 mySz = new Vector2(
                characterSize.x * Mathf.Abs(transform.localScale.x),
                characterSize.y * Mathf.Abs(transform.localScale.y));

            Vector2 obsSz = new Vector2(
                obstacleSize.x * Mathf.Abs(obstacleTransform.localScale.x),
                obstacleSize.y * Mathf.Abs(obstacleTransform.localScale.y));

            Vector2 newPos = transform.position + move;

            Vector2 worldObsOffset = new Vector2(
                obstacleOffset.x * Mathf.Abs(obstacleTransform.localScale.x),
                obstacleOffset.y * Mathf.Abs(obstacleTransform.localScale.y));
            Vector2 obsPos = (Vector2)obstacleTransform.position + worldObsOffset;

            if (AABB(newPos, mySz, obsPos, obsSz))
            {
                Vector2 xPos = (Vector2)transform.position + new Vector2(move.x, 0f);
                if (AABB(xPos, mySz, obsPos, obsSz))
                    move.x = 0f;

                Vector2 yPos = (Vector2)transform.position + new Vector2(0f, move.y);
                if (AABB(yPos, mySz, obsPos, obsSz))
                    move.y = 0f;
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

            wasMovingLastFrame = isMoving;
        }
    }

    bool AABB(Vector2 c1, Vector2 s1, Vector2 c2, Vector2 s2)
    {
        return c1.x - s1.x * 0.5f < c2.x + s2.x * 0.5f
            && c1.x + s1.x * 0.5f > c2.x - s2.x * 0.5f
            && c1.y - s1.y * 0.5f < c2.y + s2.y * 0.5f
            && c1.y + s1.y * 0.5f > c2.y - s2.y * 0.5f;
    }
}
