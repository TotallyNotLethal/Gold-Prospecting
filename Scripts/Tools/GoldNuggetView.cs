using UnityEngine;

/// <summary>
/// Lightweight component spawned by <see cref="PanContents"/> to represent a revealed gold nugget.
/// Stores the particle reference and exposes a tiny highlight hook so interaction systems can
/// show hover feedback before the player collects the nugget.
/// </summary>
public class GoldNuggetView : MonoBehaviour
{
    [SerializeField] private GameObject highlightObject;

    public PanContents Owner { get; private set; }
    public PanContents.GoldParticle GoldParticle { get; private set; }

    public void Initialize(PanContents owner, PanContents.GoldParticle particle)
    {
        Owner = owner;
        GoldParticle = particle;
        SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(hovered);
        }
    }
}
