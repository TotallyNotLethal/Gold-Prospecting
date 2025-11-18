using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks the player's balance and optional tool unlock tiers so designers can gate more advanced
/// equipment behind gold discoveries. Gold deposits feed into the balance which then evaluates the
/// unlock list.
/// </summary>
public class GoldEconomy : MonoBehaviour
{
    [Serializable]
    public class ToolUnlockDefinition
    {
        public string id;
        public float cost;
        public bool unlocked;
    }

    [Serializable]
    public class ToolUnlockEvent : UnityEvent<ToolUnlockDefinition> { }

    [SerializeField] private float defaultSellPricePerGram = 60f;
    [SerializeField] private float sellBonusMultiplier = 1f;
    [SerializeField] private List<ToolUnlockDefinition> toolUnlocks = new();
    [SerializeField] private UnityEvent<float> onBalanceChanged;
    [SerializeField] private ToolUnlockEvent onToolUnlocked;

    private float balance;

    public float Balance => balance;
    public IReadOnlyList<ToolUnlockDefinition> ToolUnlocks => toolUnlocks;

    public float DepositGold(PanContents.GoldParticle particle)
    {
        if (particle == null)
        {
            return 0f;
        }

        float baseValue = particle.EstimatedValue;
        if (baseValue <= 0f)
        {
            baseValue = particle.Mass * defaultSellPricePerGram;
        }

        float payout = baseValue * sellBonusMultiplier;
        balance += payout;
        onBalanceChanged?.Invoke(balance);
        EvaluateUnlocks();
        return payout;
    }

    public bool TrySpend(float cost)
    {
        if (cost <= 0f)
        {
            return true;
        }

        if (balance < cost)
        {
            return false;
        }

        balance -= cost;
        onBalanceChanged?.Invoke(balance);
        return true;
    }

    private void EvaluateUnlocks()
    {
        foreach (var unlock in toolUnlocks)
        {
            if (unlock.unlocked)
            {
                continue;
            }

            if (balance >= unlock.cost)
            {
                unlock.unlocked = true;
                onToolUnlocked?.Invoke(unlock);
            }
        }
    }
}
