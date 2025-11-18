using System;
using UnityEngine;

public enum ToolState
{
    Idle,
    Scooping,
    Carrying,
    Pouring
}

/// <summary>
/// Base class for interactable tools that can be picked up by the player.
/// Tracks highlighting, current state and exposes events that can drive animations.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class ToolBase : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] private GameObject highlightObject;

    [Header("Physics")]
    [SerializeField] private Rigidbody toolRigidbody;

    public event Action<ToolState> OnStateChanged;
    public event Action<float> OnFillAmountChanged;
    public event Action OnScoopStarted;
    public event Action OnScoopEnded;
    public event Action OnPourStarted;
    public event Action OnPourEnded;

    public ToolState CurrentState { get; private set; } = ToolState.Idle;
    public Rigidbody ToolRigidbody => toolRigidbody;
    public virtual float NormalizedFill => 0f;
    ToolBase IInteractable.Tool => this;

    protected virtual void Awake()
    {
        if (toolRigidbody == null)
        {
            toolRigidbody = GetComponent<Rigidbody>();
        }

        SetHighlight(false);
    }

    public virtual void SetHighlight(bool active)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(active);
        }
    }

    public void BeginScooping()
    {
        if (CurrentState == ToolState.Scooping)
        {
            return;
        }

        if (CurrentState != ToolState.Carrying && CurrentState != ToolState.Idle)
        {
            return;
        }

        ChangeState(ToolState.Scooping);
        OnScoopStarted?.Invoke();
    }

    public void EndScooping()
    {
        if (CurrentState != ToolState.Scooping)
        {
            return;
        }

        ChangeState(ToolState.Carrying);
        OnScoopEnded?.Invoke();
    }

    public void EnterCarryState()
    {
        if (CurrentState == ToolState.Carrying)
        {
            return;
        }

        ChangeState(ToolState.Carrying);
    }

    public void StartPouring()
    {
        if (CurrentState == ToolState.Pouring)
        {
            return;
        }

        ChangeState(ToolState.Pouring);
        OnPourStarted?.Invoke();
    }

    public void StopPouring()
    {
        if (CurrentState != ToolState.Pouring)
        {
            return;
        }

        ChangeState(ToolState.Carrying);
        OnPourEnded?.Invoke();
    }

    public void ReturnToIdle()
    {
        ChangeState(ToolState.Idle);
    }

    protected void NotifyFillChanged(float normalized)
    {
        OnFillAmountChanged?.Invoke(Mathf.Clamp01(normalized));
    }

    private void ChangeState(ToolState state)
    {
        if (CurrentState == state)
        {
            return;
        }

        CurrentState = state;
        OnStateChanged?.Invoke(CurrentState);
    }
}
