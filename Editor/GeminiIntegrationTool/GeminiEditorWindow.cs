using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic; // Required for List

namespace GeminiIntegrationTool
{
    // Enum to define the type of response (Text, Image, etc.)
    public enum ResponseType
    {
        Text,
        Image,
        // Add other types as needed
    }

    // Struct to hold response data
    public struct ResponseData
    {
        public string OriginalQuery; // To store the query that generated this response
        public ResponseType Type;
        public string TextContent;
        public Texture2D ImageContent; // For image responses
        // Add other content fields as needed

        public ResponseData(string query, string text, ResponseType type = ResponseType.Text)
        {
            OriginalQuery = query;
            Type = type;
            TextContent = text;
            ImageContent = null; // Initialize image content to null
        }

        public ResponseData(string query, Texture2D image, ResponseType type = ResponseType.Image)
        {
            OriginalQuery = query;
            Type = type;
            TextContent = string.Empty; // Initialize text content to empty
            ImageContent = image;
        }
    }

    public class GeminiEditorWindow : EditorWindow
    {
        private enum ApiKeyStatus { Unknown, Valid, Invalid, Validating }
        private ApiKeyStatus apiKeyStatus = ApiKeyStatus.Unknown;
        private string apiKey = "";
        private string validationMessage = "";
        private MessageType messageType = MessageType.None;

        private const string ApiKeyPrefName = "GeminiApiKey";

        // Fields for message sending UI
        private string textInput = "";
        private UnityEngine.Object fileInput = null; // Using UnityEngine.Object for flexibility
        private List<ResponseData> responses = new List<ResponseData>();
        private Vector2 scrollPosition;
        private bool isSendingMessage = false; // For loading indicator

        [MenuItem("Tools/Gemini Integration")]
        public static void ShowWindow()
        {
            GetWindow<GeminiEditorWindow>("Gemini Integration");
        }

        private void OnEnable()
        {
            apiKey = EditorPrefs.GetString(ApiKeyPrefName, "");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                apiKeyStatus = ApiKeyStatus.Validating;
                ValidateApiKeyAsync(apiKey);
            }
            else
            {
                apiKeyStatus = ApiKeyStatus.Unknown;
            }
            // Initialize responses list if it's null (e.g., after script recompile)
            if (responses == null)
            {
                responses = new List<ResponseData>();
            }
        }

