// Controllers/HomeController.cs - Updated with New Bubble Chart Data Source
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Northeastern_Personal_Workspace.Models;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using HtmlAgilityPack;
using System.Xml;
using System.Diagnostics;

namespace okta_aspnetcore_mvc_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        
        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;

        }

        // Existing methods...
        public IActionResult Index()
        {
            // Your existing code...

            // Optional: Add to dashboards list if you decide to use ViewBag approach
            ViewBag.Dashboards = new List<DashboardInfo>
    {
        new DashboardInfo
        {
            Name = "COE & COS Enrollment Dashboard",
            Description = "View enrollment data with interactive charts and filters",
            Controller = "Dashboard",
            Action = "Index",
            Icon = "fa-graduation-cap"
        } };

            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        public IActionResult Profile()
        {
            return View(HttpContext.User.Claims);
        }

        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            _logger.LogError($"An error occurred at {exceptionHandlerPathFeature?.Path}. Error: {exceptionHandlerPathFeature?.Error?.Message}");
            ViewBag.ErrorMessage = exceptionHandlerPathFeature?.Error?.Message;
            return View();
        }

        // UPDATED BUBBLE CHART METHODS WITH NEW DATA SOURCE

        [AllowAnonymous]
        public async Task<IActionResult> BubbleChartVisualization()
        {
            try
            {
                _logger.LogInformation("Starting Bubble Chart Visualization data loading from new data source...");

                var viewModel = await GetBubbleChartDataAsync();

                if (viewModel.TotalProgramsCount == 0)
                {
                    _logger.LogError("No programs loaded for bubble chart from new data source");
                    ViewBag.ErrorMessage = "Failed to load program data from Google Sheets. Please check the connection and try again.";
                    ViewBag.DataSource = "Google Sheets - Bubble Chart Data (Failed)";
                }
                else
                {
                    ViewBag.DataSource = "Google Sheets - Bubble Chart Data";
                    ViewBag.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    ViewBag.Message = $"Bubble Chart Visualization - {viewModel.TotalProgramsCount} Programs Loaded from New Data Source";
                }

                ViewBag.CurrentTime = DateTime.Now.ToString();
                _logger.LogInformation($"Successfully loaded {viewModel.TotalProgramsCount} programs for bubble chart from new data source");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading bubble chart visualization from new data source: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                ViewBag.ErrorMessage = $"Unable to load bubble chart data from Google Sheets: {ex.Message}";
                ViewBag.DataSource = "Google Sheets - Bubble Chart Data (Error)";

                var errorViewModel = new BubbleChartViewModel();
                return View(errorViewModel);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetBubbleChartData([FromQuery] BubbleChartFilter filter = null)
        {
            try
            {
                var viewModel = await GetBubbleChartDataAsync();

                if (filter != null)
                {
                    viewModel.Programs = ApplyBubbleChartFilter(viewModel.Programs, filter);
                    viewModel.ProcessData(); // Recalculate statistics after filtering
                }

                var response = new BubbleChartApiResponse
                {
                    Success = true,
                    Message = "Data loaded successfully from new bubble chart data source",
                    Data = viewModel,
                    TotalRecords = viewModel.TotalProgramsCount,
                    FilteredRecords = viewModel.Programs.Count,
                    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DataSource = "Google Sheets - Bubble Chart Data"
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching bubble chart data from new source: {ex.Message}");
                return StatusCode(500, new BubbleChartApiResponse
                {
                    Success = false,
                    Message = $"Error loading data from new source: {ex.Message}",
                    Data = new BubbleChartViewModel()
                });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetFilteredBubbleChartData([FromBody] BubbleChartFilter filter)
        {
            try
            {
                var allData = await GetBubbleChartDataAsync();
                var filteredPrograms = ApplyBubbleChartFilter(allData.Programs, filter);

                var filteredViewModel = new BubbleChartViewModel
                {
                    Programs = filteredPrograms,
                    DataSource = "Google Sheets - Bubble Chart Data",
                    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                filteredViewModel.ProcessData();

                var response = new BubbleChartApiResponse
                {
                    Success = true,
                    Message = $"Filtered {filteredPrograms.Count} programs from {allData.TotalProgramsCount} total",
                    Data = filteredViewModel,
                    TotalRecords = allData.TotalProgramsCount,
                    FilteredRecords = filteredPrograms.Count
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error applying bubble chart filter: {ex.Message}");
                return StatusCode(500, new BubbleChartApiResponse
                {
                    Success = false,
                    Message = $"Error filtering data: {ex.Message}"
                });
            }
        }

        // UPDATED METHOD TO USE NEW BUBBLE CHART DATA SOURCE
        private async Task<BubbleChartViewModel> GetBubbleChartDataAsync()
        {
            // Use the new bubble chart specific data source instead of combining other sources
            var bubbleChartPrograms = await GetBubbleChartProgramsFromGoogleSheetsAsync();

            var viewModel = new BubbleChartViewModel
            {
                Programs = bubbleChartPrograms,
                DataSource = "Google Sheets - Bubble Chart Data",
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            viewModel.ProcessData();
            return viewModel;
        }

        // NEW METHOD TO FETCH DATA FROM THE SPECIFIC BUBBLE CHART SHEET
        private async Task<List<BubbleChartProgram>> GetBubbleChartProgramsFromGoogleSheetsAsync()
        {
            try
            {
                // Updated URL to use the specific GID for bubble chart data (1065445862)
                const string BUBBLE_CHART_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQA00pBsgS8EvPEZFA2j6S_QraXI4G4cg_77m-uKgQ7_990xsfhKvibli0FhtHI5ysIYiJ7_tFpEuT7/pub?gid=1065445862&single=true&output=csv";

                _logger.LogInformation($"Fetching bubble chart data from: {BUBBLE_CHART_CSV_URL}");

                var csv = await _httpClient.GetStringAsync(BUBBLE_CHART_CSV_URL);

                _logger.LogInformation($"Bubble chart CSV data received: {csv.Length} characters");

                return ParseBubbleChartCSVContent(csv);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching bubble chart data from Google Sheets: {ex.Message}");
                return new List<BubbleChartProgram>();
            }
        }

        // UPDATED METHOD TO PARSE BUBBLE CHART SPECIFIC CSV DATA WITH BETTER LOGGING
        private List<BubbleChartProgram> ParseBubbleChartCSVContent(string csvContent)
        {
            var programs = new List<BubbleChartProgram>();
            var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
            {
                _logger.LogWarning("Bubble chart CSV has insufficient data");
                return programs;
            }

            // Log the header row for debugging
            _logger.LogInformation($"Bubble chart CSV header: {lines[0]}");

            // Verify header structure
            var headerColumns = ParseCSVValues(lines[0]);
            _logger.LogInformation($"Expected 13 columns, found {headerColumns.Count} columns in header");

            if (headerColumns.Count < 13)
            {
                _logger.LogWarning($"Header has only {headerColumns.Count} columns, expected 13. Header: {string.Join(" | ", headerColumns)}");
            }

            int successCount = 0;
            int errorCount = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var program = ParseBubbleChartCSVLine(lines[i], i);
                if (program != null)
                {
                    programs.Add(program);
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }

            _logger.LogInformation($"Parsed {successCount} bubble chart programs from CSV (Errors: {errorCount})");

            // Log sample of parsed data for verification
            if (programs.Count > 0)
            {
                var sample = programs.Take(3);
                foreach (var prog in sample)
                {
                    _logger.LogInformation($"Sample: {prog.ProgramName} | {prog.DegreeLevel} | {prog.TotalAiMlCourses} courses | Complexity: {prog.ComplexityScore}");
                }
            }

            return programs;
        }

        // UPDATED METHOD TO PARSE INDIVIDUAL BUBBLE CHART CSV LINES WITH CORRECT COLUMN MAPPING
        private BubbleChartProgram ParseBubbleChartCSVLine(string line, int rowIndex)
        {
            try
            {
                var values = ParseCSVValues(line);

                if (values.Count < 13) // Now expecting 13 columns as per your specification
                {
                    _logger.LogWarning($"Bubble chart CSV line {rowIndex} has insufficient columns: {values.Count}, expected 13");
                    return null;
                }

                // Column mapping according to your specification:
                // 0: Program Name
                // 1: Degree Level  
                // 2: Degree Type
                // 3: Total Courses
                // 4: Required
                // 5: Elective
                // 6: Capstone
                // 7: Complexity Score
                // 8: Complexity Level (not used in model but available)
                // 9: Academic Domain
                // 10: Level 4 Topics (count)
                // 11: Top Level 4 Topics (text/description)
                // 12: Program Strengths

                string programName = values[0]?.Trim() ?? "";
                if (string.IsNullOrEmpty(programName))
                {
                    _logger.LogWarning($"Bubble chart CSV line {rowIndex} has empty program name");
                    return null;
                }

                string degreeLevel = values[1]?.Trim() ?? "Graduate";
                string degreeType = values[2]?.Trim() ?? "";

                // Parse numeric values with proper validation
                int totalCourses = 0;
                if (!int.TryParse(values[3]?.Trim(), out totalCourses))
                {
                    _logger.LogWarning($"Invalid total courses value at line {rowIndex}: '{values[3]}'");
                    totalCourses = 5; // Default value
                }

                int requiredCourses = 0;
                if (!int.TryParse(values[4]?.Trim(), out requiredCourses))
                {
                    requiredCourses = Math.Max(1, totalCourses / 2); // Default to half of total
                }

                int electiveCourses = 0;
                if (!int.TryParse(values[5]?.Trim(), out electiveCourses))
                {
                    electiveCourses = totalCourses - requiredCourses; // Default to remainder
                }

                int capstoneCourses = 0;
                if (!int.TryParse(values[6]?.Trim(), out capstoneCourses))
                {
                    capstoneCourses = 0; // Default to 0 if not specified
                }

                decimal complexityScore = 0;
                if (!decimal.TryParse(values[7]?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out complexityScore))
                {
                    _logger.LogWarning($"Invalid complexity score at line {rowIndex}: '{values[7]}'");
                    complexityScore = CalculateDefaultComplexityScore(degreeLevel, totalCourses); // Calculate based on other factors
                }

                // Column 8 is Complexity Level (text description) - we can use this for validation but don't store it
                string complexityLevel = values[8]?.Trim() ?? "";

                string academicDomain = values[9]?.Trim() ?? "";
                if (string.IsNullOrEmpty(academicDomain))
                {
                    academicDomain = DetermineDomainFromName(programName);
                }

                int level4TopicsCount = 0;
                if (!int.TryParse(values[10]?.Trim(), out level4TopicsCount))
                {
                    level4TopicsCount = EstimateLevel4Topics(totalCourses, complexityScore);
                }

                // Use the actual "Top Level 4 Topics" text from column 11
                string topLevel4Topics = values[11]?.Trim() ?? "";
                if (string.IsNullOrEmpty(topLevel4Topics))
                {
                    topLevel4Topics = GenerateAdvancedTopicsFromDomain(academicDomain, level4TopicsCount);
                }

                // Use the actual "Program Strengths" from column 12
                string programStrengths = values[12]?.Trim() ?? "";
                if (string.IsNullOrEmpty(programStrengths))
                {
                    programStrengths = GenerateProgramStrengthsFromData(programName, degreeLevel, degreeType, totalCourses);
                }

                var program = new BubbleChartProgram
                {
                    Id = rowIndex,
                    ProgramName = programName,
                    DegreeLevel = degreeLevel,
                    DegreeType = degreeType,
                    TotalAiMlCourses = totalCourses,
                    RequiredCourses = requiredCourses,
                    ElectiveCourses = electiveCourses,
                    CapstoneCourses = capstoneCourses,
                    ComplexityScore = complexityScore,
                    AdvancedTopicsCount = level4TopicsCount,
                    AdvancedTopicsCoverage = topLevel4Topics,
                    ProgramStrengths = programStrengths,
                    AcademicDomain = NormalizeDomainName(academicDomain)
                };

                _logger.LogDebug($"Parsed bubble chart program: {program.ProgramName} - {program.DegreeLevel} - {program.TotalAiMlCourses} courses - Complexity: {program.ComplexityScore}");
                return program;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing bubble chart CSV line {rowIndex}: {ex.Message}");
                _logger.LogError($"Line content: {line}");
                return null;
            }
        }

        // HELPER METHOD TO CALCULATE DEFAULT COMPLEXITY SCORE
        private decimal CalculateDefaultComplexityScore(string degreeLevel, int totalCourses)
        {
            decimal baseScore = 2.0m;

            // Adjust based on degree level
            if (degreeLevel.ToLower().Contains("phd") || degreeLevel.ToLower().Contains("doctor"))
                baseScore = 4.5m;
            else if (degreeLevel.ToLower().Contains("graduate") || degreeLevel.ToLower().Contains("master"))
                baseScore = 3.0m;
            else if (degreeLevel.ToLower().Contains("certificate"))
                baseScore = 2.5m;
            else if (degreeLevel.ToLower().Contains("undergraduate"))
                baseScore = 2.0m;

            // Adjust based on course count
            if (totalCourses > 15)
                baseScore += 0.5m;
            else if (totalCourses > 10)
                baseScore += 0.2m;
            else if (totalCourses < 6)
                baseScore -= 0.3m;

            return Math.Min(5.0m, Math.Max(1.0m, baseScore)); // Clamp between 1.0 and 5.0
        }

        // HELPER METHOD TO ESTIMATE LEVEL 4 TOPICS BASED ON OTHER FACTORS
        private int EstimateLevel4Topics(int totalCourses, decimal complexityScore)
        {
            // Estimate advanced topics based on total courses and complexity
            int estimatedTopics = (int)Math.Round(totalCourses * 0.3m * (complexityScore / 3.0m));
            return Math.Max(0, Math.Min(estimatedTopics, 10)); // Clamp between 0 and 10
        }

        // HELPER METHOD TO GENERATE PROGRAM STRENGTHS FROM PARSED DATA
        private string GenerateProgramStrengthsFromData(string programName, string degreeLevel, string degreeType, int totalCourses)
        {
            var strengths = new List<string>();

            // Add strengths based on degree level and type
            if (degreeLevel.ToLower().Contains("phd") || degreeType.ToLower().Contains("phd"))
                strengths.Add("Research Focus");

            if (degreeLevel.ToLower().Contains("graduate") || degreeLevel.ToLower().Contains("master"))
                strengths.Add("Advanced Curriculum");

            if (degreeType.ToLower().Contains("certificate"))
                strengths.Add("Focused Specialization");

            if (degreeType.ToLower().Contains("minor"))
                strengths.Add("Complementary Skills");

            // Add strengths based on course count
            if (totalCourses > 15)
                strengths.Add("Comprehensive Program");
            else if (totalCourses > 10)
                strengths.Add("Substantial Coverage");
            else if (totalCourses <= 6)
                strengths.Add("Efficient Learning");

            // Add strengths based on program name content
            if (programName.ToLower().Contains("data") || programName.ToLower().Contains("analytics"))
                strengths.Add("Data-Driven");

            if (programName.ToLower().Contains("machine learning") || programName.ToLower().Contains("ai"))
                strengths.Add("AI/ML Focus");

            if (programName.ToLower().Contains("cyber") || programName.ToLower().Contains("security"))
                strengths.Add("Security Emphasis");

            // Ensure we always have at least one strength
            if (strengths.Count == 0)
                strengths.Add("Industry Relevant");

            return string.Join(", ", strengths);
        }

        // Keep existing conversion methods for backward compatibility
        private BubbleChartProgram ConvertToBubbleChartProgram(GraduateProgram gradProgram)
        {
            return new BubbleChartProgram
            {
                Id = gradProgram.Id,
                ProgramName = gradProgram.ProgramName,
                DegreeLevel = "Graduate",
                DegreeType = gradProgram.DegreeType,
                TotalAiMlCourses = gradProgram.TotalCourses,
                RequiredCourses = gradProgram.Required,
                ElectiveCourses = gradProgram.Elective,
                CapstoneCourses = gradProgram.Capstone,
                ComplexityScore = gradProgram.ComplexityScore,
                AdvancedTopicsCount = gradProgram.Level4Topics,
                AdvancedTopicsCoverage = GenerateAdvancedTopicsFromDomain(gradProgram.AcademicDomain, gradProgram.Level4Topics),
                ProgramStrengths = GenerateProgramStrengths(gradProgram),
                AcademicDomain = NormalizeDomainName(gradProgram.AcademicDomain)
            };
        }

        private BubbleChartProgram ConvertToBubbleChartProgram(UndergraduateProgram undergradProgram)
        {
            return new BubbleChartProgram
            {
                Id = undergradProgram.Id,
                ProgramName = undergradProgram.ProgramName,
                DegreeLevel = "Undergraduate",
                DegreeType = undergradProgram.DegreeType,
                TotalAiMlCourses = undergradProgram.TotalCourses,
                RequiredCourses = undergradProgram.Required,
                ElectiveCourses = undergradProgram.Elective,
                CapstoneCourses = 0,
                ComplexityScore = undergradProgram.ComplexityScore,
                AdvancedTopicsCount = undergradProgram.Level4Topics,
                AdvancedTopicsCoverage = GenerateAdvancedTopicsFromDomain(undergradProgram.AcademicDomain, undergradProgram.Level4Topics),
                ProgramStrengths = GenerateUndergraduateProgramStrengths(undergradProgram),
                AcademicDomain = NormalizeDomainName(undergradProgram.AcademicDomain)
            };
        }

        private BubbleChartProgram ConvertToBubbleChartProgram(MinorProgram minorProgram)
        {
            return new BubbleChartProgram
            {
                Id = minorProgram.Id,
                ProgramName = minorProgram.ProgramName,
                DegreeLevel = "Undergraduate",
                DegreeType = "Minor",
                TotalAiMlCourses = minorProgram.TotalCourses,
                RequiredCourses = minorProgram.Required,
                ElectiveCourses = minorProgram.Elective,
                CapstoneCourses = 0,
                ComplexityScore = minorProgram.ComplexityScore,
                AdvancedTopicsCount = minorProgram.Level4Topics,
                AdvancedTopicsCoverage = GenerateAdvancedTopicsFromDomain(minorProgram.AcademicDomain, minorProgram.Level4Topics),
                ProgramStrengths = GenerateMinorProgramStrengths(minorProgram),
                AcademicDomain = NormalizeDomainName(minorProgram.AcademicDomain)
            };
        }

        private List<BubbleChartProgram> ApplyBubbleChartFilter(List<BubbleChartProgram> programs, BubbleChartFilter filter)
        {
            if (filter == null) return programs;

            return programs.Where(p =>
                (filter.DegreeLevel == "all" ||
                 filter.DegreeLevel.Equals(p.DegreeLevel, StringComparison.OrdinalIgnoreCase) ||
                 (filter.DegreeLevel == "PhD" && p.IsPhd) ||
                 (filter.DegreeLevel == "Masters" && p.IsGraduate && !p.IsPhd && !p.IsCertificate) ||
                 (filter.DegreeLevel == "Certificate" && p.IsCertificate) ||
                 (filter.DegreeLevel == "Minor" && p.IsMinor)) &&

                (filter.DegreeType == "all" ||
                 filter.DegreeType.Equals(p.DegreeType, StringComparison.OrdinalIgnoreCase)) &&

                (filter.AcademicDomain == "all" ||
                 filter.AcademicDomain.Equals(p.AcademicDomain, StringComparison.OrdinalIgnoreCase)) &&

                p.TotalAiMlCourses >= filter.MinCourses &&
                p.TotalAiMlCourses <= filter.MaxCourses &&
                p.ComplexityScore >= filter.MinComplexity &&
                p.ComplexityScore <= filter.MaxComplexity
            ).ToList();
        }

        private string NormalizeDomainName(string domain)
        {
            if (string.IsNullOrEmpty(domain)) return "other";

            var normalized = domain.ToLower().Replace(" ", "").Replace("&", "");

            // Direct domain match
            var match = normalized switch
            {
                "computerscience" or "cs" => "computerscience",
                "engineering" => "engineering",
                "business" => "business",
                "math" or "mathematics" or "maths" or "mathematicsstats" => "maths",
                "healthsciences" => "healthsciences",
                "professionalstudies" => "professionalstudies",
                "law" => "law",
                "socialscience" => "socialscience",
                "socialscienceshumanities" => "socialscience",
                "artsmedia" or "arts" => "artsmedia",
                "education" => "education",
                _ => null
            };

            if (match != null) return match;

            // Keyword-based inference
            var lowerDomain = domain.ToLower();

            if (lowerDomain.Contains("math") || lowerDomain.Contains("statistics") || lowerDomain.Contains("quantitative"))
                return "maths";

            if (lowerDomain.Contains("sociology") || lowerDomain.Contains("psychology") ||
                lowerDomain.Contains("criminology") || lowerDomain.Contains("economics") ||
                lowerDomain.Contains("philosophy") || lowerDomain.Contains("communication") ||
                lowerDomain.Contains("media") || lowerDomain.Contains("ethics") ||
                lowerDomain.Contains("international affairs") || lowerDomain.Contains("digital humanities") ||
                lowerDomain.Contains("environmental") || lowerDomain.Contains("sustainability") ||
                lowerDomain.Contains("political science") || lowerDomain.Contains("cultural") ||
                lowerDomain.Contains("humanities") || lowerDomain.Contains("marine biology") ||
                lowerDomain.Contains("anthropology"))
                return "socialscience";


            return "other";
        }

        private string GenerateAdvancedTopicsFromDomain(string domain, int count)
        {
            var topicsByDomain = new Dictionary<string, List<string>>
            {
                ["computerscience"] = new List<string>
                {
                    "Deep Learning", "Computer Vision", "Natural Language Processing", "Robotics",
                    "Neural Networks", "Machine Learning Algorithms", "AI Ethics", "Reinforcement Learning"
                },
                ["engineering"] = new List<string>
                {
                    "Control Systems", "Automation", "Signal Processing", "Pattern Recognition",
                    "Embedded AI", "IoT Integration", "System Optimization"
                },
                ["business"] = new List<string>
                {
                    "Business Analytics", "Predictive Modeling", "Decision Support Systems",
                    "AI Strategy", "Digital Transformation", "Process Automation"
                },
                ["maths"] = new List<string>
                {
                    "Applied Mathematics", "Statistics", "Mathematical Modeling", "Scientific Computing",
                    "Probability Theory", "Statistical Inference", "Bayesian Statistics", "Linear Algebra",
                    "Numerical Analysis", "Data Mining", "Statistical Learning", "Applied Statistics",
                    "Quantitative Methods", "Computational Statistics", "Stochastic Processes",
                    "Time Series Analysis", "Mathematical Optimization", "Operations Research",
                    "Mathematics for Social Sciences"
                },
                ["healthsciences"] = new List<string>
                {
                    "Medical Imaging", "Clinical Decision Support", "Health Informatics", "Biomedical AI",
                    "Diagnostic Systems", "Drug Discovery"
                },
                ["law"] = new List<string>
                {
                    "Law, LLM", "Law, LLM—Experiential", "Privacy and Technology Law", "Legal Design", "Human Rights Law"
                },
                ["socialscience"] = new List<string>
                {
                    "Behavioral Analytics", "Media Ethics", "Public Policy Modeling", "Computational Social Science",
                    "Sociology of AI", "Cognitive Science", "Cultural Analysis", "Environmental Psychology",
                    "Social Impact Analytics", "Philosophy of Technology", "Digital Sociology", "Political Data Science"
                },
                ["other"] = new List<string>
                {
                    "AI Applications", "Data Analysis", "Machine Learning", "Statistical Methods",
                    "Programming", "Algorithm Design"
                }
            };

            var normalizedDomain = NormalizeDomainName(domain);
            var availableTopics = topicsByDomain.GetValueOrDefault(normalizedDomain, topicsByDomain["other"]);

            var selectedTopics = availableTopics.Take(Math.Min(count, availableTopics.Count)).ToList();
            return string.Join(", ", selectedTopics);
        }


        private string GenerateProgramStrengths(GraduateProgram program)
        {
            var strengths = new List<string>();

            if (program.IsPhd)
                strengths.Add("Research Focus");
            if (program.TotalCourses > 15)
                strengths.Add("Comprehensive Curriculum");
            if (program.Level4Topics > 5)
                strengths.Add("Advanced Specialization");
            if (program.Capstone > 0)
                strengths.Add("Capstone Experience");

            strengths.Add("Industry Relevance");
            return string.Join(", ", strengths);
        }

        private string GenerateUndergraduateProgramStrengths(UndergraduateProgram program)
        {
            var strengths = new List<string>();

            if (program.StemDesignation)
                strengths.Add("STEM Designation");
            if (program.CoopAvailable)
                strengths.Add("Co-op Opportunities");
            if (program.InternshipRequired)
                strengths.Add("Internship Experience");
            if (program.TotalCourses > 10)
                strengths.Add("Comprehensive Foundation");

            strengths.Add("Career Preparation");
            return string.Join(", ", strengths);
        }

        private string GenerateMinorProgramStrengths(MinorProgram program)
        {
            var strengths = new List<string>();

            if (program.OnlineAvailable)
                strengths.Add("Online Available");
            if (program.CanCompleteInSummer)
                strengths.Add("Summer Completion");
            if (program.TotalCourses <= 6)
                strengths.Add("Quick Completion");

            strengths.Add("Complementary Skills");
            strengths.Add("Career Enhancement");
            return string.Join(", ", strengths);
        }

        // EXISTING METHODS FOR OTHER VISUALIZATIONS (unchanged)

        [AllowAnonymous]
        public async Task<IActionResult> UndergraduateProgramsVisualization()
        {
            try
            {
                _logger.LogInformation("Starting Undergraduate Programs Visualization data loading...");

                var bachelorPrograms = await GetUndergraduateProgramsFromGoogleSheetsAsync();
                var minorPrograms = await GetMinorProgramsFromGoogleSheetsAsync();

                _logger.LogInformation($"Raw data loaded: {bachelorPrograms?.Count ?? 0} bachelor programs, {minorPrograms?.Count ?? 0} minor programs");

                if ((bachelorPrograms == null || bachelorPrograms.Count == 0) && (minorPrograms == null || minorPrograms.Count == 0))
                {
                    _logger.LogError("No undergraduate or minor programs loaded from Google Sheets");
                    ViewBag.ErrorMessage = "Failed to load undergraduate data from Google Sheets. Please check the connection and try again.";
                    ViewBag.DataSource = "Google Sheets (Failed)";

                    var emptyViewModel = new UndergraduateVisualizationViewModel
                    {
                        BachelorPrograms = new List<UndergraduateProgram>(),
                        MinorPrograms = new List<MinorProgram>(),
                        TotalProgramsCount = 0
                    };
                    emptyViewModel.GroupProgramsByDomain();
                    emptyViewModel.BinProgramsByComplexity();

                    return View(emptyViewModel);
                }

                var validBachelorPrograms = bachelorPrograms?.Where(p => p.TotalCourses >= 5).ToList() ?? new List<UndergraduateProgram>();
                var validMinorPrograms = minorPrograms ?? new List<MinorProgram>();

                _logger.LogInformation($"Valid programs: {validBachelorPrograms.Count} bachelor programs (5+ courses), {validMinorPrograms.Count} minor programs");

                foreach (var program in validBachelorPrograms)
                {
                    if (program.ComplexityScore <= 1.0m)
                    {
                        program.CalculateComplexityScore();
                        _logger.LogInformation($"Calculated complexity for Bachelor {program.ProgramName}: {program.ComplexityScore}");
                    }
                }

                foreach (var program in validMinorPrograms)
                {
                    if (program.ComplexityScore <= 1.0m)
                    {
                        program.CalculateComplexityScore();
                        _logger.LogInformation($"Calculated complexity for Minor {program.ProgramName}: {program.ComplexityScore}");
                    }
                }

                var viewModel = new UndergraduateVisualizationViewModel
                {
                    BachelorPrograms = validBachelorPrograms,
                    MinorPrograms = validMinorPrograms,
                    TotalProgramsCount = validBachelorPrograms.Count + validMinorPrograms.Count
                };

                viewModel.BinProgramsByComplexity();
                viewModel.GroupProgramsByDomain();

                ViewBag.DataSource = "Google Sheets";
                ViewBag.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ViewBag.Message = $"Undergraduate Programs Visualization - {viewModel.TotalProgramsCount} Programs Loaded";
                ViewBag.CurrentTime = DateTime.Now.ToString();

                _logger.LogInformation($"Successfully created ViewModel with {viewModel.TotalProgramsCount} total programs ({viewModel.BachelorProgramsCount} bachelor + {viewModel.MinorProgramsCount} minor)");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading undergraduate programs visualization: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                ViewBag.ErrorMessage = $"Unable to load undergraduate programs data from Google Sheets: {ex.Message}";
                ViewBag.DataSource = "Google Sheets (Error)";

                var errorViewModel = new UndergraduateVisualizationViewModel
                {
                    BachelorPrograms = new List<UndergraduateProgram>(),
                    MinorPrograms = new List<MinorProgram>(),
                    TotalProgramsCount = 0
                };
                errorViewModel.GroupProgramsByDomain();
                errorViewModel.BinProgramsByComplexity();

                return View(errorViewModel);
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> GraduateProgramsVisualization()
        {
            try
            {
                _logger.LogInformation("Starting Graduate Programs Visualization data loading...");

                var allPrograms = await GetGraduateProgramsFromGoogleSheetsAsync();

                if (allPrograms == null || allPrograms.Count == 0)
                {
                    _logger.LogError("No programs loaded from Google Sheets");
                    ViewBag.ErrorMessage = "Failed to load data from Google Sheets. Please check the connection and try again.";
                    ViewBag.DataSource = "Google Sheets (Failed)";

                    var emptyViewModel = new VisualizationViewModel
                    {
                        MastersPrograms = new List<GraduateProgram>(),
                        CertificatePrograms = new List<GraduateProgram>(),
                        PhdPrograms = new List<GraduateProgram>(),
                        TotalProgramsCount = 0,
                        ExpectedMastersPrograms = 40,
                        ExpectedCertificatePrograms = 3,
                        ExpectedPhdPrograms = 5,
                        ExpectedTotalPrograms = 48
                    };
                    emptyViewModel.GroupProgramsByComplexity();

                    return View(emptyViewModel);
                }

                var validPrograms = allPrograms.Where(p => p.TotalCourses >= 5).ToList();

                var mastersPrograms = validPrograms.Where(p => !p.IsCertificate && !IsPhdProgram(p)).ToList();
                var certificatePrograms = validPrograms.Where(p => p.IsCertificate).ToList();
                var phdPrograms = validPrograms.Where(p => IsPhdProgram(p)).ToList();

                _logger.LogInformation($"Program breakdown: {mastersPrograms.Count} Masters, {certificatePrograms.Count} Certificates, {phdPrograms.Count} PhD");

                var viewModel = new VisualizationViewModel
                {
                    MastersPrograms = mastersPrograms,
                    CertificatePrograms = certificatePrograms,
                    PhdPrograms = phdPrograms,
                    TotalProgramsCount = validPrograms.Count,
                    ExpectedMastersPrograms = 40,
                    ExpectedCertificatePrograms = 3,
                    ExpectedPhdPrograms = 5,
                    ExpectedTotalPrograms = 48
                };

                viewModel.GroupProgramsByComplexity();

                ViewBag.DataSource = "Google Sheets";
                ViewBag.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ViewBag.Message = $"Graduate Programs Visualization - {validPrograms.Count} Programs Loaded";
                ViewBag.CurrentTime = DateTime.Now.ToString();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading graduate programs visualization: {ex.Message}");
                ViewBag.ErrorMessage = $"Unable to load graduate programs data from Google Sheets: {ex.Message}";
                ViewBag.DataSource = "Google Sheets (Error)";

                var errorViewModel = new VisualizationViewModel();
                return View(errorViewModel);
            }
        }

        // Keep all existing private methods for other data sources...
        private async Task<List<UndergraduateProgram>> GetUndergraduateProgramsFromGoogleSheetsAsync()
        {
            try
            {
                const string UNDERGRADUATE_SHEETS_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQA00pBsgS8EvPEZFA2j6S_QraXI4G4cg_77m-uKgQ7_990xsfhKvibli0FhtHI5ysIYiJ7_tFpEuT7/pub?gid=1739767611&single=true&output=csv";

                _logger.LogInformation($"Fetching undergraduate data from: {UNDERGRADUATE_SHEETS_CSV_URL}");

                var csv = await _httpClient.GetStringAsync(UNDERGRADUATE_SHEETS_CSV_URL);

                _logger.LogInformation($"CSV data received: {csv.Length} characters");

                return ParseUndergraduateCSVContent(csv);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching undergraduate data from Google Sheets: {ex.Message}");
                return new List<UndergraduateProgram>();
            }
        }

        private async Task<List<MinorProgram>> GetMinorProgramsFromGoogleSheetsAsync()
        {
            try
            {
                const string UNDERGRADUATE_SHEETS_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQA00pBsgS8EvPEZFA2j6S_QraXI4G4cg_77m-uKgQ7_990xsfhKvibli0FhtHI5ysIYiJ7_tFpEuT7/pub?gid=1739767611&single=true&output=csv";

                _logger.LogInformation($"Fetching minor data from same sheet: {UNDERGRADUATE_SHEETS_CSV_URL}");

                var csv = await _httpClient.GetStringAsync(UNDERGRADUATE_SHEETS_CSV_URL);

                return ParseMinorProgramsFromUndergraduateCSV(csv);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching minor data from Google Sheets: {ex.Message}");
                return new List<MinorProgram>();
            }
        }

        private async Task<List<GraduateProgram>> GetGraduateProgramsFromGoogleSheetsAsync()
        {
            try
            {
                const string GOOGLE_SHEETS_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQA00pBsgS8EvPEZFA2j6S_QraXI4G4cg_77m-uKgQ7_990xsfhKvibli0FhtHI5ysIYiJ7_tFpEuT7/pub?gid=0&single=true&output=csv";

                var csv = await _httpClient.GetStringAsync(GOOGLE_SHEETS_CSV_URL);
                return ParseCSVContent(csv);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching data from Google Sheets: {ex.Message}");
                throw;
            }
        }

        // Keep all existing parsing methods for other data sources...
        private List<UndergraduateProgram> ParseUndergraduateCSVContent(string csvContent)
        {
            var programs = new List<UndergraduateProgram>();
            var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < lines.Length; i++)
            {
                var program = ParseUndergraduateCSVLine(lines[i], i);
                if (program != null)
                {
                    programs.Add(program);
                }
            }

            return programs;
        }

        private List<MinorProgram> ParseMinorProgramsFromUndergraduateCSV(string csvContent)
        {
            var programs = new List<MinorProgram>();
            var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < lines.Length; i++)
            {
                var program = ParseMinorFromUndergraduateCSVLine(lines[i], i);
                if (program != null)
                {
                    programs.Add(program);
                }
            }

            return programs;
        }

        private List<GraduateProgram> ParseCSVContent(string csvContent)
        {
            var programs = new List<GraduateProgram>();
            var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < lines.Length; i++)
            {
                var program = ParseCSVLine(lines[i], i);
                if (program != null)
                {
                    programs.Add(program);
                }
            }

            return programs;
        }

        // Continue with all existing parsing methods...
        private UndergraduateProgram ParseUndergraduateCSVLine(string line, int rowIndex)
        {
            try
            {
                var values = ParseCSVValues(line);

                if (values.Count < 4) return null;

                string programName = values[0]?.Trim() ?? "";
                if (string.IsNullOrEmpty(programName)) return null;

                bool isMinor = false;
                for (int col = 0; col < values.Count; col++)
                {
                    string cellValue = values[col]?.Trim().ToUpper() ?? "";
                    if (cellValue == "TRUE" && col > 5)
                    {
                        isMinor = true;
                        break;
                    }
                }

                if (isMinor) return null;

                string degreeType = values.Count > 2 ? values[2]?.Trim() ?? "" : "";

                int totalCourses = 0;
                for (int col = 3; col < Math.Min(values.Count, 7); col++)
                {
                    if (int.TryParse(values[col]?.Trim(), out totalCourses) && totalCourses > 0 && totalCourses < 200)
                    {
                        break;
                    }
                }

                if (totalCourses == 0) return null;

                int.TryParse(values.Count > 4 ? values[4]?.Trim() : "0", out int required);
                int.TryParse(values.Count > 5 ? values[5]?.Trim() : "0", out int elective);

                string boxSizeCategory = values[12]?.Trim() ?? "";

                // Parse BoxHeight from column 13
                int boxHeight = 60; // default
                if (int.TryParse(values[13]?.Trim(), out int csvHeight))
                {
                    boxHeight = csvHeight;
                }

                // Parse BoxWidth from column 14  
                int boxWidth = 180; // default
                if (int.TryParse(values[14]?.Trim(), out int csvWidth))
                {
                    boxWidth = csvWidth;
                }

                int level4Topics = 0;
                if (values.Count > 8)
                {
                    int.TryParse(values[8]?.Trim(), out level4Topics);
                }

                decimal complexity = 0;
                if (values.Count > 7)
                {
                    decimal.TryParse(values[7]?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out complexity);
                }

                var program = new UndergraduateProgram
                {
                    Id = rowIndex,
                    ProgramName = programName,
                    DegreeType = degreeType,
                    TotalCourses = totalCourses,
                    Required = required,
                    Elective = elective,
                    Level4Topics = level4Topics,
                    ComplexityScore = complexity > 0 ? complexity : 2.0m,
                    AcademicDomain = DetermineDomainFromName(programName),
                    StemDesignation = IsStemProgram(programName, degreeType),
                    CoopAvailable = IsCoopProgram(programName, degreeType),
                    InternshipRequired = IsInternshipProgram(programName, degreeType),
                    BoxColor = "#CC3333",
                    BoxHeight = boxHeight,        // NOW USES CSV VALUE
                    BoxWidth = boxWidth,
                    CreditHours = totalCourses * 3
                };

                return program;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing undergraduate CSV line {rowIndex}: {ex.Message}");
                return null;
            }
        }

        private MinorProgram ParseMinorFromUndergraduateCSVLine(string line, int rowIndex)
        {
            try
            {
                var values = ParseCSVValues(line);

                if (values.Count < 4) return null;

                string programName = values[0]?.Trim() ?? "";
                if (string.IsNullOrEmpty(programName)) return null;

                bool isMinor = false;
                for (int col = 0; col < values.Count; col++)
                {
                    string cellValue = values[col]?.Trim().ToUpper() ?? "";
                    if (cellValue == "TRUE" && col > 5)
                    {
                        isMinor = true;
                        break;
                    }
                }

                if (!isMinor) return null;

                string degreeType = values.Count > 2 ? values[2]?.Trim() ?? "Minor" : "Minor";

                int totalCourses = 0;
                for (int col = 3; col < Math.Min(values.Count, 7); col++)
                {
                    if (int.TryParse(values[col]?.Trim(), out totalCourses) && totalCourses > 0 && totalCourses < 50)
                    {
                        break;
                    }
                }

                if (totalCourses == 0) return null;

                int.TryParse(values.Count > 4 ? values[4]?.Trim() : "0", out int required);
                int.TryParse(values.Count > 5 ? values[5]?.Trim() : "0", out int elective);

                string boxSizeCategory = values[12]?.Trim() ?? "";

                // Parse BoxHeight from column 13
                int boxHeight = 60; // default
                if (int.TryParse(values[13]?.Trim(), out int csvHeight))
                {
                    boxHeight = csvHeight;
                }

                // Parse BoxWidth from column 14  
                int boxWidth = 180; // default
                if (int.TryParse(values[14]?.Trim(), out int csvWidth))
                {
                    boxWidth = csvWidth;
                }

                int level4Topics = 0;
                if (values.Count > 8)
                {
                    int.TryParse(values[8]?.Trim(), out level4Topics);
                }

                decimal complexity = 0;
                if (values.Count > 7)
                {
                    decimal.TryParse(values[7]?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out complexity);
                }

                var program = new MinorProgram
                {
                    Id = rowIndex,
                    ProgramName = programName,
                    DegreeType = degreeType,
                    TotalCourses = totalCourses,
                    Required = required,
                    Elective = elective,
                    Level4Topics = level4Topics,
                    ComplexityScore = complexity > 0 ? complexity : 1.5m,
                    AcademicDomain = DetermineDomainFromName(programName),
                    OnlineAvailable = programName.ToLower().Contains("online"),
                    CanCompleteInSummer = totalCourses <= 8,
                    BoxColor = "#FF6666",
                    BoxHeight = boxHeight,        // NOW USES CSV VALUE
                    BoxWidth = boxWidth      // NOW USES CSV VALUE  
                    // CreditHours = totalCourses * 3
                };

                return program;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing minor CSV line {rowIndex}: {ex.Message}");
                return null;
            }
        }

        // SIMPLE FIX: Just update your existing ParseCSVLine method in HomeController.cs

        private GraduateProgram ParseCSVLine(string line, int rowIndex)
        {
            try
            {
                var values = ParseCSVValues(line);

                if (values.Count < 16) return null;

                string programName = values[0]?.Trim() ?? "";
                string degreeType = values[2]?.Trim() ?? "";

                if (string.IsNullOrEmpty(programName)) return null;

                if (!int.TryParse(values[3]?.Trim(), out int totalCourses)) return null;

                int.TryParse(values[4]?.Trim(), out int required);
                int.TryParse(values[5]?.Trim(), out int elective);
                int.TryParse(values[6]?.Trim(), out int capstone);
                decimal.TryParse(values[7]?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal complexity);
                int.TryParse(values[8]?.Trim(), out int level4);

                bool isPhd = IsPhdProgram(new GraduateProgram { DegreeType = degreeType, ProgramName = programName });
                bool isCertificate = !isPhd && (values[10]?.Trim().ToUpper() == "TRUE" || degreeType.ToLower().Contains("certificate"));

                // SIMPLE FIX: Read the box size values directly from CSV columns 12, 13, 14, 15
                string boxSizeCategory = values[12]?.Trim() ?? "";

                // Parse BoxHeight from column 13
                int boxHeight = 60; // default
                if (int.TryParse(values[13]?.Trim(), out int csvHeight))
                {
                    boxHeight = csvHeight;
                }

                // Parse BoxWidth from column 14  
                int boxWidth = 180; // default
                if (int.TryParse(values[14]?.Trim(), out int csvWidth))
                {
                    boxWidth = csvWidth;
                }

                // Parse BoxColor from column 15
                string boxColor = values[15]?.Trim() ?? "";
                if (string.IsNullOrEmpty(boxColor))
                {
                    boxColor = isPhd ? "#6A0DAD" : isCertificate ? "#C00000" : "#8B0000";
                }

                var program = new GraduateProgram
                {
                    Id = rowIndex,
                    ProgramName = programName,
                    DegreeLevel = values[1]?.Trim() ?? "",
                    DegreeType = degreeType,
                    TotalCourses = totalCourses,
                    Required = required,
                    Elective = elective,
                    Capstone = capstone,
                    ComplexityScore = complexity,
                    Level4Topics = level4,
                    AcademicDomain = values[9]?.Trim() ?? "",
                    IsCertificate = isCertificate,
                    IsPhd = isPhd,
                    BinKey = values[11]?.Trim() ?? "",
                    BoxSizeCategory = boxSizeCategory,
                    BoxHeight = boxHeight,        // NOW USES CSV VALUE
                    BoxWidth = boxWidth,          // NOW USES CSV VALUE  
                    BoxColor = boxColor           // NOW USES CSV VALUE
                };

                // Debug log to verify values are being read
                _logger.LogInformation($"Program: {programName} | Height: {boxHeight} | Width: {boxWidth} | Color: {boxColor}");

                return program;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing CSV line {rowIndex}: {ex.Message}");
                return null;
            }
        }

        private List<string> ParseCSVValues(string csvLine)
        {
            var values = new List<string>();
            var inQuotes = false;
            var currentValue = new StringBuilder();

            for (int i = 0; i < csvLine.Length; i++)
            {
                char c = csvLine[i];

                if (c == '"' && (i == 0 || csvLine[i - 1] == ','))
                {
                    inQuotes = true;
                }
                else if (c == '"' && inQuotes && (i == csvLine.Length - 1 || csvLine[i + 1] == ','))
                {
                    inQuotes = false;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString());
            return values;
        }

        private bool IsPhdProgram(GraduateProgram program)
        {
            if (program == null) return false;

            string degreeType = program.DegreeType?.ToLower() ?? "";
            string programName = program.ProgramName?.ToLower() ?? "";

            return degreeType.Contains("phd") ||
                   degreeType.Contains("dmsc") ||
                   degreeType.Contains("doctor") ||
                   programName.Contains("phd") ||
                   programName.Contains("dmsc") ||
                   programName.Contains("doctor");
        }

        private bool IsStemProgram(string programName, string degreeType)
        {
            var stemKeywords = new[] { "engineering", "computer", "science", "mathematics", "technology",
                                     "physics", "chemistry", "biology", "data", "cyber", "information" };

            var combined = $"{programName} {degreeType}".ToLower();
            return stemKeywords.Any(keyword => combined.Contains(keyword));
        }

        private bool IsCoopProgram(string programName, string degreeType)
        {
            var coopKeywords = new[] { "engineering", "business", "computer", "technology", "cyber" };
            var combined = $"{programName} {degreeType}".ToLower();
            return coopKeywords.Any(keyword => combined.Contains(keyword));
        }

        private bool IsInternshipProgram(string programName, string degreeType)
        {
            var internshipKeywords = new[] { "health", "nursing", "education", "social work", "psychology" };
            var combined = $"{programName} {degreeType}".ToLower();
            return internshipKeywords.Any(keyword => combined.Contains(keyword));
        }

        private string DetermineDomainFromName(string programName)
        {
            if (string.IsNullOrEmpty(programName)) return "Other";

            var nameLower = programName.ToLower();
            if (nameLower.Contains("computer") || nameLower.Contains("software") || nameLower.Contains("data"))
                return "Computer Science";
            if (nameLower.Contains("engineering") || nameLower.Contains("technical"))
                return "Engineering";
            if (nameLower.Contains("business") || nameLower.Contains("management"))
                return "Business";
            if (nameLower.Contains("health") || nameLower.Contains("medical"))
                return "Health Sciences";
            return "Other";
        }

        // FIXED GetAllProgramsData METHOD
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllProgramsData()
        {
            try
            {
                var graduatePrograms = await GetGraduateProgramsFromGoogleSheetsAsync();
                var undergraduatePrograms = await GetUndergraduateProgramsFromGoogleSheetsAsync();
                var minorPrograms = await GetMinorProgramsFromGoogleSheetsAsync();

                var validGradPrograms = graduatePrograms.Where(p => p.TotalCourses >= 5).ToList();
                var validUndergradPrograms = undergraduatePrograms.Where(p => p.TotalCourses >= 5).ToList();

                // FETCH BUBBLE CHART DATA - This was missing!
                var bubbleChartPrograms = await GetBubbleChartProgramsFromGoogleSheetsAsync();

                // Get raw CSV for debugging info
                const string BUBBLE_CHART_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQA00pBsgS8EvPEZFA2j6S_QraXI4G4cg_77m-uKgQ7_990xsfhKvibli0FhtHI5ysIYiJ7_tFpEuT7/pub?gid=1065445862&single=true&output=csv";
                string bubbleChartCsv = "";

                try
                {
                    bubbleChartCsv = await _httpClient.GetStringAsync(BUBBLE_CHART_CSV_URL);
                }
                catch (Exception bubbleEx)
                {
                    _logger.LogWarning($"Could not fetch bubble chart CSV data: {bubbleEx.Message}");
                    bubbleChartCsv = "Error fetching CSV data";
                }

                var result = new
                {
                    success = true,
                    graduate = new
                    {
                        totalPrograms = validGradPrograms.Count,
                        mastersPrograms = validGradPrograms.Count(p => !p.IsCertificate && !p.IsPhd),
                        certificatePrograms = validGradPrograms.Count(p => p.IsCertificate),
                        phdPrograms = validGradPrograms.Count(p => p.IsPhd),
                        programs = validGradPrograms
                    },
                    BubbleChart = new
                    {
                        CsvLength = bubbleChartCsv.Length,
                        CsvLines = bubbleChartCsv.Split('\n').Length,
                        TotalProgramsParsed = bubbleChartPrograms.Count,
                        SamplePrograms = bubbleChartPrograms.Take(5).Select(p => new {
                            p.ProgramName,
                            p.DegreeLevel,
                            p.DegreeType,
                            p.TotalAiMlCourses,
                            p.ComplexityScore,
                            p.AcademicDomain
                        }).ToList(),
                        FirstFewLines = bubbleChartCsv.Split('\n').Take(3).ToList()
                    },
                    undergraduate = new
                    {
                        totalPrograms = validUndergradPrograms.Count,
                        stemPrograms = validUndergradPrograms.Count(p => p.StemDesignation),
                        coopPrograms = validUndergradPrograms.Count(p => p.CoopAvailable),
                        programs = validUndergradPrograms
                    },
                    minor = new
                    {
                        totalPrograms = minorPrograms.Count,
                        onlinePrograms = minorPrograms.Count(p => p.OnlineAvailable),
                        summerPrograms = minorPrograms.Count(p => p.CanCompleteInSummer),
                        programs = minorPrograms
                    },
                    summary = new
                    {
                        totalAllPrograms = validGradPrograms.Count + validUndergradPrograms.Count + minorPrograms.Count,
                        graduateTotal = validGradPrograms.Count,
                        undergraduateTotal = validUndergradPrograms.Count,
                        minorTotal = minorPrograms.Count,
                        bubbleChartTotal = bubbleChartPrograms.Count
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching all programs data: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Unable to fetch programs data",
                    message = ex.Message
                });
            }
        }

        // Keep all your existing API methods...
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetUndergraduateProgramsData()
        {
            try
            {
                var bachelorPrograms = await GetUndergraduateProgramsFromGoogleSheetsAsync();
                var minorPrograms = await GetMinorProgramsFromGoogleSheetsAsync();
                var validBachelorPrograms = bachelorPrograms.Where(p => p.TotalCourses >= 5).ToList();

                var result = new
                {
                    success = validBachelorPrograms.Count > 0 || minorPrograms.Count > 0,
                    totalPrograms = validBachelorPrograms.Count + minorPrograms.Count,
                    bachelorPrograms = validBachelorPrograms.Count,
                    minorPrograms = minorPrograms.Count,
                    stemPrograms = validBachelorPrograms.Count(p => p.StemDesignation),
                    coopPrograms = validBachelorPrograms.Count(p => p.CoopAvailable),
                    onlineMinors = minorPrograms.Count(p => p.OnlineAvailable),
                    programs = new
                    {
                        bachelor = validBachelorPrograms.Select(p => new {
                            p.Id,
                            p.ProgramName,
                            p.DegreeType,
                            p.TotalCourses,
                            p.ComplexityScore,
                            p.StemDesignation,
                            p.CoopAvailable,
                            p.InternshipRequired,
                            p.AcademicDomain,
                            p.BoxColor
                        }).ToList(),
                        minor = minorPrograms.Select(p => new {
                            p.Id,
                            p.ProgramName,
                            p.DegreeType,
                            p.TotalCourses,
                            p.ComplexityScore,
                            p.OnlineAvailable,
                            p.CanCompleteInSummer,
                            p.AcademicDomain,
                            p.BoxColor
                        }).ToList()
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching undergraduate programs data: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetGraduateProgramsData()
        {
            try
            {
                var programs = await GetGraduateProgramsFromGoogleSheetsAsync();
                var validPrograms = programs.Where(p => p.TotalCourses >= 5).ToList();

                var result = new
                {
                    success = validPrograms.Count > 0,
                    totalPrograms = validPrograms.Count,
                    mastersPrograms = validPrograms.Count(p => !p.IsCertificate && !p.IsPhd),
                    certificatePrograms = validPrograms.Count(p => p.IsCertificate),
                    phdPrograms = validPrograms.Count(p => p.IsPhd),
                    programs = validPrograms.Select(p => new {
                        p.Id,
                        p.ProgramName,
                        p.DegreeType,
                        p.TotalCourses,
                        p.IsCertificate,
                        p.IsPhd,
                        p.BinKey,
                        p.BoxColor,
                        p.ComplexityScore
                    }).ToList()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching graduate programs data: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> DebugGoogleSheets()
        {
            try
            {
                const string GOOGLE_SHEETS_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQA00pBsgS8EvPEZFA2j6S_QraXI4G4cg_77m-uKgQ7_990xsfhKvibli0FhtHI5ysIYiJ7_tFpEuT7/pub?gid=0&single=true&output=csv";
                const string UNDERGRADUATE_SHEETS_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQA00pBsgS8EvPEZFA2j6S_QraXI4G4cg_77m-uKgQ7_990xsfhKvibli0FhtHI5ysIYiJ7_tFpEuT7/pub?gid=1739767611&single=true&output=csv";
                const string BUBBLE_CHART_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQA00pBsgS8EvPEZFA2j6S_QraXI4G4cg_77m-uKgQ7_990xsfhKvibli0FhtHI5ysIYiJ7_tFpEuT7/pub?gid=1065445862&single=true&output=csv";

                var csv = await _httpClient.GetStringAsync(GOOGLE_SHEETS_CSV_URL);
                var undergradCsv = await _httpClient.GetStringAsync(UNDERGRADUATE_SHEETS_CSV_URL);

                // Fetch bubble chart CSV data
                string bubbleChartCsv = "";
                List<BubbleChartProgram> bubbleChartPrograms = new List<BubbleChartProgram>();

                try
                {
                    bubbleChartCsv = await _httpClient.GetStringAsync(BUBBLE_CHART_CSV_URL);
                    bubbleChartPrograms = ParseBubbleChartCSVContent(bubbleChartCsv);
                }
                catch (Exception bubbleEx)
                {
                    _logger.LogWarning($"Could not fetch bubble chart data: {bubbleEx.Message}");
                    bubbleChartCsv = "Error fetching data";
                }

                var allPrograms = ParseCSVContent(csv);
                var validPrograms = allPrograms.Where(p => p.TotalCourses >= 5).ToList();
                var certificates = validPrograms.Where(p => p.IsCertificate).ToList();
                var masters = validPrograms.Where(p => !p.IsCertificate && !p.IsPhd).ToList();
                var phds = validPrograms.Where(p => p.IsPhd).ToList();

                var undergraduatePrograms = ParseUndergraduateCSVContent(undergradCsv);
                var minorPrograms = await GetMinorProgramsFromGoogleSheetsAsync();
                var validUndergraduatePrograms = undergraduatePrograms.Where(p => p.TotalCourses >= 5).ToList();

                var debug = new
                {
                    Graduate = new
                    {
                        CsvLength = csv.Length,
                        CsvLines = csv.Split('\n').Length,
                        TotalProgramsParsed = allPrograms.Count,
                        ProgramsWith5PlusCourses = validPrograms.Count,
                        MastersPrograms = masters.Count,
                        CertificatePrograms = certificates.Count,
                        PhdPrograms = phds.Count,
                        ExpectedTotals = new
                        {
                            Total = 48,
                            Masters = 40,
                            Certificates = 3,
                            Phds = 5
                        }
                    },
                    Undergraduate = new
                    {
                        CsvLength = undergradCsv.Length,
                        CsvLines = undergradCsv.Split('\n').Length,
                        TotalProgramsParsed = undergraduatePrograms.Count,
                        ProgramsWith5PlusCourses = validUndergraduatePrograms.Count,
                        STEMPrograms = validUndergraduatePrograms.Count(p => p.StemDesignation),
                        CoopPrograms = validUndergraduatePrograms.Count(p => p.CoopAvailable),
                        SamplePrograms = validUndergraduatePrograms.Take(5).Select(p => new {
                            p.ProgramName,
                            p.DegreeType,
                            p.TotalCourses,
                            p.ComplexityScore,
                            p.StemDesignation,
                            p.CoopAvailable
                        }).ToList(),
                        FirstFewLines = undergradCsv.Split('\n').Take(3).ToList()
                    },
                    Minor = new
                    {
                        TotalProgramsParsed = minorPrograms.Count,
                        OnlinePrograms = minorPrograms.Count(p => p.OnlineAvailable),
                        SummerPrograms = minorPrograms.Count(p => p.CanCompleteInSummer),
                        SamplePrograms = minorPrograms.Take(5).Select(p => new {
                            p.ProgramName,
                            p.DegreeType,
                            p.TotalCourses,
                            p.ComplexityScore,
                            p.OnlineAvailable,
                            p.CanCompleteInSummer
                        }).ToList()
                    },
                    BubbleChart = new
                    {
                        CsvLength = bubbleChartCsv.Length,
                        CsvLines = bubbleChartCsv.Split('\n').Length,
                        TotalProgramsParsed = bubbleChartPrograms.Count,
                        SamplePrograms = bubbleChartPrograms.Take(5).Select(p => new {
                            p.ProgramName,
                            p.DegreeLevel,
                            p.DegreeType,
                            p.TotalAiMlCourses,
                            p.ComplexityScore,
                            p.AcademicDomain,
                            p.AdvancedTopicsCoverage,
                            p.ProgramStrengths
                        }).ToList(),
                        FirstFewLines = bubbleChartCsv.Split('\n').Take(3).ToList()
                    },
                    Summary = new
                    {
                        TotalAllPrograms = validPrograms.Count + validUndergraduatePrograms.Count + minorPrograms.Count + bubbleChartPrograms.Count,
                        GraduatePrograms = validPrograms.Count,
                        UndergraduatePrograms = validUndergraduatePrograms.Count,
                        MinorPrograms = minorPrograms.Count,
                        BubbleChartPrograms = bubbleChartPrograms.Count
                    },
                    URLs = new
                    {
                        Graduate = "gid=0",
                        Undergraduate = "gid=1739767611",
                        BubbleChart = "gid=1065445862"
                    }
                };

                return Json(debug);
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }


        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> QuickCertificateTest()
        {
            try
            {
                var allPrograms = await GetGraduateProgramsFromGoogleSheetsAsync();
                var validPrograms = allPrograms.Where(p => p.TotalCourses >= 5).ToList();

                var result = new
                {
                    TotalPrograms = validPrograms.Count,
                    MastersCount = validPrograms.Count(p => !p.IsCertificate && !p.IsPhd),
                    CertificatesCount = validPrograms.Count(p => p.IsCertificate),
                    PhdCount = validPrograms.Count(p => p.IsPhd),

                    AllPrograms = validPrograms.Select(p => new {
                        p.ProgramName,
                        p.DegreeType,
                        p.TotalCourses,
                        p.IsCertificate,
                        p.IsPhd,
                        p.BinKey
                    }).ToList(),

                    CertificatesOnly = validPrograms.Where(p => p.IsCertificate).Select(p => new {
                        p.ProgramName,
                        p.DegreeType,
                        p.TotalCourses,
                        p.BinKey,
                        p.BoxColor
                    }).ToList(),

                    PhdsOnly = validPrograms.Where(p => p.IsPhd).Select(p => new {
                        p.ProgramName,
                        p.DegreeType,
                        p.TotalCourses,
                        p.BinKey,
                        p.BoxColor
                    }).ToList()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetIndustryReports(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest("Query is required.");
            }

            try
            {
                string openAiApiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(openAiApiKey))
                {
                    throw new Exception("OpenAI API key is missing from configuration.");
                }

                string openAiUrl = "https://api.openai.com/v1/chat/completions";

                var requestData = new
                {
                    model = "gpt-4",
                    messages = new[]
                    {
                        new { role = "system", content = "You are an AI assistant that provides the latest industry reports based on user queries." },
                        new { role = "user", content = $"Find the latest industry reports on {query}. Provide website links if available." }
                    },
                    max_tokens = 200
                };

                var requestJson = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiApiKey}");

                var response = await _httpClient.PostAsync(openAiUrl, content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                string reply = result?.choices?[0]?.message?.content ?? "No reports found.";

                return Ok(new { ReportSummary = reply });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching industry reports: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        // Additional helper methods for existing functionality
        public IActionResult DomainAndSubDomainWork()
        {
            return View();
        }

        public IActionResult IPDWorkspace()
        {
            return View();
        }

        public IActionResult IPDWorkspacedashboard()
        {
            return View();
        }

        public IActionResult SkillsVisualization()
        {
            return View();
        }

        public IActionResult Emailoutreach()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult CustomUnauthorized()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult FetchIndustryReports()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult DegreeLevelVisualization()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Timeout()
        {
            ViewBag.TimeoutMessage = "The Page has timed out. Please reload the page.";
            return View();
        }
    }

    // Services/GoogleSheetsService.cs - Keep your existing service
    public class GoogleSheetsService
    {
        private readonly string _sheetId;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GoogleSheetsService(IConfiguration config, HttpClient httpClient)
        {
            _sheetId = config["GoogleSheets:SheetId"];
            _apiKey = config["GoogleSheets:ApiKey"];
            _httpClient = httpClient;
        }

        public async Task<List<OutreachRecord>> GetOutreachRecords()
        {
            var url = $"https://sheets.googleapis.com/v4/spreadsheets/{_sheetId}/values/Outreach!A2:J?key={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<GoogleSheetData>(json);

            return data.Values.Select(row => new OutreachRecord
            {
                Name = row[0].ToString(),
                Email = row[1].ToString(),
                Position = row[2].ToString(),
                Organization = row[3].ToString(),
                OutreachDate = DateTime.Parse(row[4].ToString()),
                OutreachEffort = row[5].ToString(),
                OutreachType = row[6].ToString(),
                Status = row[7].ToString(),
                Notes = row[8].ToString(),
                OutreachBy = row[9].ToString(),
                FollowUpDate = string.IsNullOrEmpty(row[10].ToString()) ? null : DateTime.Parse(row[10].ToString())
            }).ToList();
        }

        public async Task AddOutreachRecord(OutreachRecord record)
        {
            var url = $"https://sheets.googleapis.com/v4/spreadsheets/{_sheetId}/values/Outreach!A:J:append?valueInputOption=RAW&key={_apiKey}";

            var values = new List<object>
            {
                record.Name,
                record.Email,
                record.Position,
                record.Organization,
                record.OutreachDate.ToString("yyyy-MM-dd"),
                record.OutreachEffort,
                record.OutreachType,
                record.Status,
                record.Notes,
                record.OutreachBy,
                record.FollowUpDate?.ToString("yyyy-MM-dd") ?? ""
            };

            var requestBody = new
            {
                range = "Outreach!A:J",
                majorDimension = "ROWS",
                values = new List<List<object>> { values }
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }
    }

    public class GoogleSheetData
    {
        public List<List<string>> Values { get; set; }
    }

}
public class DashboardInfo
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Controller { get; set; }
    public string Action { get; set; }
    public string Icon { get; set; }
}