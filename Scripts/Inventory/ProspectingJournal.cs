using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple data container that logs each discovered gold nugget. Entries can be piped into UI
/// using the provided UnityEvents while the optional <see cref="GoldEconomy"/> reference will
/// convert each find into spendable currency.
/// </summary>
public class ProspectingJournal : MonoBehaviour
{
    [Serializable]
    public class ProspectingEntry
    {
        public float mass;
        public float value;
        public DateTime timestamp;
        public Vector3 worldPosition;
    }

    [Serializable]
    public class ProspectingEntryEvent : UnityEvent<ProspectingEntry> { }

    [SerializeField] private GoldEconomy economy;
    [SerializeField] private ProspectingEntryEvent onEntryRecorded;
    [SerializeField] private UnityEvent<string> onEntryNotification;

    private readonly List<ProspectingEntry> entries = new();

    public IReadOnlyList<ProspectingEntry> Entries => entries;

    public void RecordFind(PanContents.GoldParticle particle, Vector3 worldPosition)
    {
        if (particle == null)
        {
            return;
        }

        float saleValue = particle.EstimatedValue;
        if (economy != null)
        {
            saleValue = economy.DepositGold(particle);
        }

        var entry = new ProspectingEntry
        {
            mass = particle.Mass,
            value = saleValue,
            timestamp = DateTime.UtcNow,
            worldPosition = worldPosition
        };

        entries.Add(entry);
        onEntryRecorded?.Invoke(entry);

        if (onEntryNotification != null)
        {
            string message = $"+{entry.value:0.##} credits • {entry.mass:0.##}g nugget";
            onEntryNotification.Invoke(message);
        }
    }
}
