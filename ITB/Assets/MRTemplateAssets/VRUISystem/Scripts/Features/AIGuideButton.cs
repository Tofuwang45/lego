using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Templates.MR;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Button to trigger AI assistance for building suggestions
    /// </summary>
    public class AIGuideButton : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The button component")]
        public Button guideButton;

        [Tooltip("Button label text")]
        public TextMeshProUGUI buttonLabel;

        [Tooltip("Loading indicator")]
        public GameObject loadingIndicator;

        [Header("API Settings")]
        [Tooltip("Your AI API endpoint (if using custom API)")]
        public string apiEndpoint = "";

        [Tooltip("API key for authentication")]
        public string apiKey = "";

        private bool isProcessing = false;

        private void Start()
        {
            if (guideButton != null)
            {
                guideButton.onClick.AddListener(OnGuideButtonClicked);
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }
        }

        private void OnGuideButtonClicked()
        {
            if (isProcessing) return;

            TriggerAIGuide();
        }

        /// <summary>
        /// Trigger the AI guide functionality
        /// </summary>
        public async void TriggerAIGuide()
        {
            isProcessing = true;

            // Show loading state
            SetLoadingState(true);

            try
            {
                // Get current scene context
                string sceneContext = GatherSceneContext();

                // Call AI API
                string aiResponse = await CallAIAPI(sceneContext);

                // Process AI response
                ProcessAIResponse(aiResponse);

                Debug.Log($"AI Guide Response: {aiResponse}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AI Guide Error: {e.Message}");
            }
            finally
            {
                // Hide loading state
                SetLoadingState(false);
                isProcessing = false;
            }
        }

        private string GatherSceneContext()
        {
            // Gather information about the current build
            string context = "Current VR Lego Build Context:\n";

            // Get block usage statistics
            if (BlockUsageTracker.Instance != null)
            {
                int totalBlocks = BlockUsageTracker.Instance.GetTotalBlockCount();
                context += $"Total blocks placed: {totalBlocks}\n";

                var topBlocks = BlockUsageTracker.Instance.GetTopUsedBlocks(5);
                context += "Most used blocks:\n";
                foreach (var usage in topBlocks)
                {
                    context += $"  - {usage.count}x {usage.GetColorName()} {usage.blockName}\n";
                }
            }

            // Get spawned objects information
            var spawnedObjectsManager = FindObjectOfType<SpawnedObjectsManager>();
            int objectCount = spawnedObjectsManager != null ? spawnedObjectsManager.gameObject.transform.childCount : 0;
            context += $"\nNumber of objects in scene: {objectCount}\n";

            return context;
        }

        private async System.Threading.Tasks.Task<string> CallAIAPI(string context)
        {
            // This is a placeholder for actual AI API integration
            // You would implement your specific API call here

            // Example: Using UnityWebRequest to call an API
            /*
            using (UnityWebRequest request = new UnityWebRequest(apiEndpoint, "POST"))
            {
                string jsonData = JsonUtility.ToJson(new { prompt = context });
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    throw new System.Exception($"API Error: {request.error}");
                }
            }
            */

            // For now, return a mock response
            await System.Threading.Tasks.Task.Delay(1000); // Simulate API delay

            return "AI Suggestion: Based on your current build with multiple red and blue bricks, " +
                   "consider adding some yellow accent pieces to create visual contrast. " +
                   "A 2x4 yellow plate on top would work well!";
        }

        private void ProcessAIResponse(string response)
        {
            // Display the AI response to the user
            // You could create a popup panel, speech bubble, or notification

            Debug.Log($"AI Guide says: {response}");

            // Example: Show a temporary notification
            ShowNotification(response);
        }

        private void ShowNotification(string message)
        {
            // Placeholder for notification display
            // You would implement a proper notification UI here

            Debug.Log($"[AI Guide Notification] {message}");

            // Could trigger a UI panel to show the message
            // Or use a text-to-speech system in VR
        }

        private void SetLoadingState(bool loading)
        {
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(loading);
            }

            if (guideButton != null)
            {
                guideButton.interactable = !loading;
            }

            if (buttonLabel != null && loading)
            {
                buttonLabel.text = "Processing...";
            }
            else if (buttonLabel != null)
            {
                buttonLabel.text = "AI Guide";
            }
        }

        private void OnDestroy()
        {
            if (guideButton != null)
            {
                guideButton.onClick.RemoveListener(OnGuideButtonClicked);
            }
        }
    }
}
