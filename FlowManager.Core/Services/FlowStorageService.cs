using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FlowManager.Core.Models;

namespace FlowManager.Core.Services
{
    /// <summary>
    /// Flow Storage Service - Persistent file-based storage for flow definitions
    /// </summary>
    /// <remarks>
    /// <para>
    /// FlowStorageService provides CRUD operations for flow definitions using JSON file storage.
    /// It acts as a "filing cabinet" for your flows, handling serialization, deserialization,
    /// and file management.
    /// </para>
    /// <para>
    /// <b>Storage Location:</b>
    /// <br/>
    /// Default: <c>%USERPROFILE%\FlowManager\Flows\</c>
    /// <br/>
    /// Custom: Specify directory in constructor
    /// </para>
    /// <para>
    /// <b>File Format:</b>
    /// <br/>
    /// Each flow is stored as a separate JSON file: <c>{flowId}.json</c>
    /// <br/>
    /// JSON is formatted with indentation for readability and uses camelCase property naming
    /// </para>
    /// <para>
    /// <b>Features:</b>
    /// <list type="bullet">
    ///   <item><description><b>Auto-Create Directory:</b> Creates storage directory if it doesn't exist</description></item>
    ///   <item><description><b>Timestamp Tracking:</b> Automatically updates LastModified on save</description></item>
    ///   <item><description><b>Error Resilience:</b> ListFlows skips corrupted files and continues</description></item>
    ///   <item><description><b>Case-Insensitive:</b> Accepts both camelCase and PascalCase when reading JSON</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Thread Safety:</b> Not thread-safe. Use locking if accessing from multiple threads.
    /// </para>
    /// </remarks>
    /// <example>
    /// Basic usage:
    /// <code>
    /// var storage = new FlowStorageService();
    ///
    /// // Save a flow
    /// var flow = new Flow { Id = "my-flow", Name = "My Flow" };
    /// storage.SaveFlow(flow);
    ///
    /// // Load a flow
    /// var loaded = storage.LoadFlow("my-flow");
    ///
    /// // List all flows
    /// var allFlows = storage.ListFlows();
    ///
    /// // Delete a flow
    /// storage.DeleteFlow("my-flow");
    /// </code>
    ///
    /// Custom storage location:
    /// <code>
    /// var storage = new FlowStorageService("C:\\MyFlows");
    /// </code>
    /// </example>
    public class FlowStorageService
    {
        // WHERE to save files
        // Example: C:\FlowManager\Flows\
        private readonly string _storageDirectory;
        
        // JSON OPTIONS - How to format the JSON
        private readonly JsonSerializerOptions _jsonOptions;
        
        /// <summary>
        /// Initializes a new instance of FlowStorageService with specified storage directory
        /// </summary>
        /// <param name="storageDirectory">
        /// Optional custom storage directory. If null, uses default location:
        /// %USERPROFILE%\FlowManager\Flows\
        /// </param>
        /// <remarks>
        /// Automatically creates the storage directory if it doesn't exist.
        /// </remarks>
        public FlowStorageService(string? storageDirectory = null)
        {
            // If no directory provided, use default
            _storageDirectory = storageDirectory 
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "FlowManager", "Flows");
            
            // Create directory if it doesn't exist
            Directory.CreateDirectory(_storageDirectory);
            
