using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Northeastern_Personal_Workspace.Models;
using Northeastern_Personal_Workspace.Services;
using System.IO;

namespace Northeastern_Personal_Workspace.Controllers
{
    public class CourseController : Controller
    {
        private readonly PdfParsingService _pdfService;
        private readonly IWebHostEnvironment _env;

        public CourseController(PdfParsingService pdfService, IWebHostEnvironment env)
        {
            _pdfService = pdfService;
            _env = env;
        }

        public async Task<IActionResult> Index(string searchQuery = "")
        {
            var stopwatch = Stopwatch.StartNew();

            var model = new CourseSearchViewModel
            {
                SearchQuery = searchQuery ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var allCourses = await _pdfService.GetAllCoursesAsync();
                Console.WriteLine($"Total courses available: {allCourses.Count}");

                model.Results = SearchCourses(allCourses, searchQuery);
                model.TotalResults = model.Results.Count;

                Console.WriteLine($"Search for '{searchQuery}' returned {model.TotalResults} results");
            }

            stopwatch.Stop();
            model.SearchTime = $"{stopwatch.ElapsedMilliseconds}ms";

            return View(model);
        }

        // Debug action to test PDF parsing
        public async Task<IActionResult> Debug()
        {
            var courses = await _pdfService.GetAllCoursesAsync();

            var debugInfo = new
            {
                TotalCourses = courses.Count,
                SampleCourses = courses.Take(10).Select(c => new
                {
                    c.CourseNumber,
                    c.CourseName,
                    c.Credits,
                    c.Department,
                    KeywordCount = c.Keywords.Count
                }),
                DepartmentCounts = courses.GroupBy(c => c.Department)
                    .Select(g => new { Department = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
            };

            return Json(debugInfo);
        }

        // Check PDF status
        public IActionResult PdfStatus()
        {
            var pdfPath = Path.Combine(_env.WebRootPath, "data", "course_catalog.pdf");

            var status = new
            {
                PdfPath = pdfPath,
                Exists = System.IO.File.Exists(pdfPath),
                FileSize = System.IO.File.Exists(pdfPath) ? new FileInfo(pdfPath).Length : 0,
                FileSizeMB = System.IO.File.Exists(pdfPath) ? (new FileInfo(pdfPath).Length / 1024.0 / 1024.0).ToString("F2") + " MB" : "N/A",
                LastModified = System.IO.File.Exists(pdfPath) ? System.IO.File.GetLastWriteTime(pdfPath).ToString() : "N/A",
                DataFolderExists = Directory.Exists(Path.Combine(_env.WebRootPath, "data")),
                WebRootPath = _env.WebRootPath
            };

            return Json(status);
        }

        // Force download PDF
        public async Task<IActionResult> DownloadPdf()
        {
            try
            {
                var pdfPath = Path.Combine(_env.WebRootPath, "data", "course_catalog.pdf");
                var dataDir = Path.GetDirectoryName(pdfPath);

                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir!);
                }

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(5);

                var url = "https://catalog.northeastern.edu/pdf/Northeastern%20University%202024-2025%20Graduate%20Catalog.pdf";

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(pdfPath, pdfBytes);

                return Json(new
                {
                    Success = true,
                    Message = "PDF downloaded successfully",
                    FileSize = pdfBytes.Length,
                    FileSizeMB = (pdfBytes.Length / 1024.0 / 1024.0).ToString("F2") + " MB",
                    Path = pdfPath
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        // Test PDF parsing with a simple text file
        public async Task<IActionResult> TestParsing()
        {
            var pdfPath = Path.Combine(_env.WebRootPath, "data", "course_catalog.pdf");

            if (!System.IO.File.Exists(pdfPath))
            {
                return Json(new { Error = "PDF not found. Run /Course/DownloadPdf first" });
            }

            try
            {
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
                var pageCount = pdf.NumberOfPages;
                var firstPageText = "";
                var page10Text = "";
                var page50Text = "";

                if (pageCount > 0)
                {
                    firstPageText = pdf.GetPage(1).Text.Substring(0, Math.Min(500, pdf.GetPage(1).Text.Length));
                }
                if (pageCount > 10)
                {
                    page10Text = pdf.GetPage(10).Text.Substring(0, Math.Min(500, pdf.GetPage(10).Text.Length));
                }
                if (pageCount > 50)
                {
                    page50Text = pdf.GetPage(50).Text.Substring(0, Math.Min(500, pdf.GetPage(50).Text.Length));
                }

                // Try to find course patterns
                var sampleText = pdf.GetPage(Math.Min(100, pageCount)).Text;
                var patterns = new[]
                {
                    @"([A-Z]{2,4})\s+(\d{4})",
                    @"([A-Z]{2,4})\s+(\d{4})\.",
                    @"([A-Z]{2,4})\s+(\d{4})\s+([^.]+)\."
                };

                var patternMatches = new Dictionary<string, int>();
                foreach (var pattern in patterns)
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(sampleText, pattern);
                    patternMatches[pattern] = matches.Count;
                }

                return Json(new
                {
                    Success = true,
                    PageCount = pageCount,
                    FirstPagePreview = firstPageText,
                    Page10Preview = page10Text,
                    Page50Preview = page50Text,
                    PatternMatches = patternMatches,
                    SampleCourseNumbers = System.Text.RegularExpressions.Regex.Matches(sampleText, @"([A-Z]{2,4})\s+(\d{4})")
                        .Cast<System.Text.RegularExpressions.Match>()
                        .Take(10)
                        .Select(m => m.Value)
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Error = ex.Message,
                    Type = ex.GetType().Name,
                    StackTrace = ex.StackTrace
                });
            }
        }

        private List<CourseResult> SearchCourses(List<Course> allCourses, string searchQuery)
        {
            // Parse search terms
            var searchTerms = searchQuery.ToLower()
                .Split(new[] { '+', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length >= 2)
                .Distinct()
                .ToList();

            Console.WriteLine($"Search terms: {string.Join(", ", searchTerms)}");

            var results = new List<CourseResult>();

            foreach (var course in allCourses)
            {
                var matchedTerms = new List<string>();
                double relevanceScore = 0;

                foreach (var term in searchTerms)
                {
                    // Check course number (highest weight)
                    if (course.CourseNumber.ToLower().Contains(term))
                    {
                        matchedTerms.Add(term);
                        relevanceScore += 10;
                    }

                    // Check course name (high weight)
                    if (course.CourseName.ToLower().Contains(term))
                    {
                        matchedTerms.Add(term);
                        relevanceScore += 8;
                    }

                    // Check keywords (medium weight)
                    if (course.Keywords.Any(k => k.Contains(term)))
                    {
                        matchedTerms.Add(term);
                        relevanceScore += 5;
                    }

                    // Check description (lower weight)
                    if (!string.IsNullOrEmpty(course.Description) && course.Description.ToLower().Contains(term))
                    {
                        matchedTerms.Add(term);
                        relevanceScore += 3;
                    }

                    // Check department/college
                    if ((course.Department?.ToLower().Contains(term) ?? false) ||
                        (course.College?.ToLower().Contains(term) ?? false))
                    {
                        matchedTerms.Add(term);
                        relevanceScore += 4;
                    }
                }

                if (matchedTerms.Any())
                {
                    var result = new CourseResult
                    {
                        CourseNumber = course.CourseNumber,
                        CourseName = course.CourseName,
                        Credits = course.Credits,
                        Program = course.Program,
                        Department = course.Department,
                        College = course.College,
                        Concentrations = course.Concentrations,
                        CourseType = course.CourseType,
                        Description = course.Description,
                        Prerequisites = course.Prerequisites,
                        Keywords = course.Keywords,
                        MatchedTerms = matchedTerms.Distinct().ToList(),
                        RelevanceScore = relevanceScore,
                        WhyRelevant = GenerateRelevanceExplanation(course, matchedTerms.Distinct().ToList())
                    };

                    results.Add(result);

                    if (results.Count <= 5) // Log first 5 matches
                    {
                        Console.WriteLine($"Match: {course.CourseNumber} - Score: {relevanceScore}");
                    }
                }
            }

            // Sort by relevance score
            return results.OrderByDescending(r => r.RelevanceScore)
                         .ThenBy(r => r.CourseNumber)
                         .ToList();
        }

        private string GenerateRelevanceExplanation(Course course, List<string> matchedTerms)
        {
            var reasons = new List<string>();

            foreach (var term in matchedTerms)
            {
                if (course.CourseNumber.ToLower().Contains(term))
                    reasons.Add($"course number matches '{term}'");
                else if (course.CourseName.ToLower().Contains(term))
                    reasons.Add($"title contains '{term}'");
                else if (course.Keywords.Any(k => k.Contains(term)))
                    reasons.Add($"related to '{term}'");
                else if (course.Description?.ToLower().Contains(term) ?? false)
                    reasons.Add($"covers topics in '{term}'");
            }

            var reasonText = string.Join(", ", reasons.Take(3));
            var descPreview = string.IsNullOrEmpty(course.Description) ? ""
                : course.Description.Length > 150
                    ? course.Description.Substring(0, 150) + "..."
                    : course.Description;

            return $"This course is relevant because {reasonText}. {descPreview}";
        }

        // Add this enhanced TestParsing action to your CourseController

        public async Task<IActionResult> TestParsingComprehensive()
        {
            var pdfPath = Path.Combine(_env.WebRootPath, "data", "course_catalog.pdf");

            if (!System.IO.File.Exists(pdfPath))
            {
                return Json(new { Error = "PDF not found. Run /Course/DownloadPdf first" });
            }

            try
            {
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
                var pageCount = pdf.NumberOfPages;

                // Search for course patterns in different sections of the PDF
                var coursePatterns = new[]
                {
            @"([A-Z]{2,4})\s+(\d{4})\.\s+([^.]+)\.\s*\((\d+)\s*(?:Hours?|Credits?|Semester Hours?)\)",
            @"([A-Z]{2,4})\s+(\d{4})\s+([^(]+)\s*\((\d+)\s*SH\)",
            @"([A-Z]{2,4})\s+(\d{4})\s*[-–]\s*([^(]+)",
            @"^([A-Z]{2,4})\s+(\d{4})",
            @"([A-Z]{2,4})\s+(\d{4}):?\s+([^\n]+)",
            @"Course:\s*([A-Z]{2,4})\s+(\d{4})"
        };

                var results = new List<object>();
                var pageSamples = new List<object>();

                // Sample pages throughout the document
                var samplePages = new[] { 1, 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };

                foreach (var pageNum in samplePages)
                {
                    if (pageNum <= pageCount)
                    {
                        var pageText = pdf.GetPage(pageNum).Text;
                        var preview = pageText.Length > 200 ? pageText.Substring(0, 200) : pageText;

                        // Check each pattern
                        var patternResults = new Dictionary<string, List<string>>();
                        foreach (var pattern in coursePatterns)
                        {
                            var matches = System.Text.RegularExpressions.Regex.Matches(pageText, pattern, System.Text.RegularExpressions.RegexOptions.Multiline);
                            if (matches.Count > 0)
                            {
                                patternResults[pattern] = matches.Cast<System.Text.RegularExpressions.Match>()
                                    .Take(5)
                                    .Select(m => m.Value)
                                    .ToList();
                            }
                        }

                        pageSamples.Add(new
                        {
                            PageNumber = pageNum,
                            Preview = preview,
                            PatternMatches = patternResults,
                            HasCourseContent = patternResults.Any()
                        });
                    }
                }

                // Try to find pages with the most course-like content
                var coursePagesSearch = new List<object>();
                for (int i = 100; i < Math.Min(pageCount, 1100); i += 50)
                {
                    var pageText = pdf.GetPage(i).Text;
                    var courseMatches = System.Text.RegularExpressions.Regex.Matches(pageText, @"([A-Z]{2,4})\s+(\d{4})");
                    if (courseMatches.Count > 5) // If we find more than 5 potential course numbers
                    {
                        coursePagesSearch.Add(new
                        {
                            Page = i,
                            CourseCount = courseMatches.Count,
                            SampleCourses = courseMatches.Cast<System.Text.RegularExpressions.Match>()
                                .Take(10)
                                .Select(m => m.Value)
                                .ToList(),
                            TextSample = pageText.Substring(0, Math.Min(500, pageText.Length))
                        });
                    }
                }

                // Look for specific keywords that might indicate course sections
                var keywordPages = new List<object>();
                var keywords = new[] { "Course Descriptions", "Course List", "Graduate Courses", "Course Offerings", "COURSE DESCRIPTIONS", "Courses" };

                for (int i = 1; i <= Math.Min(pageCount, 200); i++)
                {
                    var pageText = pdf.GetPage(i).Text;
                    foreach (var keyword in keywords)
                    {
                        if (pageText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            keywordPages.Add(new
                            {
                                Page = i,
                                Keyword = keyword,
                                Context = GetContextAroundKeyword(pageText, keyword, 100)
                            });
                        }
                    }
                }

                return Json(new
                {
                    Success = true,
                    TotalPages = pageCount,
                    PageSamples = pageSamples,
                    CoursePagesFound = coursePagesSearch,
                    KeywordPagesFound = keywordPages,
                    Recommendation = coursePagesSearch.Any() ?
                        $"Found potential course listings around pages: {string.Join(", ", coursePagesSearch.Select(p => ((dynamic)p).Page))}" :
                        "No clear course listing pages found. May need manual inspection of PDF structure."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Error = ex.Message,
                    Type = ex.GetType().Name
                });
            }
        }

        private string GetContextAroundKeyword(string text, string keyword, int contextLength)
        {
            var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index == -1) return "";

            var start = Math.Max(0, index - contextLength);
            var length = Math.Min(text.Length - start, contextLength * 2 + keyword.Length);

            return text.Substring(start, length).Replace("\n", " ").Replace("\r", " ");
        }

        // Add this alternative parsing action that looks for specific course formats
        public async Task<IActionResult> FindCourseFormat()
        {
            var pdfPath = Path.Combine(_env.WebRootPath, "data", "course_catalog.pdf");

            if (!System.IO.File.Exists(pdfPath))
            {
                return Json(new { Error = "PDF not found" });
            }

            try
            {
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfPath);

                // Look for pages containing department abbreviations
                var deptAbbreviations = new[] { "ACCT", "BINF", "BIOE", "BIOL", "CHEM", "COS", "CS", "CY", "DS", "ECON", "EECE", "INFO", "MATH", "ME", "PHYS" };
                var foundDepartments = new Dictionary<string, List<int>>();

                for (int i = 1; i <= Math.Min(pdf.NumberOfPages, 1162); i++)
                {
                    var pageText = pdf.GetPage(i).Text;

                    foreach (var dept in deptAbbreviations)
                    {
                        // Look for department code followed by 4 digits
                        var pattern = $@"\b{dept}\s+\d{{4}}\b";
                        if (System.Text.RegularExpressions.Regex.IsMatch(pageText, pattern))
                        {
                            if (!foundDepartments.ContainsKey(dept))
                                foundDepartments[dept] = new List<int>();

                            foundDepartments[dept].Add(i);

                            // Get a sample of the match
                            var match = System.Text.RegularExpressions.Regex.Match(pageText, pattern);
                            if (match.Success && foundDepartments[dept].Count == 1) // Only for first occurrence
                            {
                                var contextStart = Math.Max(0, match.Index - 50);
                                var contextEnd = Math.Min(pageText.Length, match.Index + match.Length + 200);
                                var context = pageText.Substring(contextStart, contextEnd - contextStart);

                                Console.WriteLine($"Found {dept} on page {i}: {context.Replace("\n", " ")}");
                            }
                        }
                    }
                }

                return Json(new
                {
                    Success = true,
                    DepartmentsFound = foundDepartments.Select(kvp => new
                    {
                        Department = kvp.Key,
                        PagesFound = kvp.Value.Take(5).ToList(), // First 5 pages where found
                        TotalOccurrences = kvp.Value.Count
                    }).OrderByDescending(x => x.TotalOccurrences),
                    Recommendation = foundDepartments.Any() ?
                        "Found course patterns. Check the console output for examples." :
                        "No standard course patterns found."
                });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Error = ex.Message });
            }
        }
        // Add this action to your CourseController to examine the PDF content

