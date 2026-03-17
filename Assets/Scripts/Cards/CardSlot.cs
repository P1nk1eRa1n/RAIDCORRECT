using UnityEngine;

[DisallowMultipleComponent]
public class CardSlot : MonoBehaviour, IInteractable
{
    [Header("Slot")]
    public Transform slotTransform;
    public string slotId;

    [Header("Facing")]
    [Tooltip("Оффсет в euler (в градусах) чтобы карта 'лицом' смотрела на игрока. Например (90,0,0) если передняя грань у модели - local up.")]
    public Vector3 frontEulerOffset = new Vector3(0f, 0f, 0f);

    [Header("Visual root")]
    [Tooltip("Если указать - этот объект будет включаться/выключаться для визуальной части слота.\nЕсли null - будет использован сам gameObject.")]
    public GameObject visualRoot;

    [Header("Snap")]
    public float snapSmooth = 20f;

    [Header("Eject")]
    public float ejectForce = 2f;
    public float ejectUp = 1f;

    [Header("Levitataion")]
    public float bobAmplitude = 0.02f;
    public float bobSpeed = 2f;
    public float facePlayerDistance = 2.5f;
    public float faceSlerp = 6f;

    public CardItem placedCard { get; private set; } = null;
    public bool isEnabled { get; private set; } = true;

    private Vector3 basePosition;

    private void OnValidate() { if (slotTransform == null) slotTransform = transform; if (visualRoot == null) visualRoot = gameObject; }
    private void Awake() { if (slotTransform == null) slotTransform = transform; basePosition = slotTransform.position; if (visualRoot == null) visualRoot = gameObject; }

    private void Update()
    {
        if (placedCard != null)
        {
            // bob + face player logic (as before)
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            Vector3 targetPos = basePosition + Vector3.up * bob;
            var t = placedCard.transform;
            t.position = Vector3.Lerp(t.position, targetPos, Time.deltaTime * snapSmooth);

            var cam = Camera.main;
            if (cam != null)
            {
                float dist = Vector3.Distance(cam.transform.position, t.position);
                if (dist <= facePlayerDistance)
                {
                    // направление на камеру (немного выше центра)
                    Vector3 dirToCam = (cam.transform.position + Vector3.up * 0.5f) - t.position;
                    if (dirToCam.sqrMagnitude > 0.0001f)
                    {
                        // базовая ротация, чтобы forward объекта был направлен на камеру
                        Quaternion baseLook = Quaternion.LookRotation(dirToCam.normalized, Vector3.up);

                        // применяем оффсет (локальный euler) — таким образом можно корректировать,
                        // какая локальная ось считается "лицом"
                        Quaternion offset = Quaternion.Euler(frontEulerOffset);

                        // целевая ротация: базовая * оффсет (порядок важен: оффсет после LookRotation)
                        Quaternion targetRotation = baseLook * offset;

                        t.rotation = Quaternion.Slerp(t.rotation, targetRotation, Time.deltaTime * faceSlerp);
                    }
                }
                else
                {
                    // возвращаем в исходную ориентацию слота
                    t.rotation = Quaternion.Slerp(t.rotation, slotTransform.rotation, Time.deltaTime * snapSmooth);
                }
            }
            else
            {
                t.rotation = Quaternion.Slerp(t.rotation, slotTransform.rotation, Time.deltaTime * snapSmooth);
            }
        }
    }

    // enable/disable visual part
    public void SetEnabled(bool on)
    {
        isEnabled = on;
        if (visualRoot != null)
            visualRoot.SetActive(on);
        else
            gameObject.SetActive(on);
    }

    public bool PlaceCard(CardItem card)
    {
        if (card == null) return false;
        if (!isEnabled) return false; // respect disabled state
        if (placedCard != null) return false;
        if (card.IsPlaced) return false;

        placedCard = card;
        card.PlaceInSlot(this);

        // If part of a pedestal - debug log & notify manager
        var pedestal = GetComponentInParent<SpellPedestal>();
        if (pedestal != null)
        {
            Debug.Log($"[Pedestal] Placed card '{(card.cardData != null ? card.cardData.displayName : card.name)}' (id={(card.cardData != null ? card.cardData.cardId : "-")}) into pedestal slot '{slotId}'.");
            SpellManager.Instance?.NotifyPreparedChanged();
        }

        return true;
    }

    public bool RemoveCard()
    {
        if (placedCard == null) return false;
        var card = placedCard;
        placedCard = null;

        Vector3 dir = slotTransform.forward + Vector3.up * 0.3f;
        Vector3 impulse = dir.normalized * ejectForce + Vector3.up * ejectUp;
        card.RemoveFromSlot(impulse);

        var pedestal = GetComponentInParent<SpellPedestal>();
        if (pedestal != null)
        {
            Debug.Log($"[Pedestal] Removed card '{(card.cardData != null ? card.cardData.displayName : card.name)}' from pedestal slot '{slotId}'.");
            SpellManager.Instance?.NotifyPreparedChanged();
        }

        return true;
    }

    public void Interact(PlayerStateMachine player)
    {
        if (placedCard == null) { Debug.Log($"CardSlot {slotId} empty."); return; }
        RemoveCard();
    }
}