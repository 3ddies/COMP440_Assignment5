using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class FallingBlock : MonoBehaviour
{
    [Header("Gameplay")]
    public float minFallSpeed = 1f;  // minimum fall rate (cells/sec)
    public float maxFallSpeed = 3f;  // maximum fall rate (cells/sec)
    public bool hasLanded = false;

    public static event Action<FallingBlock> OnBlockLanded;

    private TetrisControls controls;
    private float fallSpeed;
    private float lastFallTime;
    private Collider2D[] childColliders;

    private void Awake()
    {
        controls = new TetrisControls();

        // Assign random fall speed
        fallSpeed = UnityEngine.Random.Range(minFallSpeed, maxFallSpeed);

        NormalizeChildLocalPositions();
        childColliders = GetComponentsInChildren<Collider2D>();
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Gameplay.MoveLeft.performed += OnMoveLeft;
        controls.Gameplay.MoveRight.performed += OnMoveRight;
        controls.Gameplay.Rotate.performed += OnRotate;
    }

    private void OnDisable()
    {
        controls.Gameplay.MoveLeft.performed -= OnMoveLeft;
        controls.Gameplay.MoveRight.performed -= OnMoveRight;
        controls.Gameplay.Rotate.performed -= OnRotate;
        controls.Disable();
    }

    private void Update()
    {
        if (hasLanded) return;

        if (Time.time - lastFallTime >= 1f / fallSpeed)
        {
            TryMove(Vector3.down);
            lastFallTime = Time.time;
        }
    }

    // ----------------- Input handlers -----------------
    private void OnMoveLeft(InputAction.CallbackContext ctx) { if (ctx.performed) TryMove(Vector3.left); }
    private void OnMoveRight(InputAction.CallbackContext ctx) { if (ctx.performed) TryMove(Vector3.right); }
    private void OnRotate(InputAction.CallbackContext ctx) { if (ctx.performed) AttemptRotate(); }

    // ----------------- Movement & collision -----------------
    private void TryMove(Vector3 dir)
    {
        if (hasLanded) return;

        Vector3 originalParent = transform.position;
        Vector3 candidateParent = originalParent + dir;
        candidateParent.x = Mathf.Round(candidateParent.x);
        candidateParent.y = Mathf.Round(candidateParent.y);

        if (!CanPlaceAtParent(candidateParent))
        {
            if (dir == Vector3.down)
            {
                LandBlock();
            }
            return;
        }

        transform.position = candidateParent;
    }

    private bool CanPlaceAtParent(Vector3 parentPos)
    {
        int collisionMask = LayerMask.GetMask("Floor", "Tetromino");

        foreach (Transform child in transform)
        {
            Vector2 worldPos = (Vector2)(parentPos + child.localPosition);

            BoxCollider2D bcol = child.GetComponent<BoxCollider2D>();
            Vector2 size = (bcol != null) ? bcol.size : Vector2.one;

            Collider2D hit = Physics2D.OverlapBox(worldPos, size * 0.9f, 0f, collisionMask);
            if (hit != null && hit.transform.root != this.transform.root)
                return false;
        }

        return true;
    }

    // ----------------- Rotation -----------------
    private void AttemptRotate()
    {
        if (hasLanded) return;

        SnapParentToGrid();
        NormalizeChildLocalPositions();

        int n = transform.childCount;
        Vector2Int[] locals = new Vector2Int[n];
        Transform[] children = new Transform[n];

        for (int i = 0; i < n; i++)
        {
            children[i] = transform.GetChild(i);
            Vector3 lp = children[i].localPosition;
            locals[i] = new Vector2Int(Mathf.RoundToInt(lp.x), Mathf.RoundToInt(lp.y));
        }

        Vector2Int[] rotated = new Vector2Int[n];
        for (int i = 0; i < n; i++)
        {
            int x = locals[i].x;
            int y = locals[i].y;
            rotated[i] = new Vector2Int(y, -x);
        }

        Vector3[] kicks = new Vector3[]
        {
            Vector3.zero,
            new Vector3(1,0,0),
            new Vector3(-1,0,0),
            new Vector3(0,1,0),
            new Vector3(0,-1,0),
            new Vector3(1,1,0),
            new Vector3(-1,1,0),
            new Vector3(1,-1,0),
            new Vector3(-1,-1,0)
        };

        Vector3 baseParent = transform.position;

        foreach (var kick in kicks)
        {
            Vector3 candidateParent = new Vector3(
                Mathf.Round(baseParent.x + kick.x),
                Mathf.Round(baseParent.y + kick.y),
                0f
            );

            bool blocked = false;
            int collisionMask = LayerMask.GetMask("Floor", "Tetromino");

            for (int i = 0; i < n; i++)
            {
                Vector2 probeWorld = (Vector2)(candidateParent + new Vector3(rotated[i].x, rotated[i].y, 0f));
                BoxCollider2D childCol = children[i].GetComponent<BoxCollider2D>();
                Vector2 size = (childCol != null) ? childCol.size : Vector2.one;

                Collider2D hit = Physics2D.OverlapBox(probeWorld, size * 0.9f, 0f, collisionMask);
                if (hit != null && hit.transform.root != this.transform.root)
                {
                    blocked = true;
                    break;
                }
            }

            if (!blocked)
            {
                for (int i = 0; i < n; i++)
                    children[i].localPosition = new Vector3(rotated[i].x, rotated[i].y, 0f);

                transform.position = candidateParent;
                childColliders = GetComponentsInChildren<Collider2D>();
                return;
            }
        }
    }

    // ----------------- Snap & Normalize -----------------
    private void NormalizeChildLocalPositions()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform c = transform.GetChild(i);
            Vector3 lp = c.localPosition;
            lp.x = Mathf.Round(lp.x);
            lp.y = Mathf.Round(lp.y);
            lp.z = 0f;
            c.localPosition = lp;
        }
        childColliders = GetComponentsInChildren<Collider2D>();
    }

    private void SnapParentToGrid()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Round(p.x);
        p.y = Mathf.Round(p.y);
        p.z = 0f;
        transform.position = p;
    }

    // ----------------- Landing -----------------
    private void LandBlock()
    {
        SnapParentToGrid();
        GridManager.Instance.AddToGrid(transform);

        hasLanded = true;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Static;

        OnBlockLanded?.Invoke(this);
    }
}
