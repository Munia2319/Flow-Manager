using System;
using System.Collections.Generic;
using System.Linq;
using FlowManager.Core.Models;

namespace FlowManager.Core.Services
{
    /// <summary>
    /// In-memory conversation storage.
    /// Fast and simple, but data is lost on application restart.
    /// Good for development and testing.
    /// </summary>
    public class InMemoryConversationStore : IConversationStore
    {
        // Dictionary to store conversations by ID
        // Key: conversationId, Value: ConversationMemory
        private readonly Dictionary<string, ConversationMemory> _conversations = new();

        // Lock object for thread-safe access
        private readonly object _lock = new object();

        public ConversationMemory? Load(string conversationId)
        {
            lock (_lock)
            {
                if (_conversations.TryGetValue(conversationId, out var conversation))
                {
                    Console.WriteLine($"[InMemoryStore] 📂 Loaded conversation: {conversationId} ({conversation.Messages.Count} messages)");
                    return conversation;
                }

                Console.WriteLine($"[InMemoryStore] ❌ Conversation not found: {conversationId}");
                return null;
            }
        }

        public void Save(ConversationMemory conversation)
        {
            lock (_lock)
            {
                conversation.LastUpdated = DateTime.UtcNow;
                _conversations[conversation.ConversationId] = conversation;
                Console.WriteLine($"[InMemoryStore] 💾 Saved conversation: {conversation.ConversationId} ({conversation.Messages.Count} messages)");
            }
        }

        public void Append(string conversationId, ConversationMessage message)
        {
            lock (_lock)
            {
                // Load or create conversation
                if (!_conversations.TryGetValue(conversationId, out var conversation))
                {
                    conversation = new ConversationMemory
                    {
                        ConversationId = conversationId,
                        Created = DateTime.UtcNow
                    };
                    _conversations[conversationId] = conversation;
                }

                // Append message
                conversation.Messages.Add(message);
                conversation.LastUpdated = DateTime.UtcNow;

                Console.WriteLine($"[InMemoryStore] ➕ Appended message to {conversationId} (total: {conversation.Messages.Count})");
            }
        }

        public List<ConversationSummary> List()
        {
            lock (_lock)
            {
                var summaries = _conversations.Values
                    .Select(conv => new ConversationSummary
                    {
                        ConversationId = conv.ConversationId,
                        Created = conv.Created,
                        LastUpdated = conv.LastUpdated,
                        MessageCount = conv.Messages.Count,
                        FirstMessagePreview = conv.Messages.FirstOrDefault()?.Content?.Substring(0, Math.Min(50, conv.Messages.FirstOrDefault()?.Content?.Length ?? 0)),
                        LastMessagePreview = conv.Messages.LastOrDefault()?.Content?.Substring(0, Math.Min(50, conv.Messages.LastOrDefault()?.Content?.Length ?? 0))
                    })
                    .OrderByDescending(s => s.LastUpdated)
                    .ToList();

                Console.WriteLine($"[InMemoryStore] 📋 Listed {summaries.Count} conversation(s)");
                return summaries;
            }
        }

        public void Delete(string conversationId)
        {
            lock (_lock)
            {
                if (_conversations.Remove(conversationId))
                {
                    Console.WriteLine($"[InMemoryStore] 🗑️ Deleted conversation: {conversationId}");
                }
                else
                {
                    Console.WriteLine($"[InMemoryStore] ⚠️ Cannot delete - conversation not found: {conversationId}");
                }
            }
        }

        public bool Exists(string conversationId)
        {
            lock (_lock)
            {
                return _conversations.ContainsKey(conversationId);
            }
        }

        public ConversationMemory CreateNew()
        {
            var conversation = new ConversationMemory
            {
                ConversationId = $"conv-{Guid.NewGuid().ToString().Substring(0, 8)}",
                Created = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            lock (_lock)
            {
                _conversations[conversation.ConversationId] = conversation;
                Console.WriteLine($"[InMemoryStore] ✨ Created new conversation: {conversation.ConversationId}");
            }

            return conversation;
        }
    }
}
