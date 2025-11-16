using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;
using TMPro;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class APICallInstruction : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
    private string apiKey = "";

    [Header("AI Prompt Settings")]
    [SerializeField] private string systemPrompt = "You are a helpful assistant tasked with decoding a set of actions into instructions. Given a set of actions detailing what parts are connected to what, you will detail how to assemble the parts step by step. For example, given actions ('pink_block -> light_red_block') and a corresponding coordinate pair, you will respond with instructions like '1. Attach a pink block to the right of a light red block.' Ensure clarity and conciseness in your instructions.";
    
    [Header("Text Input")]
    [SerializeField] private TextMeshProUGUI inputTextObject;

    [Header("Output Settings")]
    [SerializeField] private TextMeshProUGUI outputTextObject;
    [SerializeField] private string outputFileName = "ai_response.txt";
    [SerializeField] private bool autoSaveOutput = true;

    [Header("Response")]
    [SerializeField] private string lastResponse = "";
    
    private bool isWaitingForResponse = false;

    private void Awake()
    {
        LoadApiKeyFromEnv();
    }

    private void LoadApiKeyFromEnv()
    {
        string envFilePath = Path.Combine(Application.persistentDataPath, "..", "..", ".env");
        
        // Try multiple possible paths
        string[] possiblePaths = new string[]
        {
            Path.Combine(Application.dataPath, "..", ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            envFilePath
        };

        foreach (string path in possiblePaths)
        {
            Debug.Log("[ENV] Checking path: " + path);
            if (File.Exists(path))
            {
                Debug.Log("[ENV] File found at: " + path);
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    // Look for GEMINI_API_KEY instead of OPENAI_API_KEY
                    if (line.StartsWith("GEMINI_API_KEY="))
                    {
                        apiKey = line.Replace("GEMINI_API_KEY=", "").Trim();
                        Debug.Log("[ENV] Gemini API Key loaded successfully. Key starts with: " + apiKey.Substring(0, Math.Min(5, apiKey.Length)) + "...");
                        return;
                    }
                }
                Debug.LogWarning("[ENV] GEMINI_API_KEY not found in file at: " + path);
            }
            else
            {
                Debug.Log("[ENV] File not found at: " + path);
            }
        }

        Debug.LogError("[ENV] Could not find .env file or GEMINI_API_KEY not set!");
    }

    public void CallAI()
    {
        if (isWaitingForResponse)
        {
            Debug.LogWarning("Already waiting for a response from the API.");
            return;
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API Key is not set!");
            return;
        }

        if (inputTextObject == null)
        {
            Debug.LogError("Input text object is not assigned!");
            return;
        }

        string userMessage = inputTextObject.text;
        if (string.IsNullOrEmpty(userMessage))
        {
            Debug.LogWarning("Input text is empty!");
            return;
        }

        Debug.Log("[API CALL] Calling API with user message: " + userMessage);
        StartCoroutine(SendAPIRequest(userMessage));
    }

    private IEnumerator SendAPIRequest(string userMessage)
    {
        isWaitingForResponse = true;

        // Combine system prompt with user message for Gemini
        string combinedPrompt = systemPrompt + "\n\nUser request: " + userMessage;

        // Create the request payload for Gemini
        GeminiRequestPayload payload = new GeminiRequestPayload
        {
            contents = new Content[]
            {
                new Content
                {
                    parts = new Part[]
                    {
                        new Part { text = combinedPrompt }
                    }
                }
            }
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        // Gemini uses API key as URL parameter
        string urlWithKey = apiEndpoint + "?key=" + apiKey;

        using (UnityWebRequest request = new UnityWebRequest(urlWithKey, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("API Response received successfully");
                ProcessResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("API Error: " + request.error);
                Debug.LogError("Response: " + request.downloadHandler.text);
                lastResponse = "Error: " + request.error;
            }
        }

        isWaitingForResponse = false;
    }

    private void ProcessResponse(string responseText)
    {
        try
        {
            GeminiResponsePayload response = JsonUtility.FromJson<GeminiResponsePayload>(responseText);
            
            if (response.candidates != null && response.candidates.Length > 0 &&
                response.candidates[0].content != null &&
                response.candidates[0].content.parts != null &&
                response.candidates[0].content.parts.Length > 0)
            {
                lastResponse = response.candidates[0].content.parts[0].text;
                Debug.Log("[API OUTPUT] AI Response: " + lastResponse);
                
                // Display output if output text object is assigned
                if (outputTextObject != null)
                {
                    outputTextObject.text = lastResponse;
                    Debug.Log("[API OUTPUT] Response displayed in UI");
                }
                
                // Auto-save output if enabled
                if (autoSaveOutput)
                {
                    SaveOutputToFile(lastResponse);
                    Debug.Log("[API OUTPUT] Response auto-saved to file");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse API response: " + e.Message);
            Debug.LogError("Raw response: " + responseText);
            lastResponse = "Error parsing response";
        }
    }

    private void SaveOutputToFile(string content)
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, outputFileName);
            File.WriteAllText(filePath, content);
            Debug.Log("Output saved to: " + filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save output file: " + e.Message);
        }
    }

    public string GetLastResponse()
    {
        return lastResponse;
    }

    public void SetInputText(string text)
    {
        if (inputTextObject != null)
        {
            inputTextObject.text = text;
        }
        else
        {
            Debug.LogError("Input text object is not assigned!");
        }
    }

    public void SetSystemPrompt(string prompt)
    {
        systemPrompt = prompt;
    }

    public void SaveOutputManually()
    {
        if (string.IsNullOrEmpty(lastResponse))
        {
            Debug.LogWarning("No response to save!");
            return;
        }
        SaveOutputToFile(lastResponse);
    }

    // JSON Serialization classes for Gemini API
    [System.Serializable]
    private class Part
    {
        public string text;
    }

    [System.Serializable]
    private class Content
    {
        public Part[] parts;
    }

    [System.Serializable]
    private class GeminiRequestPayload
    {
        public Content[] contents;
    }

    [System.Serializable]
    private class Candidate
    {
        public Content content;
        public string finishReason;
        public int index;
    }

    [System.Serializable]
    private class SafetyRating
    {
        public string category;
        public string probability;
    }

    [System.Serializable]
    private class GeminiResponsePayload
    {
        public Candidate[] candidates;
        public PromptFeedback promptFeedback;
    }

    [System.Serializable]
    private class PromptFeedback
    {
        public SafetyRating[] safetyRatings;
    }
}