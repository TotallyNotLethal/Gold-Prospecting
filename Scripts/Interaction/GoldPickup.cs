using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Performs a centre-screen raycast looking for <see cref="GoldNuggetView"/> instances so the player
/// can pick them up after they have been revealed. Once collected, the referenced journal/economy
/// components receive the gold particle information to update scoring and UI.
/// </summary>
public class GoldPickup : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float pickupDistance = 3f;
    [SerializeField] private LayerMask goldLayerMask = ~0;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private ProspectingJournal prospectingJournal;

    [System.Serializable]
    public class GoldParticleEvent : UnityEvent<PanContents.GoldParticle> { }

    [SerializeField] private GoldParticleEvent onGoldPickedUp;

    private GoldNuggetView currentHover;

    private void Awake()
    {
        if (!playerCamera)
        {
            playerCamera = Camera.main;
        }

        if (prospectingJournal == null)
        {
            prospectingJournal = GetComponentInChildren<ProspectingJournal>();
        }
    }

    private void Update()
    {
        UpdateHover();

        if (currentHover != null && Input.GetKeyDown(pickupKey))
        {
            AttemptPickup(currentHover);
        }
    }

    private void UpdateHover()
    {
        if (playerCamera == null)
        {
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, goldLayerMask))
        {
            var view = hit.collider.GetComponentInParent<GoldNuggetView>();
            if (view != null && view.GoldParticle != null && view.GoldParticle.IsRevealed)
            {
                SetHover(view);
                return;
            }
        }

        SetHover(null);
    }

    private void SetHover(GoldNuggetView view)
    {
        if (currentHover == view)
        {
            return;
        }

        if (currentHover != null)
        {
            currentHover.SetHovered(false);
        }

        currentHover = view;
        if (currentHover != null)
        {
            currentHover.SetHovered(true);
        }
    }

    private void AttemptPickup(GoldNuggetView view)
    {
        if (view == null || view.Owner == null || view.GoldParticle == null)
        {
            return;
        }

        Vector3 pickupPosition = view.transform.position;
        if (view.Owner.TryCollectGold(view.GoldParticle, out var collected))
        {
            prospectingJournal?.RecordFind(collected, pickupPosition);
            onGoldPickedUp?.Invoke(collected);
            SetHover(null);
        }
    }
}