        public async Task<IActionResult> ExaminePdf(int startPage = 360, int endPage = 370)
        {
            var pdfPath = Path.Combine(_env.WebRootPath, "data", "course_catalog.pdf");

            if (!System.IO.File.Exists(pdfPath))
            {
                return Json(new { Error = "PDF not found" });
            }

            var results = new List<object>();

            try
            {
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfPath);

                for (int pageNum = startPage; pageNum <= Math.Min(endPage, pdf.NumberOfPages); pageNum++)
                {
                    var page = pdf.GetPage(pageNum);
                    var pageText = page.Text;

                    // Look for AI-related content
                    if (pageText.ToLower().Contains("artificial intelligence") ||
                        pageText.ToLower().Contains("machine learning") ||
                        pageText.ToLower().Contains("cs 5100") ||
                        pageText.ToLower().Contains("cs 6140"))
                    {
                        // Extract a portion of the page to see the structure
                        var lines = pageText.Split('\n').Take(50).ToList();

                        results.Add(new
                        {
                            Page = pageNum,
                            SampleLines = lines,
                            FullTextLength = pageText.Length,
                            ContainsTable = pageText.Contains("Code") && pageText.Contains("Title") && pageText.Contains("Hours"),
                            CourseMatches = System.Text.RegularExpressions.Regex.Matches(pageText, @"CS\s+\d{4}").Count
                        });
                    }
                }

                return Json(new
                {
                    Success = true,
                    Results = results,
                    Message = results.Any() ? "Found AI-related content" : "No AI content found in this range"
                });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Error = ex.Message });
            }
        }

        // Add this action to extract courses from a specific page
        public async Task<IActionResult> ExtractFromPage(int pageNumber)
        {
            var pdfPath = Path.Combine(_env.WebRootPath, "data", "course_catalog.pdf");

            if (!System.IO.File.Exists(pdfPath))
            {
                return Json(new { Error = "PDF not found" });
            }

            try
            {
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
                var page = pdf.GetPage(pageNumber);
                var pageText = page.Text;

                // Clean text
                pageText = pageText.Replace("\uFB01", "fi");

                // Try multiple extraction patterns
                var patterns = new[]
                {
            // Pattern 1: Standard table format
            @"([A-Z]{2,4}\s+\d{4})\s+([A-Za-z][^0-9\n]{10,100}?)\s+(\d+)",
            
            // Pattern 2: Multi-line format
            @"([A-Z]{2,4}\s+\d{4})\n([A-Za-z][^0-9\n]{10,100}?)\n(\d+)",
            
            // Pattern 3: With dots or other separators
            @"([A-Z]{2,4}\s+\d{4})[\s\.]+([A-Za-z][^0-9\n]{10,100}?)[\s\.]+(\d+)"
        };

                var extractedCourses = new List<object>();

                foreach (var pattern in patterns)
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(pageText, pattern);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        extractedCourses.Add(new
                        {
                            Pattern = pattern,
                            CourseCode = match.Groups[1].Value,
                            CourseName = match.Groups[2].Value.Trim(),
                            Credits = match.Groups[3].Value,
                            FullMatch = match.Value
                        });
                    }
                }

                // Also show the raw text structure
                var lines = pageText.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

                return Json(new
                {
                    PageNumber = pageNumber,
                    ExtractedCourses = extractedCourses,
                    TotalLines = lines.Count,
                    SampleLines = lines.Take(100),
                    RawTextSample = pageText.Substring(0, Math.Min(2000, pageText.Length))
                });
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message });
            }
        }
    }
}