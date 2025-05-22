using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// Define ResponseType Enum
public enum ResponseType
{
    Text,
    Image,
    Voice
}

// Define ResponseData Struct
public struct ResponseData
{
    public ResponseType Type;
    public string TextContent;
    public UnityEngine.Texture2D ImageContent; // For future use
    public string VoiceContentPath; // For future use
    public string OriginalQuery; // To show what query this response was for

    // Constructor for text responses
    public ResponseData(string query, string text)
    {
        Type = ResponseType.Text;
        TextContent = text;
        ImageContent = null;
        VoiceContentPath = null;
        OriginalQuery = query;
    }

    // Add more constructors as needed for Image/Voice later
}

public class GeminiEditorWindow : EditorWindow
{
    private string apiKey = "";
    private string textInput = "";
    private UnityEngine.Object fileInput = null;
    private List<ResponseData> responses = new List<ResponseData>();
    private Vector2 scrollPosition;

    void OnEnable()
    {
        // Load the API key from EditorPrefs when the window is enabled
        apiKey = EditorPrefs.GetString("GeminiApiKey", "");
    }

    [MenuItem("Tools/Gemini Integration")]
    public static void ShowWindow()
    {
        GetWindow<GeminiEditorWindow>("Gemini Integration");
    }

    void OnGUI()
    {
        GUILayout.Label("Gemini API Key:", EditorStyles.boldLabel);
        apiKey = EditorGUILayout.TextField("API Key", apiKey);

        if (GUILayout.Button("Save API Key"))
        {
            EditorPrefs.SetString("GeminiApiKey", apiKey);
            Debug.Log("Gemini API Key saved.");
        }

        EditorGUILayout.Space(); // Add some space

        GUILayout.Label("Your Message:", EditorStyles.boldLabel);
        textInput = EditorGUILayout.TextArea(textInput, GUILayout.Height(100));

        fileInput = EditorGUILayout.ObjectField("Attach File (Optional):", fileInput, typeof(UnityEngine.Object), false);

        if (GUILayout.Button("Record Voice (Placeholder)"))
        {
            // Placeholder for voice recording functionality
            Debug.Log("Record Voice button clicked (Placeholder).");
        }

        if (GUILayout.Button("Send"))
        {
            Debug.Log("Send button clicked. Text: " + textInput + (fileInput != null ? " File: " + fileInput.name : ""));

            string currentQuery = textInput;
            string mockText = "Mock Response for query: '" + currentQuery + "'\n";
            mockText += "You typed: " + textInput;
            if (fileInput != null)
            {
                mockText += "\nFile attached: " + fileInput.name;
            }
            responses.Add(new ResponseData(currentQuery, mockText));
            textInput = ""; // Clear the text input field
            // fileInput = null; // Optionally clear the file input as well, if desired
        }

        EditorGUILayout.Space(); // Visual separation

        GUILayout.Label("Responses:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        for (int i = 0; i < responses.Count; i++)
        {
            ResponseData responseItem = responses[i];
            GUILayout.Label("Original Query: " + responseItem.OriginalQuery, EditorStyles.boldLabel);
            EditorGUILayout.TextArea(responseItem.TextContent, EditorStyles.textArea, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(150)); // Read-only
            
            if (GUILayout.Button("Copy Response " + (i + 1)))
            {
                GUIUtility.systemCopyBuffer = responseItem.TextContent;
                Debug.Log("Copied to clipboard: " + responseItem.TextContent);
            }
            EditorGUILayout.Separator(); // Separator after each response
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Clear Responses"))
        {
            responses.Clear();
            Debug.Log("Responses cleared.");
        }
    }
}
