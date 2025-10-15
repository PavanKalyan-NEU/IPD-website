using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Northeastern_Personal_Workspace.Models;
using UglyToad.PdfPig;

namespace Northeastern_Personal_Workspace.Services
{
    public class ProgramParsingService
    {
        private readonly string _pdfPath;
        private List<GraduateProgram>? _cachedPrograms;
        private readonly object _lock = new();

        public ProgramParsingService(string pdfPath)
        {
            _pdfPath = pdfPath;
            Console.WriteLine($"PDF Path configured for programs: {_pdfPath}");

            var directory = Path.GetDirectoryName(_pdfPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine($"Created directory: {directory}");
            }
        }

        public async Task<List<GraduateProgram>> GetAllProgramsAsync()
        {
            lock (_lock)
            {
                if (_cachedPrograms != null)
                {
                    Console.WriteLine($"Returning cached programs: {_cachedPrograms.Count} programs");
                    return _cachedPrograms;
                }
            }

            var programs = await ParseProgramsFromCatalogAsync();

            lock (_lock)
            {
                _cachedPrograms = programs;
            }

            Console.WriteLine($"Loaded {programs.Count} programs from PDF");
            return programs;
        }

        private async Task<List<GraduateProgram>> ParseProgramsFromCatalogAsync()
        {
            var programs = new List<GraduateProgram>();

            try
            {
                if (!File.Exists(_pdfPath))
                {
                    Console.WriteLine("PDF not found, downloading...");
                    await DownloadPdfAsync();
                }

                using (var pdf = PdfDocument.Open(_pdfPath))
                {
                    var pageCount = pdf.NumberOfPages;
                    Console.WriteLine($"PDF opened successfully. Total pages: {pageCount}");

                    // Collect all text first
                    var allText = "";
                    for (int pageNum = 1; pageNum <= pageCount; pageNum++)
                    {
                        try
                        {
                            var page = pdf.GetPage(pageNum);
                            allText += page.Text + "\n";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading page {pageNum}: {ex.Message}");
                        }
                    }

                    // Clean the text
                    allText = CleanText(allText);

                    // Find programs using multiple patterns
                    var programPatterns = new[]
                    {
                        // Pattern 1: Program name followed by degree on same or next line
                        @"^([A-Za-z\s\-&,]+?)\s*[,\s]\s*(MS|MA|MBA|MEd|MFA|PhD|MPA|MPS|Graduate Certificate|Certificate)\b",
                        
                        // Pattern 2: "Master of Science in X" format
                        @"Master\s+of\s+Science\s+in\s+([A-Za-z\s\-&,]+?)(?=\s|$)",
                        
                        // Pattern 3: Program heading style with degree
                        @"^#+\s*([A-Za-z\s\-&,]+?)\s*,\s*(MS|MA|MBA|MEd|MFA|PhD|MPA|MPS)\b",
                        
                        // Pattern 4: Bold text followed by degree
                        @"\*\*([A-Za-z\s\-&,]+?)\s*,\s*(MS|MA|MBA|MEd|MFA|PhD|MPA|MPS)\*\*",
                        
                        // Pattern 5: Just the degree type preceded by program name
                        @"([A-Za-z][A-Za-z\s\-&,]{2,50})\s*,\s*(MS|MA|MBA|MEd|PhD|MPA|MPS)\s*$"
                    };

                    foreach (var pattern in programPatterns)
                    {
                        var matches = Regex.Matches(allText, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                        Console.WriteLine($"Pattern '{pattern.Substring(0, Math.Min(50, pattern.Length))}...' found {matches.Count} matches");

                        foreach (Match match in matches)
                        {
                            string programName = "";
                            string degreeType = "";

                            if (pattern.Contains("Master of Science"))
                            {
                                programName = match.Groups[1].Value.Trim();
                                degreeType = "MS";
                            }
                            else
                            {
                                programName = match.Groups[1].Value.Trim();
                                degreeType = match.Groups[2].Value.Trim().ToUpper();
                            }

                            // Validate program name
                            if (string.IsNullOrWhiteSpace(programName) ||
                                programName.Length < 3 ||
                                programName.Length > 100 ||
                                programName.Contains("Professor") ||
                                programName.Contains("University") ||
                                programName.Contains("College,"))
                            {
                                continue;
                            }

                            // Check if we already have this program
                            if (programs.Any(p => p.ProgramName.Equals(programName, StringComparison.OrdinalIgnoreCase) &&
                                                 p.DegreeType.Equals(degreeType, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }

                            // Extract description (text after the match)
                            var startIndex = match.Index + match.Length;
                            var description = ExtractProgramDescription(allText, startIndex, programName);

                            // Only add if we have a meaningful description
                            if (!string.IsNullOrWhiteSpace(description) && description.Length > 50)
                            {
                                var program = new GraduateProgram
                                {
                                    ProgramName = programName,
                                    DegreeType = degreeType,
                                    Description = description,
                                    College = ExtractCollegeFromText(description + " " + programName),
                                    PageNumber = 1 // We'll update this if needed
                                };

                                programs.Add(program);
                                Console.WriteLine($"Found program: {program.FullProgramName}");
                            }
                        }
                    }

                    // If no programs found with patterns, try a more aggressive search
                    if (programs.Count == 0)
                    {
                        Console.WriteLine("No programs found with patterns, trying direct search...");

                        // Look for lines that end with degree types
                        var lines = allText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        var degreeTypes = new[] { "MS", "MA", "MBA", "MEd", "MFA", "PhD", "MPA", "MPS" };

                        for (int i = 0; i < lines.Length; i++)
                        {
                            var line = lines[i].Trim();

                            foreach (var degree in degreeTypes)
                            {
                                if (line.EndsWith($", {degree}") || line.EndsWith($",{degree}"))
                                {
                                    var parts = line.Split(',');
                                    if (parts.Length >= 2)
                                    {
                                        var programName = string.Join(",", parts.Take(parts.Length - 1)).Trim();

                                        if (programName.Length > 3 && programName.Length < 100 &&
                                            !programName.Contains("Professor"))
                                        {
                                            var description = "";
                                            // Look for description in next few lines
                                            for (int j = i + 1; j < Math.Min(i + 10, lines.Length); j++)
                                            {
                                                if (lines[j].Length > 50 && !lines[j].Contains("Code") &&
                                                    !lines[j].Contains("Hours") && !lines[j].Contains("Professor"))
                                                {
                                                    description = lines[j].Trim();
                                                    break;
                                                }
                                            }

                                            var program = new GraduateProgram
                                            {
                                                ProgramName = programName,
                                                DegreeType = degree,
                                                Description = description,
                                                College = "Northeastern University",
                                                PageNumber = 1
                                            };

                                            programs.Add(program);
                                            Console.WriteLine($"Found program (direct): {program.FullProgramName}");
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }

                Console.WriteLine($"Parsing complete. Extracted {programs.Count} programs from PDF");

                // Log first few programs for debugging
                foreach (var prog in programs.Take(3))
                {
                    Console.WriteLine($"Sample program: {prog.FullProgramName}");
                    Console.WriteLine($"  Description preview: {prog.Description.Substring(0, Math.Min(100, prog.Description.Length))}...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing PDF: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return programs;
        }

        private string ExtractProgramDescription(string text, int startIndex, string programName)
        {
            if (startIndex >= text.Length) return "";

            // Skip any whitespace or newlines immediately after the program header
            while (startIndex < text.Length && (text[startIndex] == '\n' || text[startIndex] == '\r' || char.IsWhiteSpace(text[startIndex])))
            {
                startIndex++;
            }

            var endPatterns = new[]
            {
                "Program Requirements",
                "Core Requirements",
                "Admission Requirements",
                @"Code\s+Title\s+Hours",
                "Complete all courses",
                "semester hours required",
                "University Faculty",
                @"\d+\s*(?:total\s+)?semester\s+hours",
                @"Minimum.*GPA",
                "Concentration Options",
                "Electives",
                @"^\s*•", // Bullet points
                @"http[s]?://", // URLs
                @"\([A-Za-z]+://[^\)]+\)", // URLs in parentheses
                @"\d{3}[A-Za-z]", // Page numbers like "348Complex"
                @"\.{5,}", // Multiple dots (table of contents style)
                @"\s+\d{3,4}\s*$", // Page numbers at end of lines
                @",\s*(?:MS|MA|MBA|PhD|MFA)", // Another program starting
            };

            var substring = text.Substring(startIndex);
            var minEndIndex = Math.Min(2000, substring.Length);

            // Find the earliest occurrence of any end pattern
            foreach (var pattern in endPatterns)
            {
                var match = Regex.Match(substring, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && match.Index < minEndIndex && match.Index > 20)
                {
                    minEndIndex = match.Index;
                }
            }

            var rawDescription = substring.Substring(0, minEndIndex).Trim();

            // Clean up the description
            rawDescription = Regex.Replace(rawDescription, @"\s+", " ");
            rawDescription = Regex.Replace(rawDescription, @"\.{2,}", ""); // Remove multiple dots
            rawDescription = Regex.Replace(rawDescription, @"\d{3,4}(?=[A-Z])", " "); // Remove page numbers before capital letters

            // Remove URLs and their surrounding text
            rawDescription = Regex.Replace(rawDescription, @"\([^)]*http[s]?://[^)]+\)", "", RegexOptions.IgnoreCase);
            rawDescription = Regex.Replace(rawDescription, @"http[s]?://[^\s]+", "", RegexOptions.IgnoreCase);

            // Look for sentences that describe the program
            var sentences = new List<string>();
            var sentenceMatches = Regex.Matches(rawDescription, @"[A-Z][^.!?]*[.!?]");

            foreach (Match sentMatch in sentenceMatches)
            {
                var sentence = sentMatch.Value.Trim();

                // Include sentences that look like program descriptions
                if (sentence.Length > 30 &&
                    !sentence.Contains("Professor") &&
                    !sentence.Contains("PhD,") &&
                    !sentence.Contains("University,") &&
                    !Regex.IsMatch(sentence, @"\d{3,}") && // No page numbers
                    (sentence.ToLower().Contains("program") ||
                     sentence.ToLower().Contains("master") ||
                     sentence.ToLower().Contains("degree") ||
                     sentence.ToLower().Contains("student") ||
                     sentence.ToLower().Contains("curriculum") ||
                     sentence.ToLower().Contains("designed") ||
                     sentence.ToLower().Contains("provides") ||
                     sentence.ToLower().Contains("prepares") ||
                     sentence.ToLower().Contains("focuses") ||
                     sentence.ToLower().Contains("offers")))
                {
                    sentences.Add(sentence);
                    if (sentences.Count >= 3) break; // Limit to first 3 good sentences
                }
            }

            // If we found good sentences, use them
            if (sentences.Count > 0)
            {
                return string.Join(" ", sentences);
            }

            // Otherwise, try to extract the first paragraph
            var paragraphs = rawDescription.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (paragraphs.Length > 0)
            {
                var firstPara = paragraphs[0].Trim();
                if (firstPara.Length > 50 && !Regex.IsMatch(firstPara, @"\d{3,}"))
                {
                    return firstPara;
                }
            }

            // Last resort: return cleaned raw description if it's meaningful
            return rawDescription.Length > 50 ? rawDescription : "";
        }

        private string ExtractCollegeFromText(string text)
        {
            var collegePatterns = new Dictionary<string, string>
            {
                { @"Khoury College(?:\s+of Computer Sciences)?", "Khoury College of Computer Sciences" },
                { @"College of Engineering", "College of Engineering" },
                { @"College of Science", "College of Science" },
                { @"D['']Amore[\s-]McKim(?:\s+School of Business)?", "D'Amore-McKim School of Business" },
                { @"Bouvé College(?:\s+of Health Sciences)?", "Bouvé College of Health Sciences" },
                { @"College of Arts,?\s*Media(?:\s+and Design)?", "College of Arts, Media and Design" },
                { @"School of Law", "School of Law" },
                { @"College of Social Sciences(?:\s+and Humanities)?", "College of Social Sciences and Humanities" }
            };

            foreach (var pattern in collegePatterns)
            {
                if (Regex.IsMatch(text, pattern.Key, RegexOptions.IgnoreCase))
                {
                    return pattern.Value;
                }
            }

            // Try to infer from program name
            var lowerText = text.ToLower();
            if (lowerText.Contains("computer") || lowerText.Contains("artificial intelligence") ||
                lowerText.Contains("data science") || lowerText.Contains("cybersecurity"))
            {
                return "Khoury College of Computer Sciences";
            }
            else if (lowerText.Contains("engineering"))
            {
                return "College of Engineering";
            }
            else if (lowerText.Contains("business") || lowerText.Contains("management") ||
                     lowerText.Contains("finance") || lowerText.Contains("marketing"))
            {
                return "D'Amore-McKim School of Business";
            }

            return "Northeastern University";
        }

        public List<ProgramSearchResult> SearchPrograms(List<string> searchKeywords)
        {
            var results = new List<ProgramSearchResult>();
            var programs = _cachedPrograms ?? new List<GraduateProgram>();

            foreach (var program in programs)
            {
                var matchReasons = new List<string>();
                double relevanceScore = 0;

                foreach (var keyword in searchKeywords)
                {
                    var lowerKeyword = keyword.ToLower().Trim();

                    // Check program name
                    if (program.ProgramName.ToLower().Contains(lowerKeyword))
                    {
                        matchReasons.Add($"Program name contains '{keyword}'");
                        relevanceScore += 3.0;
                    }

                    // Check description
                    if (program.Description != null && program.Description.ToLower().Contains(lowerKeyword))
                    {
                        var occurrences = Regex.Matches(program.Description.ToLower(), lowerKeyword).Count;
                        matchReasons.Add($"Program description mentions '{keyword}' ({occurrences} times)");
                        relevanceScore += 2.0 * Math.Min(occurrences, 3);
                    }

                    // Check degree type
                    if (program.DegreeType.ToLower().Contains(lowerKeyword))
                    {
                        matchReasons.Add($"Degree type matches '{keyword}'");
                        relevanceScore += 1.0;
                    }

                    // Check college
                    if (program.College != null && program.College.ToLower().Contains(lowerKeyword))
                    {
                        matchReasons.Add($"College name contains '{keyword}'");
                        relevanceScore += 1.5;
                    }

                    // Additional keyword matching for common variations
                    var keywordVariations = GetKeywordVariations(lowerKeyword);
                    foreach (var variation in keywordVariations)
                    {
                        if (program.ProgramName.ToLower().Contains(variation) ||
                            (program.Description != null && program.Description.ToLower().Contains(variation)))
                        {
                            if (!matchReasons.Any(r => r.Contains($"'{keyword}'")))
                            {
                                matchReasons.Add($"Program contains related term '{variation}' for '{keyword}'");
                                relevanceScore += 1.0;
                            }
                        }
                    }
                }

                if (matchReasons.Any())
                {
                    results.Add(new ProgramSearchResult
                    {
                        Program = program,
                        MatchReasons = matchReasons,
                        RelevanceScore = relevanceScore
                    });
                }
            }

            return results.OrderByDescending(r => r.RelevanceScore).ToList();
        }

        private List<string> GetKeywordVariations(string keyword)
        {
            var variations = new List<string>();

            // Common AI/ML variations
            if (keyword == "ai") variations.AddRange(new[] { "artificial intelligence", "a.i." });
            if (keyword == "artificial intelligence") variations.AddRange(new[] { "ai", "a.i." });
            if (keyword == "ml") variations.AddRange(new[] { "machine learning" });
            if (keyword == "machine learning") variations.AddRange(new[] { "ml", "learning", "deep learning" });
            if (keyword == "data science") variations.AddRange(new[] { "data analytics", "data analysis" });
            if (keyword == "robotics") variations.AddRange(new[] { "robot", "robotic" });
            if (keyword == "computer vision") variations.AddRange(new[] { "vision", "image processing" });

            return variations;
        }

        private string CleanText(string text)
        {
            text = text.Replace("\uFB01", "fi");
            text = text.Replace("\uFB02", "fl");
            text = text.Replace("\u00A0", " ");
            text = text.Replace("\uFB00", "ff");
            text = text.Replace("\uFB03", "ffi");
            text = text.Replace("\uFB04", "ffl");

            return text;
        }

        private async Task DownloadPdfAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(5);

                var url = "https://catalog.northeastern.edu/pdf/Northeastern%20University%202024-2025%20Graduate%20Catalog.pdf";
                Console.WriteLine($"Downloading from: {url}");

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine($"Downloaded {pdfBytes.Length} bytes");

                await File.WriteAllBytesAsync(_pdfPath, pdfBytes);
                Console.WriteLine($"PDF saved to: {_pdfPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading PDF: {ex.Message}");
                throw;
            }
        }
    }
}