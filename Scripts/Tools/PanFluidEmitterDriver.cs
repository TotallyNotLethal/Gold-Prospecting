using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the birth rate of any attached fluid emitters so they match the current
/// water volume inside the <see cref="PanContents"/>.
/// </summary>
[DisallowMultipleComponent]
public class PanFluidEmitterDriver : MonoBehaviour
{
    [SerializeField] private PanContents contents;
    [SerializeField] private float maxBirthRate = 400f;
    [SerializeField] private float volumeForMaxBirthRate = 1f;
    [Tooltip("Adapters implementing IFluidEmitterAdapter that point to Obi or VFX emitters inside the pan.")]
    [SerializeField] private List<MonoBehaviour> emitterAdapters = new();

    private readonly List<IFluidEmitterAdapter> cachedAdapters = new();

    private void Awake()
    {
        if (contents == null)
        {
            contents = GetComponentInParent<PanContents>();
        }

        CacheAdapters();
    }

    private void OnValidate()
    {
        CacheAdapters();
    }

    private void OnEnable()
    {
        if (contents != null)
        {
            contents.OnWaterVolumeChanged += HandleWaterVolumeChanged;
            HandleWaterVolumeChanged(contents.CurrentWaterVolume);
        }
    }

    private void OnDisable()
    {
        if (contents != null)
        {
            contents.OnWaterVolumeChanged -= HandleWaterVolumeChanged;
        }
    }

    private void CacheAdapters()
    {
        cachedAdapters.Clear();
        foreach (var behaviour in emitterAdapters)
        {
            if (behaviour is IFluidEmitterAdapter adapter)
            {
                cachedAdapters.Add(adapter);
            }
        }
    }

    private void HandleWaterVolumeChanged(float volume)
    {
        if (volumeForMaxBirthRate <= Mathf.Epsilon)
        {
            return;
        }

        float normalized = Mathf.Clamp01(volume / volumeForMaxBirthRate);
        float targetBirthRate = normalized * maxBirthRate;

        for (int i = 0; i < cachedAdapters.Count; i++)
        {
            cachedAdapters[i].SetBirthRate(targetBirthRate);
        }
    }
}
