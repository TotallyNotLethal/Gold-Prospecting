using UnityEngine;

/// <summary>
/// Handles the logic of detecting interactable tools, highlighting them, picking them up, and
/// driving the tool state machine based on player input.
/// </summary>
public class InteractionController : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxRayDistance = 5f;
    [SerializeField] private LayerMask interactableLayers = ~0;

    [Header("Tool Handling")]
    [SerializeField] private Transform handPivot;
    [SerializeField] private Rigidbody handRigidbody;
    [SerializeField] private float jointBreakForce = 1500f;
    [SerializeField] private float jointBreakTorque = 1500f;
    [SerializeField] private ToolFillDisplay fillDisplay;

    private IInteractable currentHover;
    private ToolBase currentTool;
    private Joint activeJoint;

    private void Awake()
    {
        if (!playerCamera)
        {
            playerCamera = Camera.main;
        }

        if (handRigidbody == null && handPivot != null)
        {
            handRigidbody = handPivot.GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (currentTool == null)
        {
            HandleHoverRaycast();

            if (Input.GetMouseButtonDown(0) && currentHover != null)
            {
                TryPickup(currentHover);
            }
        }
        else
        {
            HandleToolInput();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            DropCurrentTool();
        }
    }

    private void HandleHoverRaycast()
    {
        if (!playerCamera)
        {
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayers))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            SetHover(interactable);
        }
        else
        {
            SetHover(null);
        }
    }

    private void HandleToolInput()
    {
        if (currentTool == null)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            currentTool.BeginScooping();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            currentTool.EndScooping();
        }
    }

    private void SetHover(IInteractable interactable)
    {
        if (currentHover == interactable)
        {
            return;
        }

        currentHover?.SetHighlight(false);
        currentHover = interactable;
        currentHover?.SetHighlight(true);
    }

    private void TryPickup(IInteractable interactable)
    {
        if (interactable?.Tool == null)
        {
            return;
        }

        AttachTool(interactable.Tool);
    }

    private void AttachTool(ToolBase tool)
    {
        if (handRigidbody == null || handPivot == null)
        {
            Debug.LogWarning("InteractionController requires a hand pivot with a Rigidbody to attach tools.");
            return;
        }

        DropCurrentTool();
        currentTool = tool;
        currentTool.EnterCarryState();
        fillDisplay?.BindTool(currentTool);

        var toolBody = currentTool.ToolRigidbody;
        if (toolBody == null)
        {
            Debug.LogWarning($"{tool.name} is missing a Rigidbody component.");
            return;
        }

        FixedJoint joint = toolBody.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = handRigidbody;
        joint.breakForce = jointBreakForce;
        joint.breakTorque = jointBreakTorque;
        activeJoint = joint;

        SetHover(null);
    }

    private void DropCurrentTool()
    {
        if (currentTool == null)
        {
            return;
        }

        if (activeJoint != null)
        {
            Destroy(activeJoint);
            activeJoint = null;
        }

        currentTool.ReturnToIdle();
        currentTool = null;
        fillDisplay?.BindTool(null);
    }
}
