using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DraggableObject : MonoBehaviour, IDraggable, IHoverable
{
    [Header("Data")]
    public ItemData itemData;

    [Header("Highlight")]
    public Color highlightColor = new Color(0.9f, 0.8f, 0.1f, 1f);
    [Range(0f, 1f)]
    public float highlightAmount = 0.6f;

    // state
    public bool IsPlaced { get; private set; } = false;
    public BowlArea PlacedInBowl { get; private set; } = null;

    // теперь external code может устанавливать флаг (DragSystem делает это раньше)
    public bool IsDragging { get; set; } = false;

    // renderers & colors
    Renderer[] renderers;
    Color[] originalColors;

    // physics
    Rigidbody rb;
    Collider[] colliders;
    Transform originalParent;
    Vector3 originalLocalScale;

    // store original physics settings to restore later
    bool hadRigidbody = false;
    bool originalUseGravity = true;
    bool originalDetectCollisions = true;
    bool originalIsKinematic = false;
    CollisionDetectionMode originalCollisionMode;
    int originalLayer;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null)
                renderers[i].material = new Material(renderers[i].material);
            if (renderers[i].material != null && renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }

        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);
        originalParent = transform.parent;
        originalLocalScale = transform.localScale;
        hadRigidbody = rb != null;

        if (hadRigidbody)
        {
            originalUseGravity = rb.useGravity;
            originalDetectCollisions = rb.detectCollisions;
            originalIsKinematic = rb.isKinematic;
            originalCollisionMode = rb.collisionDetectionMode;
        }

        originalLayer = gameObject.layer;
    }

    #region Hover
    public void SetHover(bool on)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].material;
            if (mat == null) continue;

            if (mat.HasProperty("_Color"))
            {
                Color baseCol = originalColors[i];
                Color target = Color.Lerp(baseCol, highlightColor, highlightAmount);
                mat.color = on ? target : baseCol;
            }
            else if (mat.HasProperty("_EmissionColor"))
            {
                if (on)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EMISSION", highlightColor * highlightAmount);
                }
                else
                {
                    mat.SetColor("_EMISSION", Color.black);
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }
    #endregion

    #region Drag API
    // –еализаци€ IDraggable.StartDrag
    public void StartDrag()
    {
        if (IsDragging) return;

        // ≈сли объект был в чаше, предполагаем, что BowlArea.RemoveItem была вызвана ранее.
        IsDragging = true;
        IsPlaced = false;
        PlacedInBowl = null;

        originalLayer = gameObject.layer;

        if (hadRigidbody)
        {
            // если тело динамическое Ч обнулим скорости перед переводом в кинематическое
            if (!rb.isKinematic)
            {
#if UNITY_2022_2_OR_NEWER
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
#endif
            }

            // делаем kinematic чтобы безопасно двигать трансформом
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.isKinematic = true;
        }

        transform.SetParent(null, true);
    }

    // –еализаци€ IDraggable.StopDrag()
    public void StopDrag()
    {
        if (!IsDragging) return;
        IsDragging = false;

        gameObject.layer = originalLayer;

        if (hadRigidbody)
        {
            // вернуть физику в прежнее состо€ние
            rb.isKinematic = false;
            rb.detectCollisions = originalDetectCollisions;
            rb.useGravity = originalUseGravity;
            rb.collisionDetectionMode = originalCollisionMode;
        }
    }

    // ƒополнительный безопасный релиз с начальной скоростью
    public void ReleaseWithVelocity(Vector3 velocity)
    {
        // сначала делаем StopDrag (внутри это делает rb.isKinematic = false)
        if (!IsDragging)
        {
            // всЄ равно применим velocity, если rb есть и динамический
            if (hadRigidbody && !rb.isKinematic)
            {
#if UNITY_2022_2_OR_NEWER
                rb.linearVelocity = velocity;
#else
                rb.velocity = velocity;
#endif
            }
            return;
        }

        // выключаем drag (делает rb non-kinematic)
        StopDrag();

        // теперь rb не кинематический Ч безопасно назначать скорость
        if (hadRigidbody && rb != null)
        {
#if UNITY_2022_2_OR_NEWER
            rb.linearVelocity = velocity;
#else
            rb.velocity = velocity;
#endif
        }
    }
    #endregion

    #region Bowl slot API
    public void PlaceInSlot(Transform slotTransform, BowlArea bowl)
    {
        if (slotTransform == null) return;

        IsPlaced = true;
        PlacedInBowl = bowl;
        IsDragging = false;

        transform.SetParent(null, true);
        transform.position = slotTransform.position;
        transform.rotation = slotTransform.rotation;
        transform.localScale = originalLocalScale;

        if (hadRigidbody)
        {
            // если тело сейчас не кинематическое Ч обнулим скорости
            if (!rb.isKinematic)
            {
#if UNITY_2022_2_OR_NEWER
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
#endif
            }

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }
    }

    public void RemoveFromSlot()
    {
        if (!IsPlaced) return;

        IsPlaced = false;
        PlacedInBowl = null;

        if (hadRigidbody)
        {
            rb.isKinematic = false;
            rb.detectCollisions = originalDetectCollisions;
            rb.useGravity = originalUseGravity;
        }

        transform.SetParent(null, true);
    }
    #endregion

    #region Helpers
    public string[] GetTags() => itemData != null ? itemData.tags : new string[0];
    public string GetDisplayName() => itemData != null ? itemData.displayName : gameObject.name;
    #endregion
}