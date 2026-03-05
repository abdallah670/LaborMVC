using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LaborBLL.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LaborBLL.Service.Implementation
{
    /// <summary>
    /// Result of content inspection
    /// </summary>
    public class ContentInspectionResult
    {
        public bool IsClean { get; set; }
        public List<string> DetectedThreats { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool IsPolyglot { get; set; }
        public List<string> ValidFormats { get; set; } = new();
        public bool HasEncodedPayload { get; set; }
        public List<string> EncodingTypes { get; set; } = new();
        public double Entropy { get; set; }
    }

    /// <summary>
    /// Service for deep content inspection
    /// </summary>
    public interface IContentInspector
    {
        /// <summary>
        /// Inspect file content for malicious patterns
        /// </summary>
        Task<ContentInspectionResult> InspectAsync(
            IFormFile file,
            string? userId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if file is a polyglot (valid as multiple formats)
        /// </summary>
        Task<bool> IsPolyglotAsync(IFormFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detect encoded payloads
        /// </summary>
        Task<List<string>> DetectEncodedPayloadsAsync(IFormFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculate file entropy (randomness score)
        /// </summary>
        Task<double> CalculateEntropyAsync(IFormFile file, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Deep content inspector for detecting polyglot files, encoded payloads, and malicious content
    /// </summary>
    public class ContentInspector : IContentInspector
    {
        private readonly ILogger<ContentInspector> _logger;

        // EICAR test file signature (standard antivirus test)
        private const string EicarSignature = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

        // Suspicious patterns for embedded scripts
        private static readonly List<PatternDefinition> SuspiciousPatterns = new()
        {
            // PHP patterns
            new PatternDefinition("PHP_OpenTag", @"<\?php", RiskLevel.High, "PHP code detected"),
            new PatternDefinition("PHP_ShortTag", @"<\?=", RiskLevel.High, "PHP short tag detected"),
            new PatternDefinition("PHP_Eval", @"eval\s*\(", RiskLevel.Critical, "PHP eval() detected"),
            new PatternDefinition("PHP_System", @"system\s*\(", RiskLevel.Critical, "PHP system() detected"),
            new PatternDefinition("PHP_Exec", @"exec\s*\(", RiskLevel.Critical, "PHP exec() detected"),
            new PatternDefinition("PHP_ShellExec", @"shell_exec\s*\(", RiskLevel.Critical, "PHP shell_exec() detected"),
            new PatternDefinition("PHP_Passthru", @"passthru\s*\(", RiskLevel.Critical, "PHP passthru() detected"),

            // ASP/JSP patterns
            new PatternDefinition("ASP_Code", @"<%.*%>", RiskLevel.High, "ASP code detected"),
            new PatternDefinition("JSP_Code", @"<jsp:.*>", RiskLevel.High, "JSP code detected"),

            // JavaScript patterns
            new PatternDefinition("JS_Eval", @"eval\s*\(", RiskLevel.High, "JavaScript eval() detected"),
            new PatternDefinition("JS_Function", @"function\s*\(", RiskLevel.Medium, "JavaScript function detected"),
            new PatternDefinition("JS_DocumentWrite", @"document\.write", RiskLevel.High, "document.write detected"),
            new PatternDefinition("JS_InnerHTML", @"\.innerHTML\s*=", RiskLevel.High, "innerHTML assignment detected"),
            new PatternDefinition("JS_ScriptTag", @"<script[^>]*>", RiskLevel.High, "Script tag detected"),

            // Shell script patterns
            new PatternDefinition("Shell_Shebang", @"^#!/bin/(bash|sh|zsh)", RiskLevel.High, "Shell script shebang detected"),
            new PatternDefinition("Shell_Exec", @"`.*`", RiskLevel.High, "Shell command substitution detected"),
            new PatternDefinition("Shell_DollarParen", @"\$\(.*\)", RiskLevel.High, "Shell substitution detected"),

            // Python patterns
            new PatternDefinition("Python_Import", @"import\s+(os|sys|subprocess|socket)", RiskLevel.Medium, "Python system import detected"),
            new PatternDefinition("Python_Exec", @"exec\s*\(", RiskLevel.High, "Python exec() detected"),
            new PatternDefinition("Python_Eval", @"eval\s*\(", RiskLevel.High, "Python eval() detected"),

            // General malicious patterns
            new PatternDefinition("NullByte", @"\x00", RiskLevel.High, "Null byte injection detected"),
            new PatternDefinition("PathTraversal", @"\.\./|\.\.\\", RiskLevel.High, "Path traversal attempt detected"),
            new PatternDefinition("NullByte_Path", @"%00", RiskLevel.Critical, "Null byte injection in path detected"),
        };

        // Encoded payload patterns
        private static readonly List<EncodingPattern> EncodingPatterns = new()
        {
            new EncodingPattern("Base64", @"(?:[A-Za-z0-9+/]{4}){10,}(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?"),
            new EncodingPattern("Hex", @"(?:[0-9A-Fa-f]{2}){20,}"),
            new EncodingPattern("URL_Encoded", @"(?:%[0-9A-Fa-f]{2}){10,}"),
            new EncodingPattern("HTML_Entity", @"(?:&#[0-9]{1,5};|&#[Xx][0-9A-Fa-f]{1,4};){10,}"),
            new EncodingPattern("Unicode_Escape", @"(?:\\u[0-9A-Fa-f]{4}){10,}"),
        };

        // File signatures for polyglot detection
        private static readonly Dictionary<string, byte[][]> FileSignatures = new()
        {
            ["GIF"] = new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } },
            ["PNG"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            ["JPEG"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            ["PDF"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            ["ZIP"] = new[]
            {
                new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                new byte[] { 0x50, 0x4B, 0x07, 0x08 }
            },
            ["EXE"] = new[] { new byte[] { 0x4D, 0x5A } },
            ["ELF"] = new[] { new byte[] { 0x7F, 0x45, 0x4C, 0x46 } },
        };

        public ContentInspector(ILogger<ContentInspector> logger)
        {
            _logger = logger;
        }

        public async Task<ContentInspectionResult> InspectAsync(
            IFormFile file,
            string? userId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new ContentInspectionResult { IsClean = true };

            try
            {
                // Check for EICAR test file
                if (await ContainsEicarSignatureAsync(file, cancellationToken))
                {
                    result.IsClean = false;
                    result.DetectedThreats.Add("EICAR test signature detected - this is a test malware file");
                    throw new VirusDetectedException(
                        "EICAR test file detected",
                        virusName: "EICAR-Test-File",
                        scanEngine: "ContentInspector",
                        fileName: file.FileName,
                        userId: userId,
                        ipAddress: ipAddress);
                }

                // Read file content as text
                var content = await ReadFileAsStringAsync(file, cancellationToken);

                if (!string.IsNullOrEmpty(content))
                {
                    // Scan for suspicious patterns
                    foreach (var pattern in SuspiciousPatterns)
                    {
                        try
                        {
                            if (Regex.IsMatch(content, pattern.Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                            {
                                var message = $"{pattern.Description} (Risk: {pattern.RiskLevel})";

                                if (pattern.RiskLevel >= RiskLevel.High)
                                {
                                    result.IsClean = false;
                                    result.DetectedThreats.Add(message);
                                }
                                else
                                {
                                    result.Warnings.Add(message);
                                }
                            }
                        }
                        catch (RegexMatchTimeoutException)
                        {
                            _logger.LogWarning("Pattern matching timed out for {Pattern}", pattern.Name);
                        }
                    }

                    // Detect encoded payloads
                    var encodings = await DetectEncodedPayloadsAsync(file, cancellationToken);
                    if (encodings.Any())
                    {
                        result.HasEncodedPayload = true;
                        result.EncodingTypes.AddRange(encodings);

                        // High entropy + encoded payload = suspicious
                        if (result.Entropy > 7.5)
                        {
                            result.IsClean = false;
                            result.DetectedThreats.Add($"High entropy file with encoded content detected: {string.Join(", ", encodings)}");
                        }
                    }
                }

                // Check for polyglot files
                var validFormats = await DetectValidFormatsAsync(file, cancellationToken);
                if (validFormats.Count > 1)
                {
                    result.IsPolyglot = true;
                    result.ValidFormats = validFormats;
                    result.IsClean = false;
                    result.DetectedThreats.Add($"Polyglot file detected - valid as: {string.Join(", ", validFormats)}");

                    throw new PolyglotFileDetectedException(
                        $"File is valid as multiple formats: {string.Join(", ", validFormats)}",
                        validFormats,
                        fileName: file.FileName,
                        userId: userId,
                        ipAddress: ipAddress);
                }

                // Calculate entropy
                result.Entropy = await CalculateEntropyAsync(file, cancellationToken);
                if (result.Entropy > 7.9)
                {
                    result.Warnings.Add($"Very high entropy ({result.Entropy:F2}) - file may be encrypted or compressed");
                }

                _logger.LogDebug(
                    "Content inspection completed for {FileName}. Clean: {IsClean}, Threats: {ThreatCount}",
                    file.FileName, result.IsClean, result.DetectedThreats.Count);
            }
            catch (FileUploadSecurityException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during content inspection of {FileName}", file.FileName);
                result.Warnings.Add("Content inspection encountered an error");
            }

            return result;
        }

        public async Task<bool> IsPolyglotAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            var validFormats = await DetectValidFormatsAsync(file, cancellationToken);
            return validFormats.Count > 1;
        }

        public async Task<List<string>> DetectEncodedPayloadsAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            var detectedEncodings = new List<string>();

            try
            {
                var content = await ReadFileAsStringAsync(file, cancellationToken);
                if (string.IsNullOrEmpty(content))
                    return detectedEncodings;

                foreach (var encoding in EncodingPatterns)
                {
                    try
                    {
                        if (Regex.IsMatch(content, encoding.Pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5)))
                        {
                            detectedEncodings.Add(encoding.Name);
                        }
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        _logger.LogWarning("Encoding detection timed out for {Encoding}", encoding.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error detecting encoded payloads in {FileName}", file.FileName);
            }

            return detectedEncodings;
        }

        public async Task<double> CalculateEntropyAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var bytes = new byte[Math.Min(file.Length, 8192)]; // Sample first 8KB
                var bytesRead = await stream.ReadAsync(bytes.AsMemory(0, bytes.Length), cancellationToken);

                if (bytesRead == 0)
                    return 0;

                // Calculate frequency of each byte value
                var frequencies = new int[256];
                for (int i = 0; i < bytesRead; i++)
                {
                    frequencies[bytes[i]]++;
                }

                // Calculate Shannon entropy
                double entropy = 0;
                for (int i = 0; i < 256; i++)
                {
                    if (frequencies[i] > 0)
                    {
                        double probability = (double)frequencies[i] / bytesRead;
                        entropy -= probability * Math.Log2(probability);
                    }
                }

                return entropy;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating entropy for {FileName}", file.FileName);
                return 0;
            }
        }

        private async Task<bool> ContainsEicarSignatureAsync(IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                var content = await ReadFileAsStringAsync(file, cancellationToken);
                return content?.Contains(EicarSignature) ?? false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<List<string>> DetectValidFormatsAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var validFormats = new List<string>();

            try
            {
                using var stream = file.OpenReadStream();
                var headerBytes = new byte[16];
                var bytesRead = await stream.ReadAsync(headerBytes.AsMemory(0, 16), cancellationToken);

                foreach (var signature in FileSignatures)
                {
                    foreach (var pattern in signature.Value)
                    {
                        if (bytesRead >= pattern.Length &&
                            headerBytes.Take(pattern.Length).SequenceEqual(pattern))
                        {
                            if (!validFormats.Contains(signature.Key))
                            {
                                validFormats.Add(signature.Key);
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error detecting file formats for {FileName}", file.FileName);
            }

            return validFormats;
        }

        private async Task<string> ReadFileAsStringAsync(IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                // Only read text files or first part of binary files
                var maxBytes = (int)Math.Min(file.Length, 65536); // Max 64KB
                using var stream = file.OpenReadStream();
                var bytes = new byte[maxBytes];
                var bytesRead = await stream.ReadAsync(bytes.AsMemory(0, maxBytes), cancellationToken);

                // Try to detect if this is text content
                if (IsTextContent(bytes, bytesRead))
                {
                    return Encoding.UTF8.GetString(bytes, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading file {FileName} as string", file.FileName);
            }

            return string.Empty;
        }

        private bool IsTextContent(byte[] bytes, int length)
        {
            // Check for null bytes which typically indicate binary content
            for (int i = 0; i < length; i++)
            {
                if (bytes[i] == 0)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Pattern definition for suspicious content
    /// </summary>
    internal class PatternDefinition
    {
        public string Name { get; }
        public string Pattern { get; }
        public RiskLevel RiskLevel { get; }
        public string Description { get; }

        public PatternDefinition(string name, string pattern, RiskLevel riskLevel, string description)
        {
            Name = name;
            Pattern = pattern;
            RiskLevel = riskLevel;
            Description = description;
        }
    }

    /// <summary>
    /// Encoding pattern definition
    /// </summary>
    internal class EncodingPattern
    {
        public string Name { get; }
        public string Pattern { get; }

        public EncodingPattern(string name, string pattern)
        {
            Name = name;
            Pattern = pattern;
        }
    }
}
