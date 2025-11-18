using System;
using UnityEngine;

/// <summary>
/// Observes the pan's rigidbody motion and drives settling behaviour for the particles.
/// It also removes light sediment while the player shakes the pan and only allows gold
/// to be rinsed away once there is enough water and little sediment remaining.
/// </summary>
[RequireComponent(typeof(PanContents))]
public class PanMotionAnalyzer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PanContents contents;
    [SerializeField] private Rigidbody panBody;

    [Header("Angular Velocity Sampling")]
    [SerializeField] private float maxAngularSpeed = 10f;
    [SerializeField] private float shakeThreshold = 1.25f;
    [SerializeField] private AnimationCurve heavySettlingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve lightSettlingCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Sediment Removal")]
    [SerializeField] private float sedimentRemovalRate = 6f;

    [Header("Gold Rinse")]
    [SerializeField] private float rinseWaterVolumeThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float rinseSedimentNormalizedThreshold = 0.1f;
    [SerializeField] private float goldReleaseRate = 1f;

    public event Action<bool> OnShakeStateChanged;

    private float sedimentRemovalAccumulator;
    private float goldRemovalAccumulator;
    private bool isShaking;

    private void Reset()
    {
        contents = GetComponent<PanContents>();
        if (panBody == null)
        {
            panBody = GetComponentInParent<Rigidbody>();
        }
    }

    private void Awake()
    {
        if (contents == null)
        {
            contents = GetComponent<PanContents>();
        }

        if (panBody == null)
        {
            panBody = GetComponentInParent<Rigidbody>();
        }
    }

    public PanContents Contents
    {
        get => contents;
        set => contents = value;
    }

    public Rigidbody PanBody
    {
        get => panBody;
        set => panBody = value;
    }

    public float ShakeThreshold => shakeThreshold;

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (contents == null || panBody == null)
        {
            return;
        }

        float angularSpeed = panBody.angularVelocity.magnitude;
        float normalizedSpeed = maxAngularSpeed <= Mathf.Epsilon ? 0f : Mathf.Clamp01(angularSpeed / maxAngularSpeed);

        float heavySettling = heavySettlingCurve.Evaluate(normalizedSpeed);
        float lightSettling = lightSettlingCurve.Evaluate(normalizedSpeed);
        contents.ApplySettling(heavySettling, lightSettling);

        bool currentlyShaking = angularSpeed >= shakeThreshold;
        if (currentlyShaking)
        {
            sedimentRemovalAccumulator += sedimentRemovalRate * deltaTime;
            int particlesToRemove = Mathf.FloorToInt(sedimentRemovalAccumulator);
            if (particlesToRemove > 0)
            {
                contents.RemoveLightestSedimentParticles(particlesToRemove);
                sedimentRemovalAccumulator -= particlesToRemove;
            }
        }
        else
        {
            sedimentRemovalAccumulator = Mathf.Max(0f, sedimentRemovalAccumulator - deltaTime);
        }

        bool rinseReady = contents.CurrentWaterVolume >= rinseWaterVolumeThreshold &&
                          contents.SedimentNormalized <= rinseSedimentNormalizedThreshold;

        if (rinseReady)
        {
            goldRemovalAccumulator += goldReleaseRate * deltaTime;
            int goldToRemove = Mathf.FloorToInt(goldRemovalAccumulator);
            if (goldToRemove > 0)
            {
                contents.RemoveGoldParticles(goldToRemove);
                goldRemovalAccumulator -= goldToRemove;
            }
        }
        else
        {
            goldRemovalAccumulator = 0f;
        }

        if (currentlyShaking != isShaking)
        {
            isShaking = currentlyShaking;
            OnShakeStateChanged?.Invoke(isShaking);
        }
    }
}
