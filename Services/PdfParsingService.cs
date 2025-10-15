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
    public class PdfParsingService
    {
        private readonly string _pdfPath;
        private List<Course>? _cachedCourses;
        private readonly object _lock = new();

        public PdfParsingService(string pdfPath)
        {
            _pdfPath = pdfPath;
            Console.WriteLine($"PDF Path configured: {_pdfPath}");

            var directory = Path.GetDirectoryName(_pdfPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine($"Created directory: {directory}");
            }
        }

        public async Task<List<Course>> GetAllCoursesAsync()
        {
            lock (_lock)
            {
                if (_cachedCourses != null)
                {
                    Console.WriteLine($"Returning cached courses: {_cachedCourses.Count} courses");
                    return _cachedCourses;
                }
            }

            var courses = await ParseCourseCatalogAsync();

            lock (_lock)
            {
                _cachedCourses = courses;
            }

            Console.WriteLine($"Loaded {courses.Count} courses from PDF");
            return courses;
        }

        private async Task<List<Course>> ParseCourseCatalogAsync()
        {
            var courses = new Dictionary<string, Course>();

            try
            {
                if (!File.Exists(_pdfPath))
                {
                    Console.WriteLine("PDF not found, downloading...");
                    await DownloadPdfAsync();
                }
                else
                {
                    Console.WriteLine("PDF exists, parsing...");
                }

                using (var pdf = PdfDocument.Open(_pdfPath))
                {
                    var pageCount = pdf.NumberOfPages;
                    Console.WriteLine($"PDF opened successfully. Total pages: {pageCount}");

                    // Process all pages
                    for (int pageNum = 1; pageNum <= pageCount; pageNum++)
                    {
                        try
                        {
                            var page = pdf.GetPage(pageNum);
                            var pageText = page.Text;

                            if (string.IsNullOrWhiteSpace(pageText))
                                continue;

                            // Clean the text
                            pageText = CleanText(pageText);

                            // Extract courses from this page
                            var pageCourses = ExtractCoursesFromText(pageText, pageNum);

                            foreach (var course in pageCourses)
                            {
                                if (!courses.ContainsKey(course.CourseNumber))
                                {
                                    courses[course.CourseNumber] = course;
                                }
                            }

                            if (pageNum % 100 == 0)
                            {
                                Console.WriteLine($"Processing page {pageNum}, found {courses.Count} unique courses so far...");
                            }
                        }
                        catch (Exception pageEx)
                        {
                            Console.WriteLine($"Error reading page {pageNum}: {pageEx.Message}");
                        }
                    }
                }

                Console.WriteLine($"Parsing complete. Extracted {courses.Count} courses from PDF");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing PDF: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return courses.Values.ToList();
        }

        private List<Course> ExtractCoursesFromText(string text, int pageNumber)
        {
            var courses = new List<Course>();

            try
            {
                // Pattern 1: Match course code immediately followed by course name (no space)
                // Example: ACCT6217Corporate Governance
                var pattern1 = @"([A-Z]{2,6})\s*(\d{4})([A-Z][a-z]+(?:\s+[A-Z]?[a-z]+)*(?:\s+(?:and|for|in|of|with)\s+[A-Z]?[a-z]+)*)";

                // Pattern 2: Match with potential spaces
                // Example: ACCT 6217 Corporate Governance
                var pattern2 = @"([A-Z]{2,6})\s+(\d{4})\s+([A-Z][a-z]+(?:\s+[A-Z]?[a-z]+)*(?:\s+(?:and|for|in|of|with)\s+[A-Z]?[a-z]+)*)";

                // Pattern 3: Match concatenated format more aggressively
                // Example: ACCT6217CorporateGovernanceEthicsAndFinancialReporting
                var pattern3 = @"([A-Z]{2,6})(\d{4})([A-Z][a-z]+(?:[A-Z][a-z]+)*)";

                // Try all patterns
                var allPatterns = new[] { pattern1, pattern2, pattern3 };

                foreach (var pattern in allPatterns)
                {
                    var matches = Regex.Matches(text, pattern);

                    foreach (Match match in matches)
                    {
                        var dept = match.Groups[1].Value;
                        var num = match.Groups[2].Value;
                        var courseNumber = $"{dept} {num}";
                        var courseName = match.Groups[3].Value;

                        // Skip if we already have this course
                        if (courses.Any(c => c.CourseNumber == courseNumber))
                            continue;

                        // Clean up course name - insert spaces before capitals
                        courseName = Regex.Replace(courseName, @"(?<!^)(?=[A-Z])", " ");
                        courseName = CleanCourseName(courseName);

                        // Additional cleanup for common patterns
                        courseName = FixCommonCourseNamePatterns(courseName);

                        if (!string.IsNullOrWhiteSpace(courseName) && courseName.Length > 5)
                        {
                            var course = new Course
                            {
                                CourseNumber = courseNumber,
                                CourseName = courseName,
                                Credits = ExtractCredits(text, courseNumber) ?? "4",
                                Description = "",
                                Prerequisites = new List<string>(),
                                Keywords = new List<string>()
                            };

                            AssignDepartmentAndCollege(course);
                            GenerateKeywordsFromContent(course);

                            // Special handling for AI/ML courses
                            if (IsAIMLCourse(course))
                            {
                                Console.WriteLine($"Found AI/ML course: {course.CourseNumber} - {course.CourseName}");
                            }

                            courses.Add(course);
                        }
                    }
                }

                // Also look for specific AI/ML course patterns
                var aimlPattern = @"(?:artificial\s+intelligence|machine\s+learning|deep\s+learning|neural\s+network|data\s+mining|AI\s+|ML\s+)";
                if (Regex.IsMatch(text, aimlPattern, RegexOptions.IgnoreCase))
                {
                    // Extract context around AI/ML mentions
                    var contextPattern = @"([A-Z]{2,6})\s*(\d{4})[^A-Z]*(" + aimlPattern + @")[^A-Z]*";
                    var contextMatches = Regex.Matches(text, contextPattern, RegexOptions.IgnoreCase);

                    foreach (Match match in contextMatches)
                    {
                        var dept = match.Groups[1].Value;
                        var num = match.Groups[2].Value;
                        var courseNumber = $"{dept} {num}";

                        if (!courses.Any(c => c.CourseNumber == courseNumber))
                        {
                            Console.WriteLine($"Found potential AI/ML course from context: {courseNumber}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting courses from page {pageNumber}: {ex.Message}");
            }

            return courses;
        }

        private string ExtractCredits(string text, string courseNumber)
        {
            // Look for credit info near the course number
            var creditPattern = $@"{Regex.Escape(courseNumber)}[^0-9]*(\d+(?:\.\d+)?)\s*(?:Credits?|Hours?|SH|CH)";
            var match = Regex.Match(text, creditPattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        private string FixCommonCourseNamePatterns(string name)
        {
            // Fix common concatenation issues
            var fixes = new Dictionary<string, string>
            {
                { "And", " and " },
                { "For", " for " },
                { "Of", " of " },
                { "In", " in " },
                { "With", " with " },
                { "To", " to " },
                { "The", " the " }
            };

            foreach (var fix in fixes)
            {
                // Replace only when it's between two words
                name = Regex.Replace(name, $@"(?<=[a-z]){fix.Key}(?=[A-Z])", fix.Value);
            }

            // Fix multiple spaces
            name = Regex.Replace(name, @"\s+", " ");

            return name.Trim();
        }

        private bool IsAIMLCourse(Course course)
        {
            var aimlKeywords = new[]
            {
                "artificial intelligence", "machine learning", "deep learning",
                "neural network", "data mining", "data science", "analytics",
                "pattern recognition", "computer vision", "natural language",
                "robotics", "data visualization", "algorithms", "statistical"
            };

            var courseText = $"{course.CourseName} {course.Description}".ToLower();

            return aimlKeywords.Any(keyword => courseText.Contains(keyword));
        }

        private string InsertSpacesBeforeCapitals(string text)
        {
            // Insert spaces before capital letters that follow lowercase letters
            return Regex.Replace(text, @"(?<=[a-z])(?=[A-Z])", " ");
        }

        private string CleanText(string text)
        {
            // Fix common PDF encoding issues
            text = text.Replace("\uFB01", "fi");
            text = text.Replace("\uFB02", "fl");
            text = text.Replace("\u00A0", " "); // Non-breaking space
            text = text.Replace("\uFB00", "ff");
            text = text.Replace("\uFB03", "ffi");
            text = text.Replace("\uFB04", "ffl");

            return text;
        }

        private string CleanCourseName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            name = name.Trim();

            // Remove common artifacts
            name = Regex.Replace(name, @"\s+", " "); // Normalize spaces
            name = Regex.Replace(name, @"^(and|or|for)\s+", "", RegexOptions.IgnoreCase); // Remove leading conjunctions
            name = Regex.Replace(name, @"\s+(and|or|for)$", "", RegexOptions.IgnoreCase); // Remove trailing conjunctions
            name = Regex.Replace(name, @"\s*[\d]+\s*$", ""); // Remove trailing numbers
            name = Regex.Replace(name, @"\s*[A-Z]{2,6}\s*\d{4}\s*$", ""); // Remove course codes at end

            // Remove common prefixes/suffixes that aren't part of course names
            name = Regex.Replace(name, @"^(Code|Title|Hours|Elective|Required|Complete|Course)\s+", "", RegexOptions.IgnoreCase);

            // Fix specific patterns
            name = Regex.Replace(name, @"(\d+)([A-Z])", "$1 $2"); // Add space between number and capital letter
            name = Regex.Replace(name, @"([a-z])([A-Z])", "$1 $2"); // Add space between lowercase and capital letter

            // But don't split common abbreviations
            name = Regex.Replace(name, @"\b(AI|ML|CS|IT|HCI|NLP|IoT|XR|VR|AR)\s+", "$1", RegexOptions.IgnoreCase);

            return name.Trim();
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

        private void AssignDepartmentAndCollege(Course course)
        {
            var prefix = course.CourseNumber.Split(' ')[0].ToUpper();

            course.Department = prefix switch
            {
                "CS" or "CSCI" => "Computer Science",
                "CY" => "Cybersecurity",
                "INFO" or "IS" => "Information Systems",
                "DS" or "DA" => "Data Science",
                "ME" or "MECH" => "Mechanical Engineering",
                "EECE" or "ECE" => "Electrical and Computer Engineering",
                "CIVE" or "CIV" => "Civil and Environmental Engineering",
                "IE" => "Industrial Engineering",
                "BINF" => "Bioinformatics",
                "BIOL" => "Biology",
                "CHEM" => "Chemistry",
                "PHYS" => "Physics",
                "MATH" => "Mathematics",
                "ENGR" => "Engineering",
                "PT" => "Physical Therapy",
                "MISM" => "Information Systems Management",
                "ARTG" => "Art + Design",
                "GSND" => "Game Science and Design",
                _ => prefix
            };

            course.College = prefix switch
            {
                "CS" or "CSCI" or "CY" or "INFO" or "IS" or "DS" or "DA" => "Khoury College of Computer Sciences",
                "ME" or "MECH" or "EECE" or "ECE" or "CIVE" or "CIV" or "ENGR" or "IE" => "College of Engineering",
                "BIOL" or "CHEM" or "PHYS" or "MATH" or "BINF" => "College of Science",
                "PT" => "Bouvé College of Health Sciences",
                "MISM" or "BUSN" or "ACCT" or "FINA" or "MKTG" => "D'Amore-McKim School of Business",
                "ARTG" or "GSND" => "College of Arts, Media and Design",
                _ => "Northeastern University"
            };
        }

        private void GenerateKeywordsFromContent(Course course)
        {
            // Add department prefix as keyword
            var prefix = course.CourseNumber.Split(' ')[0].ToLower();
            course.Keywords.Add(prefix);

            // Add department as keyword
            if (!string.IsNullOrEmpty(course.Department))
            {
                course.Keywords.Add(course.Department.ToLower());
            }

            // Extract keywords from course name
            var courseName = course.CourseName.ToLower();

            // Comprehensive keyword mappings
            var keywordMappings = new Dictionary<string, string[]>
            {
                { "artificial intelligence", new[] { "ai", "artificial intelligence", "machine learning" } },
                { "machine learning", new[] { "ml", "machine learning", "ai", "data science" } },
                { "deep learning", new[] { "deep learning", "neural networks", "ai", "ml" } },
                { "neural network", new[] { "neural networks", "deep learning", "ai", "ml" } },
                { "data science", new[] { "data science", "analytics", "data", "statistics" } },
                { "data mining", new[] { "data mining", "machine learning", "analytics", "data science" } },
                { "data visual", new[] { "data visualization", "visualization", "data", "analytics" } },
                { "algorithm", new[] { "algorithms", "programming", "computer science" } },
                { "pattern recognition", new[] { "pattern recognition", "ml", "computer vision", "ai" } },
                { "computer vision", new[] { "computer vision", "ai", "ml", "image processing" } },
                { "natural language", new[] { "nlp", "natural language processing", "ai", "text mining" } },
                { "human-computer", new[] { "hci", "human-computer interaction", "interaction", "ux" } },
                { "robotics", new[] { "robotics", "robots", "engineering", "ai" } },
                { "database", new[] { "database", "data", "sql", "data management" } },
                { "software", new[] { "software", "programming", "development" } },
                { "programming", new[] { "programming", "software", "coding" } },
                { "statistics", new[] { "statistics", "statistical", "data", "analytics" } },
                { "numerical", new[] { "numerical methods", "optimization", "mathematics" } },
                { "control", new[] { "control systems", "engineering", "automation" } },
                { "mechanics", new[] { "mechanics", "mechanical", "engineering" } },
                { "mixed reality", new[] { "mixed reality", "vr", "ar", "virtual reality", "augmented reality" } },
                { "empirical", new[] { "research methods", "empirical research", "data analysis" } },
                { "analytics", new[] { "analytics", "data analytics", "business analytics", "data science" } },
                { "bioinformatics", new[] { "bioinformatics", "computational biology", "data science" } },
                { "cybersecurity", new[] { "cybersecurity", "security", "information security" } },
                { "cloud", new[] { "cloud computing", "distributed systems", "aws", "azure" } },
                { "big data", new[] { "big data", "data engineering", "hadoop", "spark" } }
            };

            foreach (var mapping in keywordMappings)
            {
                if (courseName.Contains(mapping.Key))
                {
                    course.Keywords.AddRange(mapping.Value);
                }
            }

            // Remove duplicates
            course.Keywords = course.Keywords.Distinct().ToList();
        }
    }
}