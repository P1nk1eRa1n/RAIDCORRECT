using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(DraggableObject))]
[RequireComponent(typeof(Rigidbody))]
public class CardItem : MonoBehaviour, IDraggable, IHoverable
{
    [Header("Data")]
    public SpellCardData cardData;

    [Header("Visuals")]
    public SpriteRenderer artRenderer;           // assign in prefab (world-space sprite)
    public TextMeshProUGUI titleText;                // world-space TMP for title
    public TextMeshProUGUI descriptionText;          // world-space TMP for description (short)
    public TextMeshProUGUI manaText;                 // world-space TMP for mana cost
    public Renderer shimmerRenderer;             // renderer with special shimmer material (optional)

    [Header("Hover")]
    public Color hoverColor = Color.cyan;
    [Range(0f, 1f)] public float hoverAmount = 0.6f;

    [Header("Recover / Safety")]
    public float recoverCheckDelay = 0.6f;       // check after release
    public float recoverMaxDrop = 4f;            // if falls farther than this -> recover
    public float recoverHeight = 1.2f;           // where to place if recovered

    // state
    public bool IsPlaced { get; private set; } = false;
    public CardSlot PlacedInSlot { get; private set; } = null;

    // internals
    Renderer[] renderers;
    Color[] originalColors;
    Vector3 storedLocalScale;
    Rigidbody rb;
    Transform originalParent;

    // shimmer animation
    Material shimmerMaterialInstance;
    float shimmerSpeed = 0.6f;
    float shimmerIntensity = 1.0f;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].material;
            originalColors[i] = (mat != null && mat.HasProperty("_Color")) ? mat.color : Color.white;
        }

        originalParent = transform.parent;
        storedLocalScale = transform.localScale;

        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // instantiate shimmer material if provided (to avoid changing shared material)
        if (shimmerRenderer != null)
        {
            try
            {
                shimmerMaterialInstance = new Material(shimmerRenderer.material);
                shimmerRenderer.material = shimmerMaterialInstance;
            }
            catch { shimmerMaterialInstance = null; }
        }

        UpdateVisuals();
    }

    private void Update()
    {
        // animate shimmer when present
        if (shimmerMaterialInstance != null)
        {
            // Expect shader uses '_ShimmerOffset' or mainTexture offset - we'll try both
            float off = Time.time * shimmerSpeed;
            if (shimmerMaterialInstance.HasProperty("_ShimmerOffset"))
                shimmerMaterialInstance.SetFloat("_ShimmerOffset", off);
            else
            {
                // fallback: animate main texture offset X
                Vector2 o = shimmerMaterialInstance.mainTextureOffset;
                o.x = off % 1f;
                shimmerMaterialInstance.mainTextureOffset = o;
            }
            if (shimmerMaterialInstance.HasProperty("_ShimmerIntensity"))
                shimmerMaterialInstance.SetFloat("_ShimmerIntensity", shimmerIntensity);
        }
    }

    // Update visual elements based on cardData
    public void UpdateVisuals()
    {
        if (cardData != null)
        {
            if (artRenderer != null)
            {
                artRenderer.sprite = cardData.icon;
                artRenderer.enabled = cardData.icon != null;
            }

            if (titleText != null) titleText.text = cardData.displayName ?? "";
            if (descriptionText != null) descriptionText.text = cardData.description ?? "";
            if (manaText != null) manaText.text = cardData != null ? (cardData.manaCost.ToString()) : "-";
        }
        else
        {
            if (artRenderer != null) artRenderer.enabled = false;
            if (titleText != null) titleText.text = "";
            if (descriptionText != null) descriptionText.text = "";
            if (manaText != null) manaText.text = "";
        }
    }

    // IHoverable
    public void SetHover(bool on)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].material;
            if (mat == null) continue;
            if (mat.HasProperty("_Color")) mat.color = on ? Color.Lerp(originalColors[i], hoverColor, hoverAmount) : originalColors[i];
        }
    }

    // IDraggable
    public void StartDrag()
    {
        // start dragging: ensure non-kinematic and collision enabled
        IsPlaced = false;
        PlacedInSlot = null;

        if (rb != null)
        {
            // if currently kinematic, don't attempt to write velocity (that's the source of earlier errors)
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
            rb.detectCollisions = true;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void StopDrag()
    {
        if (rb != null)
        {
            // ensure physics active - let it fall naturally
            rb.detectCollisions = true;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // start recover check in case it falls through geometry
            StartCoroutine(RecoverIfFallen());
        }
    }

    // Place into a slot - safe: do not set velocities if kinematic already
    public void PlaceInSlot(CardSlot slot)
    {
        if (slot == null) return;

        IsPlaced = true;
        PlacedInSlot = slot;

        // snap transform
        transform.SetParent(null, true);
        transform.position = slot.slotTransform.position;
        transform.rotation = slot.slotTransform.rotation;
        transform.localScale = storedLocalScale;

        if (rb != null)
        {
            // Try to zero velocities only if we can (if non-kinematic)
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

            // disable collisions and gravity AFTER velocities handled
            rb.detectCollisions = false;
            rb.useGravity = false;

            // set kinematic to fix object in place
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.interpolation = RigidbodyInterpolation.None;
        }

        SetHover(false);
    }

    // Remove from slot and give small eject impulse
    public void RemoveFromSlot(Vector3 ejectImpulse)
    {
        if (!IsPlaced) return;
        IsPlaced = false;
        PlacedInSlot = null;

        transform.SetParent(null, true);
        transform.localScale = storedLocalScale;

        if (rb != null)
        {
            rb.detectCollisions = true;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // apply velocity only now (rb is non-kinematic)
#if UNITY_2022_2_OR_NEWER
            rb.linearVelocity = ejectImpulse;
#else
            rb.velocity = ejectImpulse;
#endif
        }

        // after release, start recover check in case it falls through
        StartCoroutine(RecoverIfFallen());
    }

    // If after short time the card has no ground underneath (or fell far) - recover to safe place
    private IEnumerator RecoverIfFallen()
    {
        yield return new WaitForSeconds(recoverCheckDelay);

        Vector3 origin = transform.position;
        // raycast down
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit h, recoverMaxDrop))
        {
            // close enough to ground -> OK
            yield break;
        }

        // Not found ground within recoverMaxDrop -> recover
        Debug.LogWarning($"[CardItem] Recovering fallen card '{(cardData != null ? cardData.displayName : gameObject.name)}'");

        // determine safe position: if had slot -> above that slot, else in front of camera
        Vector3 safePos = Vector3.zero;
        if (PlacedInSlot != null && PlacedInSlot.slotTransform != null)
            safePos = PlacedInSlot.slotTransform.position + Vector3.up * recoverHeight;
        else
        {
            var cam = Camera.main;
            if (cam != null)
                safePos = cam.transform.position + cam.transform.forward * 1.2f + Vector3.up * 1.0f;
            else
                safePos = transform.position + Vector3.up * 2f;
        }

        // teleport and clear velocities
        transform.position = safePos;
        transform.rotation = Quaternion.identity;
        if (rb != null)
        {
            rb.detectCollisions = true;
            rb.isKinematic = false;
            rb.useGravity = true;
#if UNITY_2022_2_OR_NEWER
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#endif
        }
    }

    public bool Matches(SpellCardData d) => d != null && cardData != null && d.cardId == cardData.cardId;
}