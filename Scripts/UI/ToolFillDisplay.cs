using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the current fill level for the active tool.
/// </summary>
public class ToolFillDisplay : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject root;

    private ToolBase boundTool;

    private void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }

        SetFillAmount(0f);
        UpdateVisibility();
    }

    /// <summary>
    /// Bind to a tool and listen for fill events.
    /// </summary>
    public void BindTool(ToolBase tool)
    {
        if (boundTool == tool)
        {
            return;
        }

        if (boundTool != null)
        {
            boundTool.OnFillAmountChanged -= HandleFillAmountChanged;
        }

        boundTool = tool;

        if (boundTool != null)
        {
            boundTool.OnFillAmountChanged += HandleFillAmountChanged;
            SetFillAmount(boundTool.NormalizedFill);
        }
        else
        {
            SetFillAmount(0f);
        }

        UpdateVisibility();
    }

    private void HandleFillAmountChanged(float normalized)
    {
        SetFillAmount(normalized);
    }

    private void SetFillAmount(float value)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(value);
        }
    }

    private void UpdateVisibility()
    {
        if (root != null)
        {
            root.SetActive(boundTool != null);
        }
    }
}
