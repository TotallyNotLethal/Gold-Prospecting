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
    public class GoldParticle
    {
        [SerializeField] private float mass = 1f;
        [SerializeField] private float density = 19.3f;
        [SerializeField] private float valuePerGram = 55f;
        [SerializeField, Range(0f, 1f)] private float revealSedimentRatio = 0.35f;
        [SerializeField] private Vector3 localPanPosition;
        [SerializeField] private bool revealed;

        public GoldParticle(float mass, float density)
        {
            this.mass = mass;
            this.density = density;
        }

        public GoldParticle(float mass, float density, float revealRatio, Vector3 localPosition, float valuePerGram = 55f)
        {
            this.mass = mass;
            this.density = density;
            this.revealSedimentRatio = Mathf.Clamp01(revealRatio);
            this.localPanPosition = localPosition;
            this.valuePerGram = Mathf.Max(0f, valuePerGram);
        }

        public float Mass => mass;
        public float Density => density;
        public float ValuePerGram => valuePerGram;
        public float EstimatedValue => mass * valuePerGram;
        public float RevealRatio => revealSedimentRatio;
        public Vector3 LocalPanPosition => localPanPosition;
        public bool IsRevealed => revealed;

        public void MarkRevealed()
        {
            revealed = true;
        }

        public void SetLocalPosition(Vector3 position)
        {
            localPanPosition = position;
        }
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

    [Header("Gold Visuals")]
    [SerializeField] private GameObject goldNuggetPrefab;
    [SerializeField] private Transform goldNuggetParent;

    private readonly HashSet<int> triggeredMilestones = new();
    private readonly Dictionary<GoldParticle, GoldNuggetView> goldVisuals = new();
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
    public event Action<GoldParticle> OnGoldRevealed;
    public event Action<GoldParticle> OnGoldCollected;

    public IReadOnlyList<WaterParticle> WaterParticles => waterParticles;
    public IReadOnlyList<SedimentParticle> SedimentParticles => sedimentParticles;
    public IReadOnlyList<GoldParticle> GoldParticles => goldParticles;

    public float CurrentWaterVolume => currentWaterVolume;
    public float HeavySettlingBias => heavySettlingBias;
    public float LightSettlingBias => lightSettlingBias;
    public float SedimentNormalized => initialSedimentCount == 0 ? 0f : (float)sedimentParticles.Count / initialSedimentCount;
    public float GoldNormalized => initialGoldCount == 0 ? 0f : (float)goldParticles.Count / initialGoldCount;

    private void OnValidate()
    {
        SortMilestones();
    }

    public GameObject GoldNuggetPrefab
    {
        get => goldNuggetPrefab;
        set => goldNuggetPrefab = value;
    }

    public Transform GoldNuggetParent
    {
        get => goldNuggetParent;
        set => goldNuggetParent = value;
    }

    public int SedimentCount => sedimentParticles.Count;
    public int GoldCount => goldParticles.Count;

    public void SyncInitialCounts()
    {
        initialSedimentCount = Mathf.Max(initialSedimentCount, sedimentParticles.Count);
        initialGoldCount = Mathf.Max(initialGoldCount, goldParticles.Count);
    }

    public void SetSedimentParticles(IEnumerable<SedimentParticle> newParticles)
    {
        sedimentParticles.Clear();
        sedimentParticles.AddRange(newParticles);
        triggeredMilestones.Clear();
        SyncInitialCounts();
        EvaluateSedimentMilestones();
    }

    public void SetGoldParticles(IEnumerable<GoldParticle> newParticles)
    {
        foreach (var view in goldVisuals.Values)
        {
            if (view == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(view.gameObject);
            }
            else
            {
                DestroyImmediate(view.gameObject);
            }
        }

        goldVisuals.Clear();
        goldParticles.Clear();
        goldParticles.AddRange(newParticles);
        triggeredMilestones.Clear();
        SyncInitialCounts();
        EvaluateSedimentMilestones();
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
        int startIndex = Mathf.Max(0, goldParticles.Count - count);
        for (int i = goldParticles.Count - 1; i >= startIndex; i--)
        {
            RemoveGoldParticleAt(i);
        }
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

    public bool TryCollectGold(GoldParticle target, out GoldParticle collectedParticle)
    {
        collectedParticle = null;

        if (target == null)
        {
            return false;
        }

        int index = goldParticles.IndexOf(target);
        if (index < 0)
        {
            return false;
        }

        if (!target.IsRevealed)
        {
            return false;
        }

        collectedParticle = target;
        RemoveGoldParticleAt(index);
        OnGoldCollected?.Invoke(collectedParticle);
        return true;
    }

    private void Awake()
    {
        if (goldNuggetParent == null)
        {
            goldNuggetParent = transform;
        }

        SyncInitialCounts();
        RecalculateWaterVolume();
        SortMilestones();
    }

    private void EvaluateSedimentMilestones()
    {
        SyncInitialCounts();

        if (initialSedimentCount == 0)
        {
            RevealGoldIfNeeded(0f);
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

        RevealGoldIfNeeded(normalizedRemaining);
    }

    private void SortMilestones()
    {
        sedimentMilestones.Sort((a, b) => a.normalizedRemainingThreshold.CompareTo(b.normalizedRemainingThreshold));
    }

    private void RevealGoldIfNeeded(float normalizedRemaining)
    {
        foreach (var gold in goldParticles)
        {
            if (gold.IsRevealed)
            {
                continue;
            }

            if (normalizedRemaining > gold.RevealRatio)
            {
                continue;
            }

            gold.MarkRevealed();
            SpawnGoldVisual(gold);
            OnGoldRevealed?.Invoke(gold);
        }
    }

    private void SpawnGoldVisual(GoldParticle gold)
    {
        if (gold == null || goldVisuals.ContainsKey(gold))
        {
            return;
        }

        if (goldNuggetPrefab == null)
        {
            return;
        }

        Transform parent = goldNuggetParent == null ? transform : goldNuggetParent;
        var nugget = Instantiate(goldNuggetPrefab, parent);
        nugget.transform.localPosition = gold.LocalPanPosition;
        nugget.transform.localRotation = Quaternion.identity;

        if (!nugget.TryGetComponent(out GoldNuggetView view))
        {
            view = nugget.AddComponent<GoldNuggetView>();
        }

        view.Initialize(this, gold);
        goldVisuals[gold] = view;
    }

    private void RemoveGoldParticleAt(int index)
    {
        if (index < 0 || index >= goldParticles.Count)
        {
            return;
        }

        var particle = goldParticles[index];
        goldParticles.RemoveAt(index);
        DespawnGoldVisual(particle);
    }

    private void DespawnGoldVisual(GoldParticle particle)
    {
        if (particle == null)
        {
            return;
        }

        if (goldVisuals.TryGetValue(particle, out GoldNuggetView view))
        {
            if (view != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(view.gameObject);
                }
                else
                {
                    DestroyImmediate(view.gameObject);
                }
            }

            goldVisuals.Remove(particle);
        }
    }
}
