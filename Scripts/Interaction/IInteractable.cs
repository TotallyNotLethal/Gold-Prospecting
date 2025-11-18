using UnityEngine;

/// <summary>
/// Simple interface for objects that can be highlighted and interacted with by the <see cref="InteractionController"/>.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called when the interaction controller begins or ends hovering over this object.
    /// </summary>
    /// <param name="active">True when the object should be highlighted.</param>
    void SetHighlight(bool active);

    /// <summary>
    /// The tool component tied to this interactable (if any).
    /// </summary>
    ToolBase Tool { get; }
}
