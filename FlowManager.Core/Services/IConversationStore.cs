using System.Collections.Generic;
using FlowManager.Core.Models;

namespace FlowManager.Core.Services
{
    /// <summary>
    /// Interface for conversation storage abstraction.
    /// Allows swapping storage backends (memory, file, database) without changing application logic.
    /// </summary>
    public interface IConversationStore
    {
        /// <summary>
        /// Loads a conversation by ID.
        /// Returns null if conversation doesn't exist.
        /// </summary>
        /// <param name="conversationId">Unique conversation identifier</param>
        /// <returns>ConversationMemory or null if not found</returns>
        ConversationMemory? Load(string conversationId);

        /// <summary>
        /// Saves a complete conversation (overwrites existing).
        /// Creates new conversation if it doesn't exist.
        /// </summary>
        /// <param name="conversation">The conversation to save</param>
        void Save(ConversationMemory conversation);

        /// <summary>
        /// Appends a single message to an existing conversation.
        /// Creates new conversation if it doesn't exist.
        /// More efficient than loading, modifying, and saving.
        /// </summary>
        /// <param name="conversationId">Conversation identifier</param>
        /// <param name="message">Message to append</param>
        void Append(string conversationId, ConversationMessage message);

        /// <summary>
        /// Lists all conversations (metadata only, not full messages).
        /// Useful for showing conversation history UI.
        /// </summary>
        /// <returns>List of conversation summaries</returns>
        List<ConversationSummary> List();

        /// <summary>
        /// Deletes a conversation permanently.
        /// </summary>
        /// <param name="conversationId">Conversation to delete</param>
        void Delete(string conversationId);

        /// <summary>
        /// Checks if a conversation exists.
        /// </summary>
        /// <param name="conversationId">Conversation identifier</param>
        /// <returns>True if conversation exists</returns>
        bool Exists(string conversationId);

        /// <summary>
        /// Creates a new conversation with a unique ID.
        /// </summary>
        /// <returns>New ConversationMemory with unique ID</returns>
        ConversationMemory CreateNew();
    }
}
