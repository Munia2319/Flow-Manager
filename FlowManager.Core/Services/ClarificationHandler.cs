using System;
using System.Collections.Generic;
using FlowManager.Core.Models;

namespace FlowManager.Core.Services
{
    /// <summary>
    /// Handles clarification requests when the orchestrator has low confidence.
    /// Manages the clarification flow and tracks pending clarifications.
    /// </summary>
    public class ClarificationHandler
    {
        /// <summary>
        /// Checks if orchestrator output indicates clarification is needed.
        /// Returns clarification details if needed, null otherwise.
        /// </summary>
        public static ClarificationRequest? CheckNeedsClarification(Dictionary<string, object> orchestratorOutputs)
        {
            // Check if orchestrator flagged this as needing clarification
            var needsClarification = false;
            if (orchestratorOutputs.ContainsKey("needs_clarification"))
            {
                if (orchestratorOutputs["needs_clarification"] is bool needsClar)
                {
                    needsClarification = needsClar;
                }
            }

            // Get confidence score
            var confidence = 100;
            if (orchestratorOutputs.ContainsKey("confidence"))
            {
                if (orchestratorOutputs["confidence"] is int conf)
                {
                    confidence = conf;
                }
            }

            // Get clarification question
            var clarificationQuestion = "";
            if (orchestratorOutputs.ContainsKey("clarification_question"))
            {
                clarificationQuestion = orchestratorOutputs["clarification_question"]?.ToString() ?? "";
            }

            // Get reasoning
            var reasoning = "";
            if (orchestratorOutputs.ContainsKey("reasoning"))
            {
                reasoning = orchestratorOutputs["reasoning"]?.ToString() ?? "";
            }

            // Determine if clarification is needed
            // Either explicitly flagged OR confidence below threshold
            if (needsClarification || confidence < 70)
            {
                // Generate fallback question if orchestrator didn't provide one
                if (string.IsNullOrEmpty(clarificationQuestion))
                {
                    clarificationQuestion = GenerateFallbackQuestion(confidence, reasoning);
                }

                return new ClarificationRequest
                {
                    Confidence = confidence,
                    Question = clarificationQuestion,
                    Reasoning = reasoning,
                    RequestedAt = DateTime.UtcNow
                };
            }

            return null; // No clarification needed
        }

        /// <summary>
        /// Generates a fallback clarification question when orchestrator doesn't provide one.
        /// </summary>
        private static string GenerateFallbackQuestion(int confidence, string reasoning)
        {
            if (confidence < 30)
            {
                return "I'm not sure I understood your request. Could you please rephrase or provide more details about what you'd like me to help with?";
            }
            else if (confidence < 50)
            {
                return "I need a bit more information to help you effectively. Could you clarify what specific information or action you're looking for?";
            }
            else // 50-69
            {
                return "To make sure I help you correctly, could you provide a bit more detail about your request?";
            }
        }

        /// <summary>
        /// Creates a clarification message to add to the conversation.
        /// This represents the system asking for clarification.
        /// </summary>
        public static ConversationMessage CreateClarificationMessage(ClarificationRequest request)
        {
            return new ConversationMessage
            {
                Role = "assistant",
                Content = request.Question,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["IsClarificationRequest"] = true,
                    ["Confidence"] = request.Confidence,
                    ["Reasoning"] = request.Reasoning,
                    ["RequestedAt"] = request.RequestedAt
                }
            };
        }

        /// <summary>
        /// Checks if the most recent assistant message is a pending clarification.
        /// </summary>
        public static bool HasPendingClarification(ConversationMemory conversation)
        {
            if (conversation == null || conversation.Messages.Count == 0)
                return false;

            // Check last assistant message
            for (int i = conversation.Messages.Count - 1; i >= 0; i--)
            {
                var msg = conversation.Messages[i];

                if (msg.Role == "assistant")
                {
                    // Check if this is a clarification request
                    if (msg.Metadata.ContainsKey("IsClarificationRequest") &&
                        msg.Metadata["IsClarificationRequest"] is bool isClarification &&
                        isClarification)
                    {
                        return true;
                    }
                    // Found non-clarification assistant message, no pending clarification
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Formats a confidence score into a user-friendly display.
        /// </summary>
        public static string FormatConfidenceLevel(int confidence)
        {
            if (confidence >= 90)
                return "Very High";
            else if (confidence >= 70)
                return "High";
            else if (confidence >= 50)
                return "Medium";
            else if (confidence >= 30)
                return "Low";
            else
                return "Very Low";
        }

        /// <summary>
        /// Gets a color indicator for confidence level (for UI).
        /// </summary>
        public static string GetConfidenceColor(int confidence)
        {
            if (confidence >= 90)
                return "success"; // Green
            else if (confidence >= 70)
                return "info"; // Blue
            else if (confidence >= 50)
                return "warning"; // Yellow
            else
                return "danger"; // Red
        }
    }

    /// <summary>
    /// Represents a clarification request from the system.
    /// </summary>
    public class ClarificationRequest
    {
        /// <summary>
        /// Confidence score (0-100) that triggered the clarification
        /// </summary>
        public int Confidence { get; set; }

        /// <summary>
        /// The clarification question to ask the user
        /// </summary>
        public string Question { get; set; } = "";

        /// <summary>
        /// Reasoning for why clarification is needed
        /// </summary>
        public string Reasoning { get; set; } = "";

        /// <summary>
        /// When this clarification was requested
        /// </summary>
        public DateTime RequestedAt { get; set; }
    }
}
