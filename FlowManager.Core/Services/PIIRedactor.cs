using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FlowManager.Core.Services
{
    /// <summary>
    /// Redacts PII from text using various strategies.
    /// Supports masking, hashing, removal, and tokenization.
    /// </summary>
    public class PIIRedactor
    {
        /// <summary>
        /// Redacts all detected PII from text using specified strategy.
        /// Returns redacted text and metadata about what was redacted.
        /// </summary>
        public static RedactionResult RedactPII(
            string text,
            RedactionStrategy strategy = RedactionStrategy.Mask,
            bool enableRedaction = true)
        {
            if (!enableRedaction || string.IsNullOrEmpty(text))
            {
                return new RedactionResult
                {
                    RedactedText = text,
                    OriginalText = text,
                    DetectedPII = new List<PIIDetection>(),
                    RedactionApplied = false
                };
            }

            // Detect all PII
            var detections = PIIDetector.ScanForPII(text);

            if (detections.Count == 0)
            {
                // No PII found
                return new RedactionResult
                {
                    RedactedText = text,
                    OriginalText = text,
                    DetectedPII = new List<PIIDetection>(),
                    RedactionApplied = false
                };
            }

            // Apply redaction strategy
            var redactedText = text;
            var redactionMap = new Dictionary<string, string>();

            // Process detections in reverse order to maintain indices
            for (int i = detections.Count - 1; i >= 0; i--)
            {
                var detection = detections[i];
                string replacement = strategy switch
                {
                    RedactionStrategy.Mask => MaskPII(detection),
                    RedactionStrategy.Hash => HashPII(detection),
                    RedactionStrategy.Remove => "",
                    RedactionStrategy.Tokenize => TokenizePII(detection),
                    _ => MaskPII(detection)
                };

                redactedText = redactedText.Remove(detection.StartIndex, detection.Length);
                redactedText = redactedText.Insert(detection.StartIndex, replacement);

                // Track what was replaced
                redactionMap[detection.Value] = replacement;
            }

            Console.WriteLine($"[PIIRedactor] 🔒 Redacted {detections.Count} PII instance(s) using {strategy} strategy");

            return new RedactionResult
            {
                RedactedText = redactedText,
                OriginalText = text,
                DetectedPII = detections,
                RedactionApplied = true,
                RedactionStrategy = strategy,
                RedactionMap = redactionMap
            };
        }

        /// <summary>
        /// Masks PII by showing only first/last characters.
        /// Example: "user@example.com" → "u***@e***.com"
        /// </summary>
        private static string MaskPII(PIIDetection detection)
        {
            var value = detection.Value;

            return detection.Type switch
            {
                PIIType.Email => MaskEmail(value),
                PIIType.PhoneNumber => MaskPhone(value),
                PIIType.SSN => "***-**-****",
                PIIType.CreditCard => MaskCreditCard(value),
                PIIType.IPAddress => "***.***.***.**",
                PIIType.StreetAddress => "[ADDRESS REDACTED]",
                PIIType.ZipCode => "*****",
                PIIType.DateOfBirth => "**/**/****",
                _ => new string('*', Math.Min(value.Length, 10))
            };
        }

        /// <summary>
        /// Masks email showing first char of username and domain.
        /// </summary>
        private static string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2)
                return "[EMAIL REDACTED]";

            var username = parts[0];
            var domain = parts[1];

            var maskedUsername = username.Length > 0 ? $"{username[0]}***" : "***";
            var maskedDomain = domain.Length > 0 ? $"{domain[0]}***.{domain.Split('.').Last()}" : "***";

            return $"{maskedUsername}@{maskedDomain}";
        }

        /// <summary>
        /// Masks phone showing last 4 digits.
        /// </summary>
        private static string MaskPhone(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length >= 4)
            {
                var last4 = digits.Substring(digits.Length - 4);
                return $"***-***-{last4}";
            }
            return "***-***-****";
        }

        /// <summary>
        /// Masks credit card showing last 4 digits.
        /// </summary>
        private static string MaskCreditCard(string card)
        {
            var digits = new string(card.Where(char.IsDigit).ToArray());
            if (digits.Length >= 4)
            {
                var last4 = digits.Substring(digits.Length - 4);
                return $"****-****-****-{last4}";
            }
            return "****-****-****-****";
        }

        /// <summary>
        /// Hashes PII using SHA256 for one-way encryption.
        /// </summary>
        private static string HashPII(PIIDetection detection)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(detection.Value);
            var hash = sha256.ComputeHash(bytes);
            var hashString = Convert.ToBase64String(hash).Substring(0, 16);

            var typeLabel = PIIDetector.GetTypeDescription(detection.Type);
            return $"[{typeLabel}:HASH:{hashString}]";
        }

        /// <summary>
        /// Tokenizes PII with a descriptive placeholder.
        /// </summary>
        private static string TokenizePII(PIIDetection detection)
        {
            var typeLabel = PIIDetector.GetTypeDescription(detection.Type).ToUpper().Replace(" ", "_");
            return $"[{typeLabel}]";
        }

        /// <summary>
        /// Checks if redaction should be enabled for this message.
        /// Can be extended to check user preferences, admin settings, etc.
        /// </summary>
        public static bool ShouldRedact(string messageRole, bool globalRedactionEnabled)
        {
            // Always redact user messages (contains most PII)
            // Optionally redact assistant messages (might echo PII)
            if (messageRole == "user")
                return globalRedactionEnabled;

            // Don't redact system messages (summaries, clarifications)
            if (messageRole == "system")
                return false;

            // For assistant messages, use global setting
            return globalRedactionEnabled;
        }
    }

    /// <summary>
    /// Redaction strategies for PII.
    /// </summary>
    public enum RedactionStrategy
    {
        /// <summary>
        /// Mask PII showing partial information (e.g., "***-***-1234")
        /// </summary>
        Mask,

        /// <summary>
        /// Hash PII using SHA256 (one-way encryption)
        /// </summary>
        Hash,

        /// <summary>
        /// Completely remove PII from text
        /// </summary>
        Remove,

        /// <summary>
        /// Replace with descriptive token (e.g., "[EMAIL_ADDRESS]")
        /// </summary>
        Tokenize
    }

    /// <summary>
    /// Result of PII redaction operation.
    /// </summary>
    public class RedactionResult
    {
        /// <summary>
        /// The text after redaction
        /// </summary>
        public string RedactedText { get; set; } = "";

        /// <summary>
        /// The original text before redaction (should be securely handled)
        /// </summary>
        public string OriginalText { get; set; } = "";

        /// <summary>
        /// List of all PII instances detected
        /// </summary>
        public List<PIIDetection> DetectedPII { get; set; } = new();

        /// <summary>
        /// Was redaction actually applied?
        /// </summary>
        public bool RedactionApplied { get; set; }

        /// <summary>
        /// Which strategy was used
        /// </summary>
        public RedactionStrategy RedactionStrategy { get; set; }

        /// <summary>
        /// Map of original values to redacted values
        /// </summary>
        public Dictionary<string, string> RedactionMap { get; set; } = new();

        /// <summary>
        /// Get summary of what was redacted.
        /// </summary>
        public string GetSummary()
        {
            if (!RedactionApplied || DetectedPII.Count == 0)
                return "No PII detected";

            var counts = DetectedPII
                .GroupBy(d => d.Type)
                .Select(g => $"{g.Count()} {PIIDetector.GetTypeDescription(g.Key)}{(g.Count() > 1 ? "s" : "")}")
                .ToList();

            return $"Redacted: {string.Join(", ", counts)}";
        }
    }
}