            // JSON formatting options (pretty print with indentation)
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,  // Makes JSON readable
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,  // Use camelCase
                PropertyNameCaseInsensitive = true  // ✨ FIX: Accept both camelCase and PascalCase when reading
            };
        }
        
        /// <summary>
        /// Saves a flow to disk as a JSON file
        /// </summary>
        /// <param name="flow">The flow to save (must have a valid Id)</param>
        /// <exception cref="ArgumentException">Thrown if flow.Id is null or empty</exception>
        /// <exception cref="Exception">Thrown if file write operation fails</exception>
        /// <remarks>
        /// <para>
        /// File naming: <c>{flow.Id}.json</c>
        /// </para>
        /// <para>
        /// Automatically updates flow.LastModified to current UTC time before saving.
        /// </para>
        /// <para>
        /// Overwrites existing file if a flow with the same ID already exists.
        /// </para>
        /// </remarks>
        public void SaveFlow(Flow flow)
        {
            try
            {
                // Validate flow has an ID
                if (string.IsNullOrEmpty(flow.Id))
                {
                    throw new ArgumentException("Flow must have an Id");
                }
                
                // Update LastModified timestamp
                flow.LastModified = DateTime.UtcNow;
                
                // Create filename: flow-id.json
                // Example: "test-flow-1.json"
                var fileName = $"{flow.Id}.json";
                var filePath = Path.Combine(_storageDirectory, fileName);
                
                // Convert flow to JSON
                var json = JsonSerializer.Serialize(flow, _jsonOptions);
                
                // Write to file
                File.WriteAllText(filePath, json);
                
                Console.WriteLine($"✅ Flow saved: {filePath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save flow: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Loads a flow from disk by its ID
        /// </summary>
        /// <param name="flowId">The unique identifier of the flow to load</param>
        /// <returns>The loaded Flow object</returns>
        /// <exception cref="FileNotFoundException">Thrown if the flow file doesn't exist</exception>
        /// <exception cref="Exception">Thrown if file read or deserialization fails</exception>
        /// <remarks>
        /// Looks for file: <c>{flowId}.json</c> in storage directory
        /// </remarks>
        public Flow LoadFlow(string flowId)
        {
            try
            {
                // Create filename
                var fileName = $"{flowId}.json";
                var filePath = Path.Combine(_storageDirectory, fileName);
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Flow not found: {flowId}");
                }
                
                // Read JSON from file
                var json = File.ReadAllText(filePath);
                
                // Convert JSON to Flow object
                var flow = JsonSerializer.Deserialize<Flow>(json, _jsonOptions);
                
                if (flow == null)
                {
                    throw new Exception("Failed to deserialize flow");
                }
                
                Console.WriteLine($"✅ Flow loaded: {flowId}");
                return flow;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load flow: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Lists all flows stored in the storage directory
        /// </summary>
        /// <returns>List of all successfully loaded flows</returns>
        /// <exception cref="Exception">Thrown if directory access fails</exception>
        /// <remarks>
        /// <para>
        /// <b>Error Resilience:</b> If individual files are corrupted or fail to deserialize,
        /// they are skipped with a warning message, and the method continues processing other files.
        /// </para>
        /// <para>
        /// Only files with .json extension are processed.
        /// </para>
        /// </remarks>
        public List<Flow> ListFlows()
        {
            try
            {
                var flows = new List<Flow>();
                
                // Get all .json files in the directory
                var jsonFiles = Directory.GetFiles(_storageDirectory, "*.json");
                
                // Load each flow
                foreach (var filePath in jsonFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var flow = JsonSerializer.Deserialize<Flow>(json, _jsonOptions);
                        
                        if (flow != null)
                        {
                            flows.Add(flow);
                        }
                    }
                    catch (Exception ex)
                    {
                        // If one file is corrupted, skip it and continue
                        Console.WriteLine($"⚠️ Warning: Could not load {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }
                
                Console.WriteLine($"✅ Found {flows.Count} flow(s)");
                return flows;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to list flows: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Deletes a flow file from disk
        /// </summary>
        /// <param name="flowId">The unique identifier of the flow to delete</param>
        /// <exception cref="FileNotFoundException">Thrown if the flow file doesn't exist</exception>
        /// <exception cref="Exception">Thrown if file deletion fails</exception>
        /// <remarks>
        /// <b>Warning:</b> This operation is permanent and cannot be undone.
        /// </remarks>
        public void DeleteFlow(string flowId)
        {
            try
            {
                var fileName = $"{flowId}.json";
                var filePath = Path.Combine(_storageDirectory, fileName);
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Flow not found: {flowId}");
                }
                
                File.Delete(filePath);
                Console.WriteLine($"✅ Flow deleted: {flowId}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete flow: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Checks if a flow file exists on disk
        /// </summary>
        /// <param name="flowId">The unique identifier of the flow to check</param>
        /// <returns>True if the flow file exists, false otherwise</returns>
        public bool FlowExists(string flowId)
        {
            var fileName = $"{flowId}.json";
            var filePath = Path.Combine(_storageDirectory, fileName);
            return File.Exists(filePath);
        }
        
        /// <summary>
        /// Gets the absolute path of the storage directory
        /// </summary>
        /// <returns>Full path to the storage directory</returns>
        public string GetStorageDirectory()
        {
            return _storageDirectory;
        }
    }
}