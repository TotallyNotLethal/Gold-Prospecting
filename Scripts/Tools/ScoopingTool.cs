using UnityEngine;

/// <summary>
/// Tool implementation that fills with water when scooping inside a volume tagged as "WaterSurface".
/// </summary>
public class ScoopingTool : ToolBase
{
    [Header("Water Settings")]
    [SerializeField] private float maxWaterVolume = 1f;
    [SerializeField] private float fillRatePerSecond = 0.25f;
    [SerializeField] private float dwellTimeToStartFilling = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = false;

    private float currentWaterVolume;
    private float dwellTimer;
    private bool insideWater;

    public float CurrentWaterVolume => currentWaterVolume;
    public float MaxWaterVolume => maxWaterVolume;
    public override float NormalizedFill => maxWaterVolume <= Mathf.Epsilon ? 0f : currentWaterVolume / maxWaterVolume;

    private void Update()
    {
        if (CurrentState != ToolState.Scooping || !insideWater)
        {
            ResetDwellTimer();
            return;
        }

        dwellTimer += Time.deltaTime;
        if (dwellTimer >= dwellTimeToStartFilling)
        {
            AddWater(Time.deltaTime * fillRatePerSecond);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WaterSurface"))
        {
            insideWater = true;
            ResetDwellTimer();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WaterSurface"))
        {
            insideWater = false;
            ResetDwellTimer();
        }
    }

    private void AddWater(float amount)
    {
        currentWaterVolume = Mathf.Clamp(currentWaterVolume + amount, 0f, maxWaterVolume);
        NotifyFillChanged(NormalizedFill);
    }

    private void ResetDwellTimer()
    {
        dwellTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
