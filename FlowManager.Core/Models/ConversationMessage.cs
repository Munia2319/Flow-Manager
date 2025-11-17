using System;
using System.Collections.Generic;

namespace FlowManager.Core.Models
{
    /// <summary>
    /// Conversation Message - Represents a single message in a conversation with rich metadata support
    /// </summary>
    /// <remarks>
    /// <para>
    /// ConversationMessage is the atomic unit of conversation storage. It supports messages from
    /// users, assistants, and system (summaries), with extensive metadata for tracking routing,
    /// topic continuity, confidence, PII redaction, and more.
    /// </para>
    /// <para>
    /// <b>Message Roles:</b>
    /// <list type="bullet">
    ///   <item><description><b>user:</b> User's questions/input</description></item>
    ///   <item><description><b>assistant:</b> AI system's responses</description></item>
    ///   <item><description><b>system:</b> System-generated summaries of older conversation segments</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Metadata Categories:</b>
    /// <list type="bullet">
    ///   <item><description><b>Routing:</b> triggeredDepartment, routingConfidence</description></item>
    ///   <item><description><b>Summarization:</b> IsSummary, SummarizedMessageCount, TokensSaved</description></item>
    ///   <item><description><b>Topic Continuity:</b> TopicSimilarity, IsTopicChange, TopicSegmentId</description></item>
    ///   <item><description><b>Confidence:</b> Confidence, IsClarificationRequest, Reasoning</description></item>
    ///   <item><description><b>Privacy:</b> PIIRedacted, PIITypes, RedactionStrategy</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class ConversationMessage
    {
        /// <summary>
        /// Message role: "user", "assistant", or "system"
        /// </summary>
        public string Role { get; set; } = "";

        /// <summary>
        /// Message content/text
        /// </summary>
        /// <remarks>
        /// User: "What are forklift safety procedures?"
        /// <br/>
        /// Assistant: "Key forklift safety procedures include..."
        /// <br/>
        /// System: "SUMMARY: User asked about forklift safety (Data dept), then battery charging..."
        /// </remarks>
        public string Content { get; set; } = "";

        /// <summary>
        /// Timestamp when this message was created (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // OPTIONAL METADATA - Extra information about this message
        // Common metadata keys:
        // - "triggeredDepartment": Which department handled this (e.g., "Data", "Search")
        // - "routingConfidence": How confident was the routing decision
        // - "tokenCount": Estimated token count for this message
        //
        // ✨ Phase 3 - Summary-specific metadata:
        // - "IsSummary": true if this is a summary message
        // - "SummarizedMessageCount": Number of original messages condensed
        // - "OriginalTokenCount": Total tokens before summarization
        // - "TokensSaved": Tokens saved by summarization
        // - "MessageRange": e.g., "1-10" indicating which messages were summarized
        //
        // ✨ Phase 4 - Topic continuity metadata:
        // - "TopicSimilarity": 0.0-1.0 score indicating similarity to previous messages
        // - "IsTopicChange": true if this message changes the topic
        // - "TopicDetectionMethod": "keyword", "embedding", "keyword-fallback"
        // - "TopicSegmentId": ID grouping messages about same topic
        //
        // ✨ Phase 5 - Confidence and clarification metadata:
        // - "Confidence": 0-100 score for routing confidence
        // - "IsClarificationRequest": true if this message is asking for clarification
        // - "Reasoning": Orchestrator's reasoning for the decision
        //
        // ✨ Phase 6 - Privacy and PII redaction metadata:
        // - "PIIRedacted": true if PII was detected and redacted
        // - "PIITypes": List of PII types found (Email, Phone, SSN, etc.)
        // - "PIICount": Number of PII instances redacted
        // - "RedactionStrategy": Strategy used (Mask, Hash, Remove, Tokenize)
        // - "RedactionSummary": Human-readable summary of what was redacted
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Checks if this message is a conversation summary
        /// </summary>
        /// <returns>True if role is "system" and IsSummary metadata is true</returns>
        public bool IsSummary =>
            Role == "system" &&
            Metadata.ContainsKey("IsSummary") &&
            Metadata["IsSummary"] is bool isSummary &&
            isSummary;

        /// <summary>
        /// Factory method to create a properly formatted summary message
        /// </summary>
        /// <param name="summaryContent">The condensed summary text</param>
        /// <param name="summarizedMessageCount">Number of messages this summary represents</param>
        /// <param name="originalTokenCount">Total tokens of original messages</param>
        /// <param name="newTokenCount">Tokens in the summary</param>
        /// <returns>ConversationMessage configured as a summary with metadata</returns>
        public static ConversationMessage CreateSummary(
            string summaryContent,
            int summarizedMessageCount,
            int originalTokenCount,
            int newTokenCount)
        {
            var tokensSaved = originalTokenCount - newTokenCount;

            return new ConversationMessage
            {
                Role = "system",
                Content = summaryContent,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["IsSummary"] = true,
                    ["SummarizedMessageCount"] = summarizedMessageCount,
                    ["OriginalTokenCount"] = originalTokenCount,
                    ["NewTokenCount"] = newTokenCount,
                    ["TokensSaved"] = tokensSaved
                }
            };
        }

        /// <summary>
        /// Gets estimated token count for this message (4 chars/token heuristic)
        /// </summary>
        /// <returns>Estimated tokens (checks metadata cache first, then estimates)</returns>
        public int GetEstimatedTokenCount()
        {
            if (string.IsNullOrEmpty(Content))
                return 0;

            // Check if token count is cached in metadata
            if (Metadata.ContainsKey("tokenCount") && Metadata["tokenCount"] is int cachedCount)
                return cachedCount;

            // Estimate: 1 token ≈ 4 characters + overhead
            return (Content.Length / 4) + 10;
        }

        // ✨ Phase 4: Helper property to check if this is a topic change
        public bool IsTopicChange =>
            Metadata.ContainsKey("IsTopicChange") &&
            Metadata["IsTopicChange"] is bool isChange &&
            isChange;

        // ✨ Phase 4: Get topic similarity score (0.0-1.0)
        public double GetTopicSimilarity()
        {
            if (Metadata.ContainsKey("TopicSimilarity"))
            {
                if (Metadata["TopicSimilarity"] is double score)
                    return score;
                if (Metadata["TopicSimilarity"] is int intScore)
                    return intScore;
            }
            return 1.0; // Default to same topic if no info
        }

        // ✨ Phase 4: Get topic segment ID (groups related messages)
        public string? GetTopicSegmentId()
        {
            if (Metadata.ContainsKey("TopicSegmentId") && Metadata["TopicSegmentId"] is string segmentId)
                return segmentId;
            return null;
        }

        // ✨ Phase 5: Helper property to check if this is a clarification request
        public bool IsClarificationRequest =>
            Metadata.ContainsKey("IsClarificationRequest") &&
            Metadata["IsClarificationRequest"] is bool isClarification &&
            isClarification;

        // ✨ Phase 5: Get confidence score (0-100)
        public int GetConfidence()
        {
            if (Metadata.ContainsKey("Confidence"))
            {
                if (Metadata["Confidence"] is int confidence)
                    return confidence;
            }
            return 100; // Default to high confidence if not specified
        }

        // ✨ Phase 5: Get reasoning for decision
        public string GetReasoning()
        {
            if (Metadata.ContainsKey("Reasoning") && Metadata["Reasoning"] is string reasoning)
                return reasoning;
            return "";
        }

        // ✨ Phase 6: Helper property to check if PII was redacted
        public bool HasPIIRedacted =>
            Metadata.ContainsKey("PIIRedacted") &&
            Metadata["PIIRedacted"] is bool redacted &&
            redacted;

        // ✨ Phase 6: Get PII redaction summary
        public string GetPIISummary()
        {
            if (Metadata.ContainsKey("RedactionSummary") && Metadata["RedactionSummary"] is string summary)
                return summary;
            return "";
        }

        // ✨ Phase 6: Get number of PII instances redacted
        public int GetPIICount()
        {
            if (Metadata.ContainsKey("PIICount") && Metadata["PIICount"] is int count)
                return count;
            return 0;
        }

        // ✨ Phase 6: Get redaction strategy used
        public string GetRedactionStrategy()
        {
            if (Metadata.ContainsKey("RedactionStrategy") && Metadata["RedactionStrategy"] is string strategy)
                return strategy;
            return "";
        }
    }
}
