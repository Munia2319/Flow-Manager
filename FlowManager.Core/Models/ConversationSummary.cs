using System;

namespace FlowManager.Core.Models
{
    /// <summary>
    /// Lightweight summary of a conversation (for listing purposes).
    /// Doesn't include full message history to save memory.
    /// </summary>
    public class ConversationSummary
    {
        /// <summary>
        /// Unique conversation identifier
        /// </summary>
        public string ConversationId { get; set; } = "";

        /// <summary>
        /// Flow ID this conversation is associated with
        /// </summary>
        public string? FlowId { get; set; }

        /// <summary>
        /// When the conversation was created
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// When the conversation was last updated
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Total number of messages in the conversation
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// Preview of the first user message (for display in list)
        /// </summary>
        public string? FirstMessagePreview { get; set; }

        /// <summary>
        /// Preview of the last message (for display in list)
        /// </summary>
        public string? LastMessagePreview { get; set; }
    }
}
