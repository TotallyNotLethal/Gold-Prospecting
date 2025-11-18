using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of the materials currently stored inside the pan and exposes helper
/// events so gameplay, audio, or VFX systems can react to their state.
/// </summary>
[DisallowMultipleComponent]
public class PanContents : MonoBehaviour
{
    [Serializable]
    public struct WaterParticle
    {
        [SerializeField] private float mass;
        [SerializeField] private float density;

        public WaterParticle(float mass, float density)
        {
            this.mass = mass;
            this.density = density;
        }

        public float Mass => mass;
        public float Density => density;
        public float Volume => density <= Mathf.Epsilon ? 0f : mass / density;
    }

    [Serializable]
    public struct SedimentParticle
    {
        [SerializeField] private float mass;
        [SerializeField] private float density;

        public SedimentParticle(float mass, float density)
        {
            this.mass = mass;
            this.density = density;
        }

        public float Mass => mass;
        public float Density => density;
    }

    [Serializable]
    public struct GoldParticle
    {
        [SerializeField] private float mass;
        [SerializeField] private float density;

        public GoldParticle(float mass, float density)
        {
            this.mass = mass;
            this.density = density;
        }

        public float Mass => mass;
        public float Density => density;
    }

    [Serializable]
    public struct SedimentMilestoneCue
    {
        [Range(0f, 1f)] public float normalizedRemainingThreshold;
        public bool triggerSloshCue;
        public bool triggerSparkleCue;
    }

    [Header("Particle Data")]
    [SerializeField] private List<WaterParticle> waterParticles = new();
    [SerializeField] private List<SedimentParticle> sedimentParticles = new();
    [SerializeField] private List<GoldParticle> goldParticles = new();

    [Header("Sediment Milestones")]
    [SerializeField] private List<SedimentMilestoneCue> sedimentMilestones = new()
    {
        new SedimentMilestoneCue { normalizedRemainingThreshold = 0.75f, triggerSloshCue = true },
        new SedimentMilestoneCue { normalizedRemainingThreshold = 0.5f, triggerSloshCue = true },
        new SedimentMilestoneCue { normalizedRemainingThreshold = 0.25f, triggerSparkleCue = true }
    };

    private readonly HashSet<int> triggeredMilestones = new();
    private float currentWaterVolume;
    private float heavySettlingBias;
    private float lightSettlingBias;
    private int initialSedimentCount;
    private int initialGoldCount;

    public event Action<float> OnWaterVolumeChanged;
    public event Action<float> OnSedimentMilestoneReached;
    public event Action<float> OnSloshCue;
    public event Action<float> OnSparkleCue;
    public event Action<float, float> OnSettlingForcesChanged;

    public IReadOnlyList<WaterParticle> WaterParticles => waterParticles;
    public IReadOnlyList<SedimentParticle> SedimentParticles => sedimentParticles;
    public IReadOnlyList<GoldParticle> GoldParticles => goldParticles;

    public float CurrentWaterVolume => currentWaterVolume;
    public float HeavySettlingBias => heavySettlingBias;
    public float LightSettlingBias => lightSettlingBias;
    public float SedimentNormalized => initialSedimentCount == 0 ? 0f : (float)sedimentParticles.Count / initialSedimentCount;
    public float GoldNormalized => initialGoldCount == 0 ? 0f : (float)goldParticles.Count / initialGoldCount;

    private void Awake()
    {
        SyncInitialCounts();
        RecalculateWaterVolume();
        SortMilestones();
    }

    private void OnValidate()
    {
        SortMilestones();
    }

    public void SyncInitialCounts()
    {
        initialSedimentCount = Mathf.Max(initialSedimentCount, sedimentParticles.Count);
        initialGoldCount = Mathf.Max(initialGoldCount, goldParticles.Count);
    }

    public void SetWaterParticles(IEnumerable<WaterParticle> newParticles)
    {
        waterParticles.Clear();
        waterParticles.AddRange(newParticles);
        RecalculateWaterVolume();
    }

    public void AddWaterParticle(WaterParticle particle)
    {
        waterParticles.Add(particle);
        RecalculateWaterVolume();
    }

    public void ClearWater()
    {
        if (waterParticles.Count == 0)
        {
            return;
        }

        waterParticles.Clear();
        RecalculateWaterVolume();
    }

    public void RemoveLightestSedimentParticles(int count)
    {
        if (count <= 0 || sedimentParticles.Count == 0)
        {
            return;
        }

        count = Mathf.Min(count, sedimentParticles.Count);
        sedimentParticles.Sort((a, b) => a.Density.CompareTo(b.Density));
        sedimentParticles.RemoveRange(0, count);
        EvaluateSedimentMilestones();
    }

    public void RemoveGoldParticles(int count)
    {
        if (count <= 0 || goldParticles.Count == 0)
        {
            return;
        }

        count = Mathf.Min(count, goldParticles.Count);
        goldParticles.RemoveRange(Mathf.Max(0, goldParticles.Count - count), count);
    }

    public void ApplySettling(float heavyParticleBias, float lightParticleBias)
    {
        heavySettlingBias = heavyParticleBias;
        lightSettlingBias = lightParticleBias;
        OnSettlingForcesChanged?.Invoke(heavySettlingBias, lightSettlingBias);
    }

    private void RecalculateWaterVolume()
    {
        float total = 0f;
        foreach (var particle in waterParticles)
        {
            total += particle.Volume;
        }

        if (!Mathf.Approximately(total, currentWaterVolume))
        {
            currentWaterVolume = total;
            OnWaterVolumeChanged?.Invoke(currentWaterVolume);
        }
    }

    private void EvaluateSedimentMilestones()
    {
        SyncInitialCounts();

        if (initialSedimentCount == 0)
        {
            return;
        }

        float normalizedRemaining = SedimentNormalized;
        for (int i = 0; i < sedimentMilestones.Count; i++)
        {
            if (triggeredMilestones.Contains(i))
            {
                continue;
            }

            var milestone = sedimentMilestones[i];
            if (normalizedRemaining <= milestone.normalizedRemainingThreshold)
            {
                triggeredMilestones.Add(i);
                OnSedimentMilestoneReached?.Invoke(normalizedRemaining);

                if (milestone.triggerSloshCue)
                {
                    OnSloshCue?.Invoke(normalizedRemaining);
                }

                if (milestone.triggerSparkleCue)
                {
                    OnSparkleCue?.Invoke(normalizedRemaining);
                }
            }
        }
    }

    private void SortMilestones()
    {
        sedimentMilestones.Sort((a, b) => a.normalizedRemainingThreshold.CompareTo(b.normalizedRemainingThreshold));
    }
}
