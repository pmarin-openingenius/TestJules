using System.Threading.Tasks;
using Uralstech.UGemini;
using Uralstech.UGemini.Models;
using UnityEngine; // Required for Debug.Log

namespace GeminiIntegrationTool
{
    public class GeminiApiClient
    {
        /// <summary>
        /// Validates the Gemini API key by attempting to list available models.
        /// </summary>
        /// <param name="apiKey">The Gemini API key to validate.</param>
        /// <returns>True if the API key is valid, false otherwise.</returns>
        public static async Task<bool> IsApiKeyValid(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError("API Key is null or empty.");
                return false;
            }

            try
            {
                // Initialize the Gemini client with the provided API key.
                // The UGemini library typically requires initialization, often via a settings object or static setup.
                // Assuming UGemini automatically picks up a globally set API key or has an initialization method.
                // For UGemini, initialization is done by creating a GeminiClient instance.
                var geminiClient = new GeminiClient(apiKey);

                // Make an API call to list available models.
                // The exact method might vary based on the UGemini library's API.
                // From the UGemini README, it supports 'models' endpoint with a 'list' method.
                // Let's assume it's something like geminiClient.Models.ListAsync().
                ListModelsResponse response = await geminiClient.Models.ListAsync();

                if (response != null && response.Models != null && response.Models.Count > 0)
                {
                    Debug.Log("API Key is valid. Successfully listed models.");
                    // Optionally, log the models found
                    // foreach (var model in response.Models)
                    // {
                    //    Debug.Log($"Found model: {model.Name} ({model.DisplayName})");
                    // }
                    return true;
                }
                else
                {
                    Debug.LogError("API Key validation failed: No models returned or empty response.");
                    return false;
                }
            }
            catch (GeminiException ex) // Catching a specific Gemini exception if available, or a general one.
            {
                // Log the specific error from the Gemini API.
                Debug.LogError($"API Key validation failed: Gemini API Error - {ex.Message}");
                // It's good to log ex.ToString() for more details during development.
                // Debug.LogError(ex.ToString());
                return false;
            }
            catch (System.Exception ex)
            {
                // Catch any other exceptions (network issues, etc.)
                Debug.LogError($"API Key validation failed: An unexpected error occurred - {ex.Message}");
                // Debug.LogError(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Sends a text message to the Gemini API and returns the response.
        /// </summary>
        /// <param name="apiKey">The Gemini API key.</param>
        /// <param name="textMessage">The text message to send.</param>
        /// <returns>The generated text response from Gemini, or null if an error occurred.</returns>
        public static async Task<string> SendMessageAsync(string apiKey, string textMessage)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError("API Key is null or empty. Cannot send message.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(textMessage))
            {
                Debug.LogError("Text message is null or empty. Cannot send message.");
                return null;
            }

            try
            {
                // Initialize the Gemini client with the API key and a default model for text generation.
                // "gemini-pro" is a common model for text generation.
                // UGemini allows specifying the model in the constructor or per request.
                // For simplicity, we'll specify it here.
                var geminiClient = new GeminiClient(apiKey, model: "gemini-pro");

                // Construct the request. For simple text, a direct string prompt is often sufficient.
                // The UGemini library's GenerateContentAsync method is suitable here.
                GenerateContentResponse response = await geminiClient.GenerateContentAsync(textMessage);

                if (response != null && response.Candidates != null && response.Candidates.Count > 0)
                {
                    // Assuming the first candidate contains the primary response.
                    // And that content is Parts, and the first Part is text.
                    if (response.Candidates[0].Content != null && response.Candidates[0].Content.Parts != null && response.Candidates[0].Content.Parts.Count > 0)
                    {
                        // Concatenate text from all parts, as some responses might be split.
                        System.Text.StringBuilder fullTextResponse = new System.Text.StringBuilder();
                        foreach(var part in response.Candidates[0].Content.Parts)
                        {
                            if (!string.IsNullOrEmpty(part.Text))
                            {
                                fullTextResponse.Append(part.Text);
                            }
                        }
                        if (fullTextResponse.Length > 0)
                        {
                             Debug.Log("Successfully received response from Gemini.");
                            return fullTextResponse.ToString();
                        }
                        else
                        {
                            Debug.LogWarning("Gemini response candidate part was empty or not text.");
                            return "Error: Received an empty or non-text response part from Gemini.";
                        }
                    }
                }

                Debug.LogWarning("No valid response content received from Gemini.");
                return "Error: No valid response content received from Gemini.";
            }
            catch (GeminiException ex)
            {
                Debug.LogError($"Gemini API Error sending message: {ex.Message} (Code: {ex.ErrorCode}, Status: {ex.Status})");
                // Debug.LogError(ex.ToString());
                return $"Error: Gemini API - {ex.Message}";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"An unexpected error occurred sending message: {ex.Message}");
                // Debug.LogError(ex.ToString());
                return $"Error: Unexpected - {ex.Message}";
            }
        }
    }
}