        void OnGUI()
        {
            // --- API Key Section ---
            GUILayout.Label("Gemini API Key Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Enter your Gemini API key below. This key will be used to authenticate with the Gemini API.", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            apiKey = EditorGUILayout.PasswordField("API Key:", apiKey);
            DisplayValidationIcon();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            if (!string.IsNullOrEmpty(validationMessage))
            {
                EditorGUILayout.HelpBox(validationMessage, messageType);
            }
            if (apiKeyStatus == ApiKeyStatus.Validating)
            {
                 EditorGUILayout.HelpBox("Validating API Key...", MessageType.Info);
            }
            if (GUILayout.Button("Validate & Save Key") && apiKeyStatus != ApiKeyStatus.Validating)
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    validationMessage = "API Key cannot be empty.";
                    messageType = MessageType.Error;
                    apiKeyStatus = ApiKeyStatus.Unknown;
                }
                else
                {
                    ValidateApiKeyAsync(apiKey);
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("You can generate an API key from Google AI Studio.", MessageType.None);

            EditorGUILayout.Separator(); // Separator between API Key and Message Sending UI

            // --- Message Sending Section ---
            GUILayout.Label("Send Message to Gemini", EditorStyles.boldLabel);

            // Disable input fields if API key is not valid or if sending message
            bool canSendMessage = apiKeyStatus == ApiKeyStatus.Valid && !isSendingMessage;
            EditorGUI.BeginDisabledGroup(!canSendMessage);

            GUILayout.Label("Your Message:");
            textInput = EditorGUILayout.TextArea(textInput, GUILayout.Height(100));

            // File input - Placeholder for now, as Gemini initially supports text.
            // Consider how you'll handle file types (images, etc.) later.
            fileInput = EditorGUILayout.ObjectField("Attach File (Optional):", fileInput, typeof(UnityEngine.Object), false);

            // Placeholder for voice recording
            if (GUILayout.Button("Record Voice (Placeholder)"))
            {
                Debug.Log("Voice recording feature not yet implemented.");
            }

            if (GUILayout.Button("Send") && !string.IsNullOrWhiteSpace(textInput)) // Ensure text input is not empty
            {
                isSendingMessage = true;
                Repaint(); // Show loading state immediately

                string currentQuery = textInput;
                // UnityEngine.Object currentFile = fileInput; // File input not implemented for Gemini yet

                // Important: Retrieve apiKey from EditorPrefs or the apiKey field, ensure it's the validated one.
                string currentApiKey = EditorPrefs.GetString(ApiKeyPrefName);

                // Asynchronously send the message
                SendMessageToGeminiAsync(currentApiKey, currentQuery);

                textInput = ""; // Clear input field
                // fileInput = null; // Clear file input if it was used
            }
            EditorGUI.EndDisabledGroup();

            if (isSendingMessage)
            {
                EditorGUILayout.HelpBox("Waiting for response...", MessageType.Info);
            }

            EditorGUILayout.Separator();

            // --- Responses Section ---
            GUILayout.Label("Responses:", EditorStyles.boldLabel);
            if (responses.Count == 0 && !isSendingMessage)
            {
                EditorGUILayout.HelpBox("No responses yet. Send a message to see results here.", MessageType.Info);
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            for (int i = responses.Count - 1; i >= 0; i--)
            {
                var response = responses[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("Query: " + response.OriginalQuery, EditorStyles.boldLabel);

                if (response.Type == ResponseType.Text && !string.IsNullOrEmpty(response.TextContent))
                {
                    EditorGUILayout.SelectableLabel(response.TextContent, EditorStyles.textArea, GUILayout.Height(EditorGUIUtility.singleLineHeight * 4));
                    if (GUILayout.Button("Copy Response Text", GUILayout.Width(150)))
                    {
                        EditorGUIUtility.systemCopyBuffer = response.TextContent;
                        Debug.Log("Response text copied to clipboard.");
                    }
                }
                // else if (response.Type == ResponseType.Image && response.ImageContent != null) // Image handling placeholder
                // {
                //     GUILayout.Box(response.ImageContent, GUILayout.Width(100), GUILayout.Height(100));
                // }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();

            if (responses.Count > 0)
            {
                if (GUILayout.Button("Clear Responses"))
                {
                    if (EditorUtility.DisplayDialog("Clear Responses", "Are you sure you want to clear all responses?", "Yes", "No"))
                    {
                        responses.Clear();
                        Debug.Log("Responses cleared.");
                    }
                }
            }
        }

        private void DisplayValidationIcon()
        {
            GUIContent iconContent = null;
            string tooltip = "";

            switch (apiKeyStatus)
            {
                case ApiKeyStatus.Validating:
                    iconContent = EditorGUIUtility.IconContent("d_RotateTool");
                    tooltip = "Validating...";
                    break;
                case ApiKeyStatus.Valid:
                    iconContent = EditorGUIUtility.IconContent("TestPassed");
                    tooltip = "API Key is Valid";
                    break;
                case ApiKeyStatus.Invalid:
                    iconContent = EditorGUIUtility.IconContent("TestFailed");
                    tooltip = "API Key is Invalid";
                    break;
                case ApiKeyStatus.Unknown:
                default:
                    iconContent = EditorGUIUtility.IconContent("TestIgnored");
                    tooltip = "API Key status is Unknown";
                    break;
            }

            if (iconContent != null)
            {
                iconContent.tooltip = tooltip;
                GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(20));
            }
        }

        private async void ValidateApiKeyAsync(string keyToValidate)
        {
            apiKeyStatus = ApiKeyStatus.Validating;
            validationMessage = "";
            Repaint();

            bool isValid = await GeminiApiClient.IsApiKeyValid(keyToValidate);

            if (isValid)
            {
                EditorPrefs.SetString(ApiKeyPrefName, keyToValidate);
                apiKeyStatus = ApiKeyStatus.Valid;
                validationMessage = "API Key is valid and has been saved!";
                messageType = MessageType.Info;
                Debug.Log("Gemini API Key saved successfully.");
            }
            else
            {
                apiKeyStatus = ApiKeyStatus.Invalid;
                validationMessage = "API Key validation failed. Please check the key and try again. (Check Console for more details)";
                messageType = MessageType.Error;
                Debug.LogError("Gemini API Key validation failed.");
            }
            Repaint();
        }

        private async void SendMessageToGeminiAsync(string key, string query)
        {
            // isSendingMessage is already set to true before calling this method.
            // Repaint() is also called before this method.
            try
            {
                string generatedText = await GeminiApiClient.SendMessageAsync(key, query);

                if (generatedText != null && !generatedText.StartsWith("Error:"))
                {
                    responses.Add(new ResponseData(query, generatedText));
                }
                else
                {
                    // If SendMessageAsync returns null or an error string, log it and add to responses.
                    string errorResponse = generatedText ?? "Error: Could not get a response from Gemini. Check console for details.";
                    responses.Add(new ResponseData(query, errorResponse));
                    Debug.LogError(errorResponse);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error sending message to Gemini: {ex.Message}");
                responses.Add(new ResponseData(query, $"Error: An exception occurred - {ex.Message}"));
            }
            finally
            {
                isSendingMessage = false;
                Repaint(); // Update UI to hide loading state and show new response
            }
        }
    }
}
