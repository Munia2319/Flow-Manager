using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlowManager.Core.Services
{
    /// <summary>
    /// Detects Personally Identifiable Information (PII) in text.
    /// Uses regex patterns to identify emails, phones, SSNs, credit cards, etc.
    /// </summary>
    public class PIIDetector
    {
        // Regex patterns for different PII types
        private static readonly Dictionary<PIIType, Regex> Patterns = new()
        {
            // Email: user@domain.com
            [PIIType.Email] = new Regex(
                @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
                RegexOptions.Compiled),

            // Phone: (123) 456-7890, 123-456-7890, 1234567890
            [PIIType.PhoneNumber] = new Regex(
                @"\b(?:\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})\b",
                RegexOptions.Compiled),

            // SSN: 123-45-6789
            [PIIType.SSN] = new Regex(
                @"\b\d{3}-\d{2}-\d{4}\b",
                RegexOptions.Compiled),

            // Credit Card: 1234-5678-9012-3456 or 1234567890123456
            [PIIType.CreditCard] = new Regex(
                @"\b(?:\d{4}[-\s]?){3}\d{4}\b",
                RegexOptions.Compiled),

            // IP Address: 192.168.1.1
            [PIIType.IPAddress] = new Regex(
                @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b",
                RegexOptions.Compiled),

            // Street Address: 123 Main St, 456 Oak Avenue
            [PIIType.StreetAddress] = new Regex(
                @"\b\d+\s+[A-Za-z\s]+(Street|St|Avenue|Ave|Road|Rd|Boulevard|Blvd|Lane|Ln|Drive|Dr|Court|Ct)\b",
                RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Zip Code: 12345 or 12345-6789
            [PIIType.ZipCode] = new Regex(
                @"\b\d{5}(?:-\d{4})?\b",
                RegexOptions.Compiled),

            // Date of Birth: MM/DD/YYYY, MM-DD-YYYY
            [PIIType.DateOfBirth] = new Regex(
                @"\b(?:0?[1-9]|1[0-2])[-/](?:0?[1-9]|[12][0-9]|3[01])[-/](?:19|20)\d{2}\b",
                RegexOptions.Compiled)
        };

        /// <summary>
        /// Scans text for all types of PII.
        /// Returns list of detected PII instances with locations.
        /// </summary>
        public static List<PIIDetection> ScanForPII(string text)
        {
            var detections = new List<PIIDetection>();

            if (string.IsNullOrEmpty(text))
                return detections;

            foreach (var (piiType, pattern) in Patterns)
            {
                var matches = pattern.Matches(text);
                foreach (Match match in matches)
                {
                    detections.Add(new PIIDetection
                    {
                        Type = piiType,
                        Value = match.Value,
                        StartIndex = match.Index,
                        Length = match.Length
                    });
                }
            }

            // Sort by start index for proper replacement order
            detections.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));

            return detections;
        }

        /// <summary>
        /// Quick check if text contains any PII.
        /// </summary>
        public static bool ContainsPII(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (var pattern in Patterns.Values)
            {
                if (pattern.IsMatch(text))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Scans for specific PII type only.
        /// </summary>
        public static List<PIIDetection> ScanForType(string text, PIIType type)
        {
            var detections = new List<PIIDetection>();

            if (string.IsNullOrEmpty(text) || !Patterns.ContainsKey(type))
                return detections;

            var matches = Patterns[type].Matches(text);
            foreach (Match match in matches)
            {
                detections.Add(new PIIDetection
                {
                    Type = type,
                    Value = match.Value,
                    StartIndex = match.Index,
                    Length = match.Length
                });
            }

            return detections;
        }

        /// <summary>
        /// Gets human-readable description of PII type.
        /// </summary>
        public static string GetTypeDescription(PIIType type)
        {
            return type switch
            {
                PIIType.Email => "Email Address",
                PIIType.PhoneNumber => "Phone Number",
                PIIType.SSN => "Social Security Number",
                PIIType.CreditCard => "Credit Card Number",
                PIIType.IPAddress => "IP Address",
                PIIType.StreetAddress => "Street Address",
                PIIType.ZipCode => "Zip Code",
                PIIType.DateOfBirth => "Date of Birth",
                _ => "Unknown PII Type"
            };
        }
    }

    /// <summary>
    /// Types of PII that can be detected.
    /// </summary>
    public enum PIIType
    {
        Email,
        PhoneNumber,
        SSN,
        CreditCard,
        IPAddress,
        StreetAddress,
        ZipCode,
        DateOfBirth
    }

    /// <summary>
    /// Represents a detected instance of PII.
    /// </summary>
    public class PIIDetection
    {
        /// <summary>
        /// Type of PII detected
        /// </summary>
        public PIIType Type { get; set; }

        /// <summary>
        /// The actual PII value found
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// Start position in the original text
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// Length of the PII value
        /// </summary>
        public int Length { get; set; }
    }
}
