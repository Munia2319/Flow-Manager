using System;
using System.Collections.Generic;

namespace FlowManager.Core.Models
{
    /// <summary>
    /// Conversation Memory - Stores complete conversation history with intelligent token management
    /// </summary>
    /// <remarks>
    /// <para>
    /// ConversationMemory acts as the "notepad" for the AI system, maintaining conversation context
    /// across multiple interactions. It implements a sliding window approach with automatic summarization
    /// to manage token budgets effectively.
    /// </para>
    /// <para>
    /// <b>Key Features:</b>
    /// <list type="bullet">
    ///   <item><description><b>Message Storage:</b> Preserves full conversation history</description></item>
    ///   <item><description><b>Automatic Summarization:</b> Condenses old messages when token limit approached</description></item>
    ///   <item><description><b>Sliding Window:</b> Keeps recent messages (default: 8) + summaries of older messages</description></item>
    ///   <item><description><b>Token Tracking:</b> Estimates and manages token usage to prevent API limit issues</description></item>
    ///   <item><description><b>Context Preservation:</b> Maintains conversation flow for follow-up questions</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Lifecycle:</b>
    /// <list type="number">
    ///   <item><description>Conversation created with unique ID</description></item>
    ///   <item><description>Messages added via AddMessage/AddUserMessage/AddAssistantMessage</description></item>
    ///   <item><description>When token count exceeds TrimThreshold → Trim() called</description></item>
    ///   <item><description>Old messages summarized and moved to Summaries list</description></item>
    ///   <item><description>Recent messages retained for immediate context</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// Basic usage:
    /// <code>
    /// var conversation = new ConversationMemory();
    ///
    /// // Add messages
    /// conversation.AddUserMessage("What are forklift safety procedures?");
    /// conversation.AddAssistantMessage("Key procedures include...");
    /// conversation.AddUserMessage("What about battery charging?");
    ///
    /// // Check if trimming needed
    /// if (conversation.NeedsTrimming())
    /// {
    ///     var oldMessages = conversation.GetMessagesToSummarize();
    ///     string summary = await SummarizeMessages(oldMessages);
    ///     conversation.Trim(summary);
    /// }
    ///
    /// // Get context for AI
    /// var context = conversation.GetContextForAI();
    /// </code>
    /// </example>
    public class ConversationMemory
    {
        /// <summary>
        /// Unique identifier for this conversation session
        /// </summary>
        /// <remarks>
        /// Auto-generated GUID on creation. Example: "conv-abc123" or "f47ac10b-58cc-4372-a567-0e02b2c3d479"
        /// </remarks>
        public string ConversationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp when this conversation was created (UTC)
        /// </summary>
        public DateTime Created { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the last message was added or conversation was modified (UTC)
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// List of all current (non-summarized) messages in this conversation
        /// </summary>
        /// <remarks>
        /// <para>
        /// Includes both user messages and assistant responses in chronological order.
        /// When trimming occurs, older messages are removed from this list and summarized.
        /// </para>
        /// </remarks>
        public List<ConversationMessage> Messages { get; set; } = new();

        /// <summary>
        /// List of summary messages representing condensed older conversation segments
        /// </summary>
        /// <remarks>
        /// <para>
        /// When conversation exceeds token budget, old messages are summarized using AI and
        /// stored here. Summaries preserve key information while drastically reducing token usage.
        /// </para>
        /// <para>
        /// Each summary typically represents 10-20 original messages condensed into 1-2 sentences.
        /// </para>
        /// </remarks>
        public List<ConversationMessage> Summaries { get; set; } = new();

        /// <summary>
        /// Maximum token budget for conversation context (default: 2000)
        /// </summary>
        /// <remarks>
        /// This is the hard limit for total tokens (summaries + messages) to send to AI models.
        /// Prevents hitting API token limits and controls costs.
        /// </remarks>
        public int MaxTokenBudget { get; set; } = 2000;

        /// <summary>
        /// Token threshold that triggers automatic summarization (default: 1800)
        /// </summary>
        /// <remarks>
        /// When estimated token count exceeds this threshold, NeedsTrimming() returns true.
        /// Set below MaxTokenBudget to trigger summarization before hitting the hard limit.
        /// </remarks>
        public int TrimThreshold { get; set; } = 1800;

        /// <summary>
        /// Number of recent messages to keep unsummarized (default: 8)
        /// </summary>
        /// <remarks>
        /// <para>
        /// Implements the "sliding window" - most recent N messages are kept in full,
        /// while older messages are summarized. This preserves immediate context while
        /// reducing token usage for older history.
        /// </para>
        /// <para>
        /// Typical value: 6-10 messages (3-5 conversation turns)
        /// </para>
        /// </remarks>
        public int KeepRecentMessageCount { get; set; } = 8;

        /// <summary>
        /// Adds a new message to the conversation
        /// </summary>
        /// <param name="role">Message role: "user", "assistant", or "system"</param>
        /// <param name="content">Message content/text</param>
        /// <param name="metadata">Optional metadata dictionary</param>
        public void AddMessage(string role, string content, Dictionary<string, object>? metadata = null)
        {
            var message = new ConversationMessage
            {
                Role = role,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            Messages.Add(message);
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Convenience method to add a user message
        /// </summary>
        /// <param name="content">User's message text</param>
        public void AddUserMessage(string content)
        {
            AddMessage("user", content);
        }

        /// <summary>
        /// Convenience method to add an assistant response message
        /// </summary>
        /// <param name="content">Assistant's response text</param>
        /// <param name="metadata">Optional metadata (routing info, confidence, etc.)</param>
        public void AddAssistantMessage(string content, Dictionary<string, object>? metadata = null)
        {
            AddMessage("assistant", content, metadata);
        }

        /// <summary>
        /// Gets the N most recent messages
        /// </summary>
        /// <param name="count">Number of recent messages to retrieve</param>
        /// <returns>List of recent messages (may be less than count if conversation is short)</returns>
        public List<ConversationMessage> GetRecentMessages(int count)
        {
            if (Messages.Count <= count)
                return Messages;

            return Messages.GetRange(Messages.Count - count, count);
        }

        /// <summary>
        /// Clears all messages and summaries, resetting the conversation
        /// </summary>
        public void Clear()
        {
            Messages.Clear();
            Summaries.Clear();
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Calculates total estimated token count for this conversation (summaries + messages)
        /// </summary>
        /// <returns>Estimated total tokens using 4 chars/token heuristic</returns>
        public int GetEstimatedTokenCount()
        {
            int total = 0;

            // Count tokens in summaries
            foreach (var summary in Summaries)
            {
                total += EstimateTokens(summary.Content);
            }

            // Count tokens in messages
            foreach (var message in Messages)
            {
                total += EstimateTokens(message.Content);
            }

            return total;
        }

        // ✨ NEW: Estimate tokens for a text string
        // Rough estimation: ~4 characters per token (OpenAI rule of thumb)
        // More accurate: use tiktoken library, but this is good enough for budget management
        private int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Rough estimate: 1 token ≈ 4 characters
            // Add some overhead for message formatting
            return (text.Length / 4) + 10;
        }

        /// <summary>
        /// Checks if conversation has exceeded TrimThreshold and needs summarization
        /// </summary>
        /// <returns>True if token count >= TrimThreshold</returns>
        public bool NeedsTrimming()
        {
            return GetEstimatedTokenCount() >= TrimThreshold;
        }

        /// <summary>
        /// Gets complete conversation context to send to AI models
        /// </summary>
        /// <returns>Combined list of summaries (older context) + current messages (recent context)</returns>
        public List<ConversationMessage> GetContextForAI()
        {
            var context = new List<ConversationMessage>();

            // Add all summaries first (these are already condensed)
            context.AddRange(Summaries);

            // Add all current messages (not yet summarized)
            context.AddRange(Messages);

            return context;
        }

        // ✨ NEW: Trim old messages and replace with summary (Phase 3)
        // This implements the "sliding window" approach:
        // 1. Keep only the N most recent messages (KeepRecentMessageCount)
        // 2. Summarize older messages and add to Summaries list
        // 3. This reduces token usage while preserving context
        //
        // Example:
        // Before: 20 messages (2000 tokens)
        // After:  1 summary (200 tokens) + 8 recent messages (800 tokens) = 1000 tokens (50% savings!)
        public void Trim(string summaryContent)
        {
            if (Messages.Count <= KeepRecentMessageCount)
            {
                // Not enough messages to trim
                Console.WriteLine($"[ConversationMemory] ⚠️ Cannot trim - only {Messages.Count} messages (need > {KeepRecentMessageCount})");
                return;
            }

            // Calculate how many messages to summarize
            int messagesToSummarize = Messages.Count - KeepRecentMessageCount;

            // Get the old messages that will be summarized
            var oldMessages = Messages.GetRange(0, messagesToSummarize);

            // Calculate token savings
            int originalTokens = 0;
            foreach (var msg in oldMessages)
            {
                originalTokens += EstimateTokens(msg.Content);
            }

            int summaryTokens = EstimateTokens(summaryContent);

            // Create summary message using the factory method
            var summaryMessage = ConversationMessage.CreateSummary(
                summaryContent,
                messagesToSummarize,
                originalTokens,
                summaryTokens
            );

            // Add summary to Summaries list
            Summaries.Add(summaryMessage);

            // Remove old messages, keeping only recent ones
            Messages.RemoveRange(0, messagesToSummarize);

            // Update timestamp
            LastUpdated = DateTime.UtcNow;

            Console.WriteLine($"[ConversationMemory] ✂️ Trimmed {messagesToSummarize} messages");
            Console.WriteLine($"[ConversationMemory] 💰 Token savings: {originalTokens} → {summaryTokens} (saved {originalTokens - summaryTokens} tokens)");
            Console.WriteLine($"[ConversationMemory] 📊 Now: {Summaries.Count} summaries + {Messages.Count} messages = {GetEstimatedTokenCount()} tokens");
        }

        /// <summary>
        /// Gets older messages that should be summarized
        /// </summary>
        /// <returns>Messages that will be condensed (all except KeepRecentMessageCount most recent)</returns>
        /// <remarks>
        /// Call this BEFORE Trim() to generate summary content using AI, then pass summary to Trim()
        /// </remarks>
        public List<ConversationMessage> GetMessagesToSummarize()
        {
            if (Messages.Count <= KeepRecentMessageCount)
            {
                return new List<ConversationMessage>();
            }

            int messagesToSummarize = Messages.Count - KeepRecentMessageCount;
            return Messages.GetRange(0, messagesToSummarize);
        }
    }
}
