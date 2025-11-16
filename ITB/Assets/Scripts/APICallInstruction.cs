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
    [SerializeField] private string apiEndpoint = "https://api.openai.com/v1/chat/completions";
    private string apiKey = "";

    [Header("AI Prompt Settings")]
    [SerializeField] private string systemPrompt = "You are a helpful assistant tasked with decoding a set of actions into insturctions. Given a set of actions detailing what parts are connected to what, you will detail how to assemble the parts step by step. For example, given actions (\'pink_block -> light_red_block\') and a corresponding coordinate pair, you will respond with instructions like \'1. Attach a pink block to the right of a light red block.\' Ensure clarity and conciseness in your instructions.";
    
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
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    if (line.StartsWith("OPENAI_API_KEY="))
                    {
                        apiKey = line.Replace("OPENAI_API_KEY=", "").Trim();
                        Debug.Log("API Key loaded from .env file");
                        return;
                    }
                }
            }
        }

        Debug.LogWarning("Could not find .env file or OPENAI_API_KEY not set!");
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

        StartCoroutine(SendAPIRequest(userMessage));
    }

    private IEnumerator SendAPIRequest(string userMessage)
    {
        isWaitingForResponse = true;

        // Create the request payload
        RequestPayload payload = new RequestPayload
        {
            model = "gpt-3.5-turbo",
            messages = new Message[]
            {
                new Message { role = "system", content = systemPrompt },
                new Message { role = "user", content = userMessage }
            },
            temperature = 0.7f,
            max_tokens = 500
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(apiEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

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
            ResponsePayload response = JsonUtility.FromJson<ResponsePayload>(responseText);
            
            if (response.choices != null && response.choices.Length > 0)
            {
                lastResponse = response.choices[0].message.content;
                Debug.Log("AI Response: " + lastResponse);
                
                // Display output if output text object is assigned
                if (outputTextObject != null)
                {
                    outputTextObject.text = lastResponse;
                }
                
                // Auto-save output if enabled
                if (autoSaveOutput)
                {
                    SaveOutputToFile(lastResponse);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse API response: " + e.Message);
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

    // JSON Serialization classes for OpenAI API
    [System.Serializable]
    private class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    private class RequestPayload
    {
        public string model;
        public Message[] messages;
        public float temperature;
        public int max_tokens;
    }

    [System.Serializable]
    private class Choice
    {
        public Message message;
        public int index;
        public string finish_reason;
    }

    [System.Serializable]
    private class ResponsePayload
    {
        public string id;
        public string @object;
        public int created;
        public string model;
        public Choice[] choices;
        public Usage usage;
    }

    [System.Serializable]
    private class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }
}
