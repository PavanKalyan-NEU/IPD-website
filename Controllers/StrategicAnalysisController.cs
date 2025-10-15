using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using USPTOQueryBuilder.Models;
using USPTOQueryBuilder.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace USPTOQueryBuilder.Controllers
{
    public class StrategicAnalysisController : Controller
    {
        private readonly PatentsViewApiService _apiService;
        private readonly ILogger<StrategicAnalysisController> _logger;

        public StrategicAnalysisController(
            PatentsViewApiService apiService,
            ILogger<StrategicAnalysisController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var model = new StrategicDashboardViewModel
            {
                StartYear = DateTime.Now.Year - 5,
                EndYear = DateTime.Now.Year,
                SelectedTimeframe = "5years"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GetMarketDynamics([FromBody] MarketAnalysisRequest request)
        {
            try
            {
                var results = new MarketDynamicsResponse
                {
                    TimeSeriesData = new List<TimeSeriesDataPoint>(),
                    TopGrowthAreas = new List<GrowthAreaMetrics>(),
                    CompanyLeaderboard = new List<CompanyPatentActivity>(),
                    GeographicHeatmap = new List<GeographicActivity>()
                };

                // Analyze patent trends for each year
                for (int year = request.StartYear; year <= request.EndYear; year++)
                {
                    var yearData = await GetYearlyPatentData(year);
                    results.TimeSeriesData.Add(new TimeSeriesDataPoint
                    {
                        Year = year,
                        TotalPatents = yearData.TotalPatents,
                        Categories = yearData.Categories
                    });
                }

                // Get growth areas by analyzing patent abstracts
                var growthAreas = await AnalyzeGrowthAreas(request);
                results.TopGrowthAreas = growthAreas;

                // Get company patent activity
                var companyData = await GetTopCompanies(request);
                results.CompanyLeaderboard = companyData;

                // Get geographic distribution
                var geoData = await GetGeographicDistribution(request);
                results.GeographicHeatmap = geoData;

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing market dynamics");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSkillsClusters([FromBody] SkillsAnalysisRequest request)
        {
            try
            {
                var results = new SkillsClustersResponse
                {
                    SkillClusters = new List<SkillCluster>(),
                    CrossDisciplinaryAreas = new List<CrossDisciplinaryMetric>()
                };

                // Analyze patent data to identify skill clusters
                var clusters = await AnalyzeSkillClusters(request);
                results.SkillClusters = clusters;

                // Find cross-disciplinary innovation areas
                var crossAreas = await FindCrossDisciplinaryAreas(request);
                results.CrossDisciplinaryAreas = crossAreas;

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing skills clusters");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetEmergingTechnologies([FromBody] EmergingTechRequest request)
        {
            try
            {
                var results = new EmergingTechnologiesResponse
                {
                    EmergingAreas = new List<EmergingTechnology>(),
                    PredictedGrowth = new List<GrowthPrediction>()
                };

                // Search for emerging technology patents
                var emergingTech = await SearchEmergingTechnologies(request);
                results.EmergingAreas = emergingTech;

                // Calculate growth predictions
                var predictions = await CalculateGrowthPredictions(emergingTech);
                results.PredictedGrowth = predictions;

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing emerging technologies");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<YearlyPatentData> GetYearlyPatentData(int year)
        {
            var query = new PatentQuery
            {
                Category = "Patents",
                StartDate = new DateTime(year, 1, 1),
                EndDate = new DateTime(year, 12, 31),
                OutputFields = new List<string> { "patent_id", "patent_abstract", "cpc_current.cpc_group_id" }
            };

            var results = await _apiService.PreviewQuery(query);

            return new YearlyPatentData
            {
                Year = year,
                TotalPatents = results.TotalRecords,
                Categories = new Dictionary<string, int>() // Will be populated from CPC codes
            };
        }

        private async Task<List<GrowthAreaMetrics>> AnalyzeGrowthAreas(MarketAnalysisRequest request)
        {
            var growthAreas = new List<GrowthAreaMetrics>();

            // Define search terms for key technology areas
            var technologySearchTerms = new Dictionary<string, string>
            {
                { "AI/Machine Learning", "artificial intelligence machine learning deep learning neural network" },
                { "Biotechnology", "biotechnology bioengineering CRISPR genetic engineering synthetic biology" },
                { "Clean Energy", "renewable energy solar wind battery energy storage carbon capture" },
                { "Quantum Computing", "quantum computing quantum algorithm quantum cryptography" },
                { "Robotics", "robotics automation autonomous systems robot" },
                { "Materials Science", "nanomaterials metamaterials smart materials graphene" },
                { "IoT/Edge Computing", "internet of things IoT edge computing sensor network" },
                { "Cybersecurity", "cybersecurity encryption blockchain security" }
            };

            foreach (var area in technologySearchTerms)
            {
                try
                {
                    // Get yearly data for growth calculation
                    var yearlyData = new List<int>();

                    for (int year = request.StartYear; year <= request.EndYear; year++)
                    {
                        var yearQuery = new PatentQuery
                        {
                            Category = "Patents",
                            StartDate = new DateTime(year, 1, 1),
                            EndDate = new DateTime(year, 12, 31),
                            SearchCriteria = new List<SearchCriteria>
                            {
                                new SearchCriteria
                                {
                                    Field = "patent_abstract",
                                    Operator = "text_any",
                                    Value = area.Value
                                }
                            },
                            OutputFields = new List<string> { "patent_id" }
                        };

                        var yearResults = await _apiService.PreviewQuery(yearQuery);
                        yearlyData.Add(yearResults.TotalRecords);
                    }

                    // Calculate proper growth rate
                    double growthRate = 0;
                    if (yearlyData.Count >= 2 && yearlyData[0] > 0)
                    {
                        // Calculate compound annual growth rate (CAGR)
                        int startValue = yearlyData[0];
                        int endValue = yearlyData[yearlyData.Count - 1];
                        int years = yearlyData.Count - 1;

                        if (years > 0 && startValue > 0 && endValue > 0)
                        {
                            growthRate = (Math.Pow((double)endValue / startValue, 1.0 / years) - 1) * 100;
                        }
                    }

                    // Total patents across all years
                    int totalPatents = yearlyData.Sum();

                    growthAreas.Add(new GrowthAreaMetrics
                    {
                        TechnologyArea = area.Key,
                        TotalPatents = totalPatents,
                        GrowthRate = Math.Round(growthRate, 1),
                        YearlyTrend = yearlyData
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error analyzing {area.Key}: {ex.Message}");
                }
            }

            return growthAreas.OrderByDescending(x => x.TotalPatents).ToList();
        }

        private async Task<List<CompanyPatentActivity>> GetTopCompanies(MarketAnalysisRequest request)
        {
            var query = new PatentQuery
            {
                Category = "Patents",
                StartDate = new DateTime(request.StartYear, 1, 1),
                EndDate = new DateTime(request.EndYear, 12, 31),
                OutputFields = new List<string>
                {
                    "patent_id",
                    "assignees.assignee_organization",
                    "assignees.assignee_type",
                    "patent_abstract",
                    "patent_date"
                }
            };

            var results = await _apiService.ExecuteFullQueryAsJson(query);

            // Group by assignee and analyze
            var companyGroups = results.Data
                .Where(d => d.ContainsKey("assignees.assignee_organization") &&
                           d["assignees.assignee_organization"] != null &&
                           !string.IsNullOrWhiteSpace(d["assignees.assignee_organization"].ToString()))
                .GroupBy(d => d["assignees.assignee_organization"].ToString())
                .Select(g => {
                    var company = new CompanyPatentActivity
                    {
                        CompanyName = g.Key,
                        TotalPatents = g.Count()
                    };

                    // Determine if it's a university
                    var firstRecord = g.First();
                    if (firstRecord.ContainsKey("assignees.assignee_type"))
                    {
                        var assigneeType = firstRecord["assignees.assignee_type"]?.ToString();
                        // Type 4 is typically universities/academic institutions
                        if (assigneeType == "4" || g.Key.ToLower().Contains("university") ||
                            g.Key.ToLower().Contains("institute") || g.Key.ToLower().Contains("college"))
                        {
                            company.IsUniversity = true;
                        }
                    }

                    // Calculate yearly patent counts for growth analysis
                    var yearlyPatents = g
                        .Where(p => p.ContainsKey("patent_date") && p["patent_date"] != null)
                        .GroupBy(p => DateTime.Parse(p["patent_date"].ToString()).Year)
                        .Select(yg => yg.Count())
                        .ToList();

                    company.YearlyPatentCounts = yearlyPatents;

                    return company;
                })
                .OrderByDescending(c => c.TotalPatents)
                .Take(30) // Get top 30
                .ToList();

            return companyGroups;
        }

        private async Task<List<InventorActivity>> GetTopInventors(MarketAnalysisRequest request)
        {
            var query = new PatentQuery
            {
                Category = "Patents",
                StartDate = new DateTime(request.StartYear, 1, 1),
                EndDate = new DateTime(request.EndYear, 12, 31),
                OutputFields = new List<string>
                {
                    "patent_id",
                    "inventors.inventor_name_first",
                    "inventors.inventor_name_last",
                    "inventors.inventor_city",
                    "inventors.inventor_state",
                    "assignees.assignee_organization"
                }
            };

            var results = await _apiService.ExecuteFullQueryAsJson(query);

            // Group by inventor full name
            var inventorGroups = results.Data
                .Where(d => d.ContainsKey("inventors.inventor_name_last") &&
                           d["inventors.inventor_name_last"] != null)
                .GroupBy(d => new
                {
                    FirstName = d.ContainsKey("inventors.inventor_name_first") ?
                                d["inventors.inventor_name_first"]?.ToString() : "",
                    LastName = d["inventors.inventor_name_last"].ToString()
                })
                .Select(g => new InventorActivity
                {
                    InventorName = $"{g.Key.FirstName} {g.Key.LastName}".Trim(),
                    PatentCount = g.Count(),
                    Location = g.First().ContainsKey("inventors.inventor_city") &&
                              g.First().ContainsKey("inventors.inventor_state") ?
                              $"{g.First()["inventors.inventor_city"]}, {g.First()["inventors.inventor_state"]}" :
                              "Unknown",
                    PrimaryAssignee = g.First().ContainsKey("assignees.assignee_organization") ?
                                    g.First()["assignees.assignee_organization"]?.ToString() :
                                    "Independent"
                })
                .OrderByDescending(i => i.PatentCount)
                .Take(20) // Top 20 inventors
                .ToList();

            return inventorGroups;
        }

        [HttpPost]
        public async Task<IActionResult> GetFilteredTechnologyData([FromBody] TechnologyFilterRequest request)
        {
            try
            {
                var results = new FilteredTechnologyResponse
                {
                    PatentData = new List<PatentDataPoint>(),
                    TotalPatents = 0,
                    GrowthMetrics = new Dictionary<string, double>(),
                    TopInnovators = new List<InnovatorInfo>(),
                    YearlyTrends = new Dictionary<int, int>()
                };

                // Build search query based on selected categories
                var searchTerms = BuildSearchTerms(request.SelectedCategories);

                // Get patent data for selected categories
                var query = new PatentQuery
                {
                    Category = "Patents",
                    StartDate = new DateTime(request.StartYear, 1, 1),
                    EndDate = new DateTime(request.EndYear, 12, 31),
                    SearchCriteria = new List<SearchCriteria>
                    {
                        new SearchCriteria
                        {
                            Field = "patent_abstract",
                            Operator = "text_any",
                            Value = searchTerms
                        }
                    },
                    OutputFields = new List<string>
                    {
                        "patent_id",
                        "patent_date",
                        "patent_title",
                        "patent_abstract",
                        "assignees.assignee_organization",
                        "inventors.inventor_name_first",
                        "inventors.inventor_name_last",
                        "inventors.inventor_city",
                        "inventors.inventor_state"
                    }
                };

                // Execute query with pagination for large results
                var allResults = await _apiService.ExecuteFullQueryAsJson(query);

                if (allResults.Success && allResults.Data != null)
                {
                    results.TotalPatents = allResults.TotalRecords;

                    // Process yearly trends
                    var yearlyGroups = allResults.Data
                        .Where(p => p.ContainsKey("patent_date") && p["patent_date"] != null)
                        .GroupBy(p => DateTime.Parse(p["patent_date"].ToString()).Year)
                        .OrderBy(g => g.Key);

                    foreach (var yearGroup in yearlyGroups)
                    {
                        results.YearlyTrends[yearGroup.Key] = yearGroup.Count();
                    }

                    // Calculate growth metrics
                    if (results.YearlyTrends.Count >= 2)
                    {
                        var years = results.YearlyTrends.Keys.OrderBy(y => y).ToList();
                        var firstYear = results.YearlyTrends[years.First()];
                        var lastYear = results.YearlyTrends[years.Last()];

                        if (firstYear > 0)
                        {
                            var cagr = (Math.Pow((double)lastYear / firstYear, 1.0 / (years.Count - 1)) - 1) * 100;
                            results.GrowthMetrics["CAGR"] = Math.Round(cagr, 2);
                        }
                    }

                    // Get top innovators
                    var topAssignees = allResults.Data
                        .Where(p => p.ContainsKey("assignees.assignee_organization") &&
                                   p["assignees.assignee_organization"] != null)
                        .GroupBy(p => p["assignees.assignee_organization"].ToString())
                        .Select(g => new InnovatorInfo
                        {
                            Name = g.Key,
                            PatentCount = g.Count(),
                            Type = DetermineOrganizationType(g.Key)
                        })
                        .OrderByDescending(i => i.PatentCount)
                        .Take(20)
                        .ToList();

                    results.TopInnovators = topAssignees;

                    // Sample patent data for visualization (limit to prevent overload)
                    results.PatentData = allResults.Data
                        .Take(100)
                        .Select(p => new PatentDataPoint
                        {
                            PatentId = p.ContainsKey("patent_id") ? p["patent_id"]?.ToString() : "",
                            Title = p.ContainsKey("patent_title") ? p["patent_title"]?.ToString() : "",
                            Date = p.ContainsKey("patent_date") ? DateTime.Parse(p["patent_date"].ToString()) : DateTime.MinValue,
                            Assignee = p.ContainsKey("assignees.assignee_organization") ? p["assignees.assignee_organization"]?.ToString() : ""
                        })
                        .ToList();
                }

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered technology data");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private string BuildSearchTerms(List<string> categories)
        {
            var termMap = new Dictionary<string, string>
            {
                { "AI & Engineering", "artificial intelligence machine learning engineering automation control systems" },
                { "Climate & Materials", "climate technology carbon capture renewable energy materials science nanomaterials" },
                { "Bioengineering & Health", "bioengineering biotechnology digital health medical devices biosensors" },
                { "Autonomous Systems", "autonomous systems robotics self-driving automated vehicles drones" },
                { "Quantum Technology", "quantum computing quantum technology quantum sensors quantum cryptography" },
                { "Program Control", "program control embedded systems microcontroller firmware real-time systems" },
                { "Tech Giants", "Google Apple Microsoft Amazon Meta IBM Intel" },
                { "Emerging Companies", "startup emerging technology innovation disruptive" }
            };

            var terms = new List<string>();
            foreach (var category in categories)
            {
                if (termMap.ContainsKey(category))
                {
                    terms.Add(termMap[category]);
                }
            }

            return string.Join(" ", terms);
        }

        private string DetermineOrganizationType(string organizationName)
        {
            var name = organizationName.ToLower();
            if (name.Contains("university") || name.Contains("institute") ||
                name.Contains("college") || name.Contains("academia"))
            {
                return "University";
            }
            else if (name.Contains("inc") || name.Contains("corp") ||
                     name.Contains("llc") || name.Contains("limited"))
            {
                return "Company";
            }
            else
            {
                return "Other";
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetInventorsAndAssignees([FromBody] MarketAnalysisRequest request)
        {
            try
            {
                // Get top companies and universities
                var companies = await GetTopCompanies(request);

                // Separate universities and companies
                var universities = companies.Where(c => c.IsUniversity).Take(10).ToList();
                var corporations = companies.Where(c => !c.IsUniversity).Take(10).ToList();

                // Get top inventors
                var inventors = await GetTopInventors(request);

                return Json(new
                {
                    universities = universities.Select(u => new
                    {
                        name = u.CompanyName,
                        patents = u.TotalPatents
                    }),
                    companies = corporations.Select(c => new
                    {
                        name = c.CompanyName,
                        patents = c.TotalPatents
                    }),
                    inventors = inventors.Select(i => new
                    {
                        name = i.InventorName,
                        patents = i.PatentCount,
                        location = i.Location,
                        assignee = i.PrimaryAssignee
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventors and assignees");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<List<GeographicActivity>> GetGeographicDistribution(MarketAnalysisRequest request)
        {
            var query = new PatentQuery
            {
                Category = "Patents",
                StartDate = new DateTime(request.StartYear, 1, 1),
                EndDate = new DateTime(request.EndYear, 12, 31),
                OutputFields = new List<string>
                {
                    "patent_id",
                    "inventors.inventor_country",
                    "inventors.inventor_state"
                }
            };

            var results = await _apiService.ExecuteFullQueryAsJson(query);

            // Group by country/state
            var geoGroups = results.Data
                .Where(d => d.ContainsKey("inventors.inventor_country"))
                .GroupBy(d => new
                {
                    Country = d["inventors.inventor_country"]?.ToString() ?? "Unknown",
                    State = d.ContainsKey("inventors.inventor_state") ? d["inventors.inventor_state"]?.ToString() : null
                })
                .Select(g => new GeographicActivity
                {
                    Country = g.Key.Country,
                    State = g.Key.State,
                    PatentCount = g.Count()
                })
                .OrderByDescending(g => g.PatentCount)
                .ToList();

            return geoGroups;
        }

        private async Task<List<SkillCluster>> AnalyzeSkillClusters(SkillsAnalysisRequest request)
        {
            // Analyze patent classifications to identify skill clusters
            var clusters = new List<SkillCluster>();

            // This would analyze CPC codes and abstracts to identify skill patterns
            // Implementation would use text analysis on patent abstracts

            return clusters;
        }

        private async Task<List<CrossDisciplinaryMetric>> FindCrossDisciplinaryAreas(SkillsAnalysisRequest request)
        {
            // Find patents that span multiple CPC classifications
            var crossAreas = new List<CrossDisciplinaryMetric>();

            // Implementation would analyze patents with multiple CPC codes

            return crossAreas;
        }

        private async Task<List<EmergingTechnology>> SearchEmergingTechnologies(EmergingTechRequest request)
        {
            // Search for recent patents with high citation rates or novel concepts
            var emergingTech = new List<EmergingTechnology>();

            // Implementation would use citation analysis and keyword emergence

            return emergingTech;
        }

        private async Task<List<GrowthPrediction>> CalculateGrowthPredictions(List<EmergingTechnology> technologies)
        {
            // Calculate growth predictions based on historical trends
            var predictions = new List<GrowthPrediction>();

            // Implementation would use trend analysis

            return predictions;
        }
    }
}