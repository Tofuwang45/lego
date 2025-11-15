using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for displaying build history and generating AI guides.
/// Attach to a Canvas or UI GameObject.
/// </summary>
public class BuildHistoryUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshPro component to display history")]
    public TextMeshProUGUI historyText;

    [Tooltip("Button to refresh the history display")]
    public Button refreshButton;

    [Tooltip("Button to generate AI prompt")]
    public Button generatePromptButton;

    [Tooltip("Button to clear history")]
    public Button clearHistoryButton;

    [Tooltip("TextMeshPro component to display AI prompt")]
    public TextMeshProUGUI aiPromptText;

    [Header("Settings")]
    [Tooltip("Auto-update display every N seconds (0 = disabled)")]
    public float autoUpdateInterval = 2f;

    private float updateTimer;

    private void Start()
    {
        // Hook up button listeners
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshDisplay);
        }

        if (generatePromptButton != null)
        {
            generatePromptButton.onClick.AddListener(GenerateAIPrompt);
        }

        if (clearHistoryButton != null)
        {
            clearHistoryButton.onClick.AddListener(ClearHistory);
        }

        // Initial display
        RefreshDisplay();
    }

    private void Update()
    {
        // Auto-update if enabled
        if (autoUpdateInterval > 0)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= autoUpdateInterval)
            {
                updateTimer = 0f;
                RefreshDisplay();
            }
        }
    }

    /// <summary>
    /// Refresh the history display
    /// </summary>
    public void RefreshDisplay()
    {
        if (historyText == null) return;

        string summary = BuildHistoryManager.Instance.GenerateSummary();
        historyText.text = summary;
    }

    /// <summary>
    /// Generate and display AI prompt
    /// </summary>
    public void GenerateAIPrompt()
    {
        if (aiPromptText == null) return;

        string prompt = BuildHistoryManager.Instance.GenerateAIPrompt();
        aiPromptText.text = prompt;

        // Also copy to clipboard for easy use
        GUIUtility.systemCopyBuffer = prompt;
        Debug.Log("AI Prompt copied to clipboard!");
    }

    /// <summary>
    /// Export history as JSON
    /// </summary>
    public void ExportJSON()
    {
        string json = BuildHistoryManager.Instance.ExportToJSON();

        // Copy to clipboard
        GUIUtility.systemCopyBuffer = json;
        Debug.Log("Build history JSON copied to clipboard!");

        // Also log to console
        Debug.Log("Build History JSON:\n" + json);
    }

    /// <summary>
    /// Clear all history
    /// </summary>
    public void ClearHistory()
    {
        BuildHistoryManager.Instance.ClearHistory();
        RefreshDisplay();
    }
}
