---
name: unity-no-physics-guide
description: Use when doing 2D movement, collision, or animation in a Unity project that lacks Physics/Physics2D modules. Covers manual AABB collision, sprite PPU/scaling, trigger zones, and common silent-failure debugging tactics.
---

# Unity 2D No-Physics Survival Guide

## When to load this skill

Read this skill when ANY of these is true:
- The project is Unity 2022 and the user asks about movement, collision, triggers, or animation
- A compile error mentions "built in package 'Physics' or 'Physics 2D'"
- A MonoBehaviour's `Update()` silently fails to run (no log output)
- The user wants WASD movement, object avoidance, or proximity-based sprite animation
- You see `[RequireComponent(typeof(Rigidbody))]` or `[RequireComponent(typeof(BoxCollider2D))]` in existing code on this project
- You're about to add Collider, Rigidbody, Rigidbody2D, or `Bounds` to any script

## Core principle

**Every Unity built-in package may be missing.** The user's Unity 2022 installation has neither Physics nor Physics2D. Any reference to a type from those modules — even just in an attribute like `[RequireComponent]` — causes a compile error. Some APIs like `Bounds` may compile but cause the whole script to silently not execute.

**Safe APIs** (in `UnityEngine.CoreModule`, always available):
`Transform`, `Vector2`, `Vector3`, `SpriteRenderer`, `Sprite`, `Input`, `Mathf`, `Debug`, `Time`, `GameObject`, `MonoBehaviour`, `GameObject.Find()`

**Unsafe APIs** (require Physics/Physics2D modules, DO NOT USE):
`Rigidbody`, `Rigidbody2D`, `Collider`, `Collider2D`, `BoxCollider`, `BoxCollider2D`, `CharacterController`, `RaycastHit`, `Physics.Raycast`, `[RequireComponent(typeof(AnyPhysicsType))]`

**Risky APIs** (may compile but cause silent Update failure on this user's setup):
`Bounds`, `Bounds.Intersects()`

## Movement pattern

```csharp
void Update()
{
    float h = Input.GetAxisRaw("Horizontal");  // NOT GetAxis — no smoothing
    float v = Input.GetAxisRaw("Vertical");
    Vector3 dir = new Vector3(h, v, 0f).normalized;
    Vector3 move = dir * moveSpeed * Time.deltaTime;
    transform.Translate(move, Space.World);
}
```

## Manual AABB collision (replace Bounds.Intersects)

```csharp
bool AABB(Vector2 c1, Vector2 s1, Vector2 c2, Vector2 s2)
{
    return c1.x - s1.x * 0.5f < c2.x + s2.x * 0.5f
        && c1.x + s1.x * 0.5f > c2.x - s2.x * 0.5f
        && c1.y - s1.y * 0.5f < c2.y + s2.y * 0.5f
        && c1.y + s1.y * 0.5f > c2.y - s2.y * 0.5f;
}
```

All size parameters passed to this function must be **world size** (baseSize × localScale), pre-computed before the call.

## Sliding collision (wall slide)

```csharp
Vector2 newPos = (Vector2)transform.position + move;
if (AABB(newPos, myWorldSz, obsWorldCenter, obsWorldSz))
{
    Vector2 xOnly = (Vector2)transform.position + new Vector2(move.x, 0f);
    if (AABB(xOnly, myWorldSz, obsWorldCenter, obsWorldSz))
        move.x = 0f;
    Vector2 yOnly = (Vector2)transform.position + new Vector2(0f, move.y);
    if (AABB(yOnly, myWorldSz, obsWorldCenter, obsWorldSz))
        move.y = 0f;
}
```

## Obstacle reference: use GameObject.Find, NOT scene YAML fileID

Scene YAML references (`obstacleTransform: {fileID: xxx}`) are unreliable — Unity may regenerate fileIDs on save, nulling the reference at runtime.

```csharp
void Start()
{
    GameObject obj = GameObject.Find("ObstacleName");
    if (obj != null) obstacleTransform = obj.transform;
    else Debug.LogError("Obstacle not found!");
}
```

## Sprite PPU mismatch

When switching between sprites from different sources, always check `.meta` → `spritePixelsToUnits`. If PPU differs, adjust `localScale` to compensate:

```
targetWorldHeight = 4.75
newSpriteHeight   = 76px / 100 PPU = 0.76
scale = 4.75 / 0.76 = 6.25
```

## Transparent padding in sprites → collision offset

A sprite PNG may be wider than the visible content (e.g., 74px wide but visible graphic only occupies left 39px). The Transform pivot at center (37px) will not align with the visible graphic center (19.5px).

**Fix:** add a `Vector2 obstacleOffset` in sprite-native units (before scale):
```
offset = visibleCenter - pivotCenter = 19.5 - 37 = -17.5px → -0.175 units at PPU 100
```

Apply offset in world space:
```csharp
Vector2 worldOff = obstacleOffset * Mathf.Abs(obstacleTransform.localScale.x);
Vector2 obsCenter = (Vector2)obstacleTransform.position + worldOff;
```

Use the SAME offset formula in both the collision script and the animation trigger script.

## Proximity-triggered sprite animation

```csharp
void Update()
{
    float dist = Vector3.Distance(bodyCenter, player.position); // bodyCenter = transform.position + offset
    bool inRange = dist < triggerDistance;

    if (inRange && currentFrame < frames.Length - 1)
    {
        timer += Time.deltaTime;
        if (timer >= frameDuration) { timer = 0f; currentFrame++; sr.sprite = frames[currentFrame]; }
    }
    else if (!inRange && currentFrame > 0)
    {
        timer += Time.deltaTime;
        if (timer >= frameDuration) { timer = 0f; currentFrame--; sr.sprite = frames[currentFrame]; }
    }
}
```

## Debugging: when Update() never fires

1. Strip the script to a bare minimum: no `[Header]`, no `[Tooltip]`, no `Bounds`, no Chinese characters in method bodies
2. Add `Debug.Log` at the top of `Start()` and `Update()`
3. If `Start` fires but `Update` doesn't → check `m_Enabled` and `m_IsActive`
4. If neither fires → there's a compile error; check Console for red messages
5. Once the minimal version works, add features back one at a time, testing after each

## Direct YAML scene edits: warning

When you edit `.unity` YAML directly:
- Unity **will overwrite** your changes if the user saves the scene in the editor
- Prefer putting logic in scripts and letting the user set Inspector values manually
- If you must edit YAML, tell the user "don't save the scene, just run"

## Scaling double-counting trap

If `obstacleSize` is meant to be the sprite's raw size (before scale), and a helper function multiplies it by `localScale`, ensure the stored value is the **pre-scale** size. Passing an already-scaled world value causes `worldSize × scale` → enormous collision boxes that cover the whole scene.
