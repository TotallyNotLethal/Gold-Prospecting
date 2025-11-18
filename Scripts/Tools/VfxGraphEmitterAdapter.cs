using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Simple adapter that exposes a VFX Graph birth rate to <see cref="PanFluidEmitterDriver"/>.
/// </summary>
[DisallowMultipleComponent]
public class VfxGraphEmitterAdapter : MonoBehaviour, IFluidEmitterAdapter
{
    [SerializeField] private VisualEffect visualEffect;
    [SerializeField] private string birthRateProperty = "SpawnRate";

    private void Awake()
    {
        if (visualEffect == null)
        {
            visualEffect = GetComponent<VisualEffect>();
        }
    }

    public void SetBirthRate(float birthRatePerSecond)
    {
        if (visualEffect == null)
        {
            return;
        }

        if (!visualEffect.HasFloat(birthRateProperty))
        {
            return;
        }

        visualEffect.SetFloat(birthRateProperty, birthRatePerSecond);
    }
}
