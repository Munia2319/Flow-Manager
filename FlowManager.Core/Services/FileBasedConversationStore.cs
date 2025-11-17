using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FlowManager.Core.Models;

namespace FlowManager.Core.Services
{
    /// <summary>
    /// File-based conversation storage.
    /// Saves conversations as JSON files in ~/FlowManager/Conversations/
    /// Persistent across application restarts.
    /// </summary>
    public class FileBasedConversationStore : IConversationStore
    {
        private readonly string _storageDirectory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lock = new object();

        public FileBasedConversationStore(string? storageDirectory = null)
        {
            // Default storage location: ~/FlowManager/Conversations/
            _storageDirectory = storageDirectory
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "FlowManager",
                    "Conversations");

            // Create directory if it doesn't exist
            Directory.CreateDirectory(_storageDirectory);

            // JSON formatting options (pretty print with indentation)
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            Console.WriteLine($"[FileBasedStore] 📁 Storage directory: {_storageDirectory}");
        }

        public ConversationMemory? Load(string conversationId)
        {
            lock (_lock)
            {
                try
                {
                    var filePath = GetFilePath(conversationId);

                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine($"[FileBasedStore] ❌ Conversation file not found: {conversationId}");
                        return null;
                    }

                    var json = File.ReadAllText(filePath);
                    var conversation = JsonSerializer.Deserialize<ConversationMemory>(json, _jsonOptions);

                    if (conversation == null)
                    {
                        throw new Exception("Failed to deserialize conversation");
                    }

                    Console.WriteLine($"[FileBasedStore] 📂 Loaded conversation: {conversationId} ({conversation.Messages.Count} messages)");
                    return conversation;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FileBasedStore] ❌ Error loading conversation {conversationId}: {ex.Message}");
                    return null;
                }
            }
        }

        public void Save(ConversationMemory conversation)
        {
            lock (_lock)
            {
                try
                {
                    conversation.LastUpdated = DateTime.UtcNow;

                    var filePath = GetFilePath(conversation.ConversationId);
                    var json = JsonSerializer.Serialize(conversation, _jsonOptions);

                    File.WriteAllText(filePath, json);

                    Console.WriteLine($"[FileBasedStore] 💾 Saved conversation: {conversation.ConversationId} ({conversation.Messages.Count} messages)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FileBasedStore] ❌ Error saving conversation: {ex.Message}");
                    throw;
                }
            }
        }

        public void Append(string conversationId, ConversationMessage message)
        {
            lock (_lock)
            {
                // Load existing conversation or create new
                var conversation = Load(conversationId) ?? new ConversationMemory
                {
                    ConversationId = conversationId,
                    Created = DateTime.UtcNow
                };

                // Append message
                conversation.Messages.Add(message);
                conversation.LastUpdated = DateTime.UtcNow;

                // Save back to disk
                Save(conversation);

                Console.WriteLine($"[FileBasedStore] ➕ Appended message to {conversationId} (total: {conversation.Messages.Count})");
            }
        }

        public List<ConversationSummary> List()
        {
            lock (_lock)
            {
                try
                {
                    var summaries = new List<ConversationSummary>();

                    // Get all .json files in the directory
                    var jsonFiles = Directory.GetFiles(_storageDirectory, "*.json");

                    foreach (var filePath in jsonFiles)
                    {
                        try
                        {
                            var json = File.ReadAllText(filePath);
                            var conversation = JsonSerializer.Deserialize<ConversationMemory>(json, _jsonOptions);

                            if (conversation != null)
                            {
                                summaries.Add(new ConversationSummary
                                {
                                    ConversationId = conversation.ConversationId,
                                    Created = conversation.Created,
                                    LastUpdated = conversation.LastUpdated,
                                    MessageCount = conversation.Messages.Count,
                                    FirstMessagePreview = conversation.Messages.FirstOrDefault()?.Content?
                                        .Substring(0, Math.Min(50, conversation.Messages.FirstOrDefault()?.Content?.Length ?? 0)),
                                    LastMessagePreview = conversation.Messages.LastOrDefault()?.Content?
                                        .Substring(0, Math.Min(50, conversation.Messages.LastOrDefault()?.Content?.Length ?? 0))
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // If one file is corrupted, skip it and continue
                            Console.WriteLine($"[FileBasedStore] ⚠️ Could not load {Path.GetFileName(filePath)}: {ex.Message}");
                        }
                    }

                    // Sort by last updated (most recent first)
                    summaries = summaries.OrderByDescending(s => s.LastUpdated).ToList();

                    Console.WriteLine($"[FileBasedStore] 📋 Listed {summaries.Count} conversation(s)");
                    return summaries;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FileBasedStore] ❌ Error listing conversations: {ex.Message}");
                    return new List<ConversationSummary>();
                }
            }
        }

        public void Delete(string conversationId)
        {
            lock (_lock)
            {
                try
                {
                    var filePath = GetFilePath(conversationId);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Console.WriteLine($"[FileBasedStore] 🗑️ Deleted conversation: {conversationId}");
                    }
                    else
                    {
                        Console.WriteLine($"[FileBasedStore] ⚠️ Cannot delete - file not found: {conversationId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FileBasedStore] ❌ Error deleting conversation: {ex.Message}");
                    throw;
                }
            }
        }

        public bool Exists(string conversationId)
        {
            lock (_lock)
            {
                var filePath = GetFilePath(conversationId);
                return File.Exists(filePath);
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

            // Save immediately
            Save(conversation);

            Console.WriteLine($"[FileBasedStore] ✨ Created new conversation: {conversation.ConversationId}");
            return conversation;
        }

        /// <summary>
        /// Gets the full file path for a conversation ID
        /// </summary>
        private string GetFilePath(string conversationId)
        {
            return Path.Combine(_storageDirectory, $"{conversationId}.json");
        }

        /// <summary>
        /// Gets the storage directory path (useful for debugging)
        /// </summary>
        public string GetStorageDirectory()
        {
            return _storageDirectory;
        }
    }
}
