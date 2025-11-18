using System.Reflection;
using UnityEngine;

/// <summary>
/// Reflection based adapter so the project does not have to take a hard dependency on
/// the Obi Fluid assembly. Assign the Obi emitter component in the inspector and specify
/// which property or field should receive the birth rate.
/// </summary>
[DisallowMultipleComponent]
public class ObiEmitterAdapter : MonoBehaviour, IFluidEmitterAdapter
{
    [SerializeField] private Component obiEmitter;
    [SerializeField] private string birthRateProperty = "speed";
    [SerializeField] private string birthRateField = string.Empty;

    private PropertyInfo cachedProperty;
    private FieldInfo cachedField;

    private void Awake()
    {
        CacheMembers();
    }

    private void OnValidate()
    {
        CacheMembers();
    }

    public void SetBirthRate(float birthRatePerSecond)
    {
        if (obiEmitter == null)
        {
            return;
        }

        if (cachedProperty != null && cachedProperty.CanWrite)
        {
            cachedProperty.SetValue(obiEmitter, birthRatePerSecond);
            return;
        }

        if (cachedField != null)
        {
            cachedField.SetValue(obiEmitter, birthRatePerSecond);
        }
    }

    private void CacheMembers()
    {
        cachedProperty = null;
        cachedField = null;

        if (obiEmitter == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(birthRateProperty))
        {
            cachedProperty = obiEmitter.GetType().GetProperty(
                birthRateProperty,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (cachedProperty == null && !string.IsNullOrWhiteSpace(birthRateField))
        {
            cachedField = obiEmitter.GetType().GetField(
                birthRateField,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
