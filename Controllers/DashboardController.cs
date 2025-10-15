// Controllers/DashboardController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnrollmentDashboard.Models;
using EnrollmentDashboard.Services;
using Microsoft.Extensions.Caching.Memory;

namespace EnrollmentDashboard.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly IMemoryCache _cache;
        private const string CACHE_KEY = "EnrollmentData";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(30);

        public DashboardController(IEnrollmentService enrollmentService, IMemoryCache cache)
        {
            _enrollmentService = enrollmentService;
            _cache = cache;
        }

        public async Task<IActionResult> Index(DashboardFilters? filters = null)
        {
            filters = filters ?? new DashboardFilters();

            // Try to get data from cache
            List<EnrollmentData>? allData;
            if (!_cache.TryGetValue(CACHE_KEY, out allData))
            {
                // Fetch data from service
                allData = await _enrollmentService.GetAllEnrollmentDataAsync();

                // Cache the data
                _cache.Set(CACHE_KEY, allData, CACHE_DURATION);
            }

            var viewModel = new DashboardViewModel
            {
                AllData = allData,
                COEData = allData.Where(d => d.Department == "COE").ToList(),
                COSData = allData.Where(d => d.Department == "COS").ToList(),
                Filters = filters,
                Metrics = _enrollmentService.CalculateMetrics(allData, filters),
                TopPrograms = _enrollmentService.GetTopPrograms(allData, filters)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> FilterData(DashboardFilters filters)
        {
            if (filters == null)
            {
                filters = new DashboardFilters();
            }

            // Apply filters and get data
            List<EnrollmentData>? allData;
            if (!_cache.TryGetValue(CACHE_KEY, out allData))
            {
                allData = await _enrollmentService.GetAllEnrollmentDataAsync();
                _cache.Set(CACHE_KEY, allData, CACHE_DURATION);
            }

            var viewModel = new DashboardViewModel
            {
                AllData = allData,
                COEData = allData.Where(d => d.Department == "COE").ToList(),
                COSData = allData.Where(d => d.Department == "COS").ToList(),
                Filters = filters, // Keep the filters
                Metrics = _enrollmentService.CalculateMetrics(allData, filters),
                TopPrograms = _enrollmentService.GetTopPrograms(allData, filters)
            };

            return View("Index", viewModel); // Return the view with the model
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string department = "All", string year = "All", string chartType = "enrollment")
        {
            List<EnrollmentData>? allData;
            if (!_cache.TryGetValue(CACHE_KEY, out allData))
            {
                allData = await _enrollmentService.GetAllEnrollmentDataAsync();
                _cache.Set(CACHE_KEY, allData, CACHE_DURATION);
            }

            var filters = new DashboardFilters { Department = department, Year = year };
            var filteredData = FilterDataByDepartment(allData, department);

            object? chartData = null;

            switch (chartType)
            {
                case "enrollment":
                    chartData = GetEnrollmentFunnelData(filteredData, year);
                    break;
                case "conversion":
                    chartData = GetConversionRateData(filteredData, year);
                    break;
                case "international":
                    chartData = GetInternationalBreakdownData(filteredData, year);
                    break;
                case "trend":
                    chartData = GetTrendData(filteredData);
                    break;
                case "comparison":
                    chartData = GetDepartmentComparisonData(allData, year);
                    break;
            }

            return Json(chartData);
        }

        [HttpGet]
        public async Task<IActionResult> ExportData(string format = "csv", string department = "All", string year = "All")
        {
            List<EnrollmentData>? allData;
            if (!_cache.TryGetValue(CACHE_KEY, out allData))
            {
                allData = await _enrollmentService.GetAllEnrollmentDataAsync();
                _cache.Set(CACHE_KEY, allData, CACHE_DURATION);
            }

            var filteredData = FilterDataByDepartment(allData, department);

            if (format == "csv")
            {
                return ExportToCsv(filteredData, year);
            }
            else if (format == "json")
            {
                return Json(filteredData);
            }

            return BadRequest("Invalid export format");
        }

        private List<EnrollmentData> FilterDataByDepartment(List<EnrollmentData> data, string department)
        {
            if (department != "All")
            {
                return data.Where(d => d.Department == department).ToList();
            }
            return data;
        }

        private object GetEnrollmentFunnelData(List<EnrollmentData> data, string year)
        {
            var stages = new string[] { "Opportunities", "Submitted", "Completed", "Admitted", "Confirmed", "Registered" };
            var values = new List<int>();

            foreach (var stage in stages)
            {
                var total = 0;
                foreach (var enrollment in data)
                {
                    if (year == "All" || year == "2023")
                    {
                        switch (stage)
                        {
                            case "Opportunities": total += enrollment.Opportunities2023; break;
                            case "Submitted": total += enrollment.Submitted2023; break;
                            case "Completed": total += enrollment.Completed2023; break;
                            case "Admitted": total += enrollment.Admitted2023; break;
                            case "Confirmed": total += enrollment.Confirmed2023; break;
                            case "Registered": total += enrollment.Registered2023; break;
                        }
                    }
                    if (year == "All" || year == "2024")
                    {
                        switch (stage)
                        {
                            case "Opportunities": total += enrollment.Opportunities2024; break;
                            case "Submitted": total += enrollment.Submitted2024; break;
                            case "Completed": total += enrollment.Completed2024; break;
                            case "Admitted": total += enrollment.Admitted2024; break;
                            case "Confirmed": total += enrollment.Confirmed2024; break;
                            case "Registered": total += enrollment.Registered2024; break;
                        }
                    }
                    if (year == "All" || year == "2025")
                    {
                        switch (stage)
                        {
                            case "Opportunities": total += enrollment.Opportunities2025; break;
                            case "Submitted": total += enrollment.Submitted2025; break;
                            case "Completed": total += enrollment.Completed2025; break;
                            case "Admitted": total += enrollment.Admitted2025; break;
                            case "Confirmed": total += enrollment.Confirmed2025; break;
                            case "Registered": total += enrollment.Registered2025; break;
                        }
                    }
                }
                values.Add(total);
            }

            return new { labels = stages, data = values };
        }

        private object GetConversionRateData(List<EnrollmentData> data, string year)
        {
            var programs = data.Select(d => d.ProgramName).ToList();
            var conversionRates = new List<double>();

            foreach (var enrollment in data)
            {
                double opportunities = 0, registered = 0;

                if (year == "All" || year == "2023")
                {
                    opportunities += enrollment.Opportunities2023;
                    registered += enrollment.Registered2023;
                }
                if (year == "All" || year == "2024")
                {
                    opportunities += enrollment.Opportunities2024;
                    registered += enrollment.Registered2024;
                }
                if (year == "All" || year == "2025")
                {
                    opportunities += enrollment.Opportunities2025;
                    registered += enrollment.Registered2025;
                }

                var rate = opportunities > 0 ? registered / opportunities * 100 : 0;
                conversionRates.Add(Math.Round(rate, 2));
            }

            return new { labels = programs, data = conversionRates };
        }

        private object GetInternationalBreakdownData(List<EnrollmentData> data, string year)
        {
            int totalInternational = 0, totalDomestic = 0;

            foreach (var enrollment in data)
            {
                if (year == "All" || year == "2023")
                {
                    totalInternational += enrollment.RegisteredInternational2023;
                    totalDomestic += enrollment.RegisteredDomestic2023;
                }
                if (year == "All" || year == "2024")
                {
                    totalInternational += enrollment.RegisteredInternational2024;
                    totalDomestic += enrollment.RegisteredDomestic2024;
                }
                if (year == "All" || year == "2025")
                {
                    totalInternational += enrollment.RegisteredInternational2025;
                    totalDomestic += enrollment.RegisteredDomestic2025;
                }
            }

            return new
            {
                labels = new string[] { "International", "Domestic" },
                data = new int[] { totalInternational, totalDomestic }
            };
        }

        private object GetTrendData(List<EnrollmentData> data)
        {
            var years = new string[] { "2023", "2024", "2025" };
            var registered = new List<int>();
            var opportunities = new List<int>();
            var conversionRates = new List<double>();

            foreach (var year in years)
            {
                int totalRegistered = 0, totalOpportunities = 0;

                foreach (var enrollment in data)
                {
                    switch (year)
                    {
                        case "2023":
                            totalRegistered += enrollment.Registered2023;
                            totalOpportunities += enrollment.Opportunities2023;
                            break;
                        case "2024":
                            totalRegistered += enrollment.Registered2024;
                            totalOpportunities += enrollment.Opportunities2024;
                            break;
                        case "2025":
                            totalRegistered += enrollment.Registered2025;
                            totalOpportunities += enrollment.Opportunities2025;
                            break;
                    }
                }

                registered.Add(totalRegistered);
                opportunities.Add(totalOpportunities);
                var rate = totalOpportunities > 0 ? (double)totalRegistered / totalOpportunities * 100 : 0;
                conversionRates.Add(Math.Round(rate, 2));
            }

            return new
            {
                labels = years,
                datasets = new object[]
                {
                    new { label = "Registered", data = registered },
                    new { label = "Opportunities", data = opportunities },
                    new { label = "Conversion Rate %", data = conversionRates }
                }
            };
        }

        private object GetDepartmentComparisonData(List<EnrollmentData> data, string year)
        {
            var coeData = data.Where(d => d.Department == "COE").ToList();
            var cosData = data.Where(d => d.Department == "COS").ToList();

            var metrics = new string[] { "Opportunities", "Registered", "International", "Domestic" };
            var coeValues = new List<int>();
            var cosValues = new List<int>();

            foreach (var metric in metrics)
            {
                int coeTotal = 0, cosTotal = 0;

                foreach (var enrollment in coeData)
                {
                    coeTotal += GetMetricTotal(enrollment, metric, year);
                }

                foreach (var enrollment in cosData)
                {
                    cosTotal += GetMetricTotal(enrollment, metric, year);
                }

                coeValues.Add(coeTotal);
                cosValues.Add(cosTotal);
            }

            return new
            {
                labels = metrics,
                datasets = new object[]
                {
                    new { label = "COE", data = coeValues },
                    new { label = "COS", data = cosValues }
                }
            };
        }

        private int GetMetricTotal(EnrollmentData enrollment, string metric, string year)
        {
            int total = 0;

            if (year == "All" || year == "2023")
            {
                switch (metric)
                {
                    case "Opportunities": total += enrollment.Opportunities2023; break;
                    case "Registered": total += enrollment.Registered2023; break;
                    case "International": total += enrollment.RegisteredInternational2023; break;
                    case "Domestic": total += enrollment.RegisteredDomestic2023; break;
                }
            }
            if (year == "All" || year == "2024")
            {
                switch (metric)
                {
                    case "Opportunities": total += enrollment.Opportunities2024; break;
                    case "Registered": total += enrollment.Registered2024; break;
                    case "International": total += enrollment.RegisteredInternational2024; break;
                    case "Domestic": total += enrollment.RegisteredDomestic2024; break;
                }
            }
            if (year == "All" || year == "2025")
            {
                switch (metric)
                {
                    case "Opportunities": total += enrollment.Opportunities2025; break;
                    case "Registered": total += enrollment.Registered2025; break;
                    case "International": total += enrollment.RegisteredInternational2025; break;
                    case "Domestic": total += enrollment.RegisteredDomestic2025; break;
                }
            }

            return total;
        }

        private IActionResult ExportToCsv(List<EnrollmentData> data, string year)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Program Name,Department,Opportunities,Submitted,Completed,Admitted,Confirmed,Registered,International,Domestic,Conversion Rate %");

            foreach (var enrollment in data)
            {
                int opportunities = 0, submitted = 0, completed = 0, admitted = 0, confirmed = 0,
                    registered = 0, international = 0, domestic = 0;

                if (year == "All" || year == "2023")
                {
                    opportunities += enrollment.Opportunities2023;
                    submitted += enrollment.Submitted2023;
                    completed += enrollment.Completed2023;
                    admitted += enrollment.Admitted2023;
                    confirmed += enrollment.Confirmed2023;
                    registered += enrollment.Registered2023;
                    international += enrollment.RegisteredInternational2023;
                    domestic += enrollment.RegisteredDomestic2023;
                }
                if (year == "All" || year == "2024")
                {
                    opportunities += enrollment.Opportunities2024;
                    submitted += enrollment.Submitted2024;
                    completed += enrollment.Completed2024;
                    admitted += enrollment.Admitted2024;
                    confirmed += enrollment.Confirmed2024;
                    registered += enrollment.Registered2024;
                    international += enrollment.RegisteredInternational2024;
                    domestic += enrollment.RegisteredDomestic2024;
                }
                if (year == "All" || year == "2025")
                {
                    opportunities += enrollment.Opportunities2025;
                    submitted += enrollment.Submitted2025;
                    completed += enrollment.Completed2025;
                    admitted += enrollment.Admitted2025;
                    confirmed += enrollment.Confirmed2025;
                    registered += enrollment.Registered2025;
                    international += enrollment.RegisteredInternational2025;
                    domestic += enrollment.RegisteredDomestic2025;
                }

                var conversionRate = opportunities > 0 ? (double)registered / opportunities * 100 : 0;

                csv.AppendLine($"{enrollment.ProgramName},{enrollment.Department},{opportunities},{submitted},{completed},{admitted},{confirmed},{registered},{international},{domestic},{conversionRate:F2}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"enrollment_data_{year}_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> TestDataFetch()
        {
            try
            {
                var coeData = await _enrollmentService.GetCOEDataAsync();
                var cosData = await _enrollmentService.GetCOSDataAsync();

                return Json(new
                {
                    COECount = coeData.Count,
                    COSCount = cosData.Count,
                    COESample = coeData.Take(3).Select(d => new {
                        d.ProgramName,
                        d.Opportunities2023,
                        d.Opportunities2024,
                        d.Opportunities2025,
                        d.Submitted2023,
                        d.Submitted2024,
                        d.Submitted2025,
                        d.TotalOpportunities,
                        d.TotalRegistered
                    }),
                    COSSample = cosData.Take(3).Select(d => new {
                        d.ProgramName,
                        d.Opportunities2023,
                        d.Opportunities2024,
                        d.Opportunities2025,
                        d.TotalOpportunities,
                        d.TotalRegistered
                    }),
                    COETotals = new
                    {
                        TotalOpportunities2023 = coeData.Sum(d => d.Opportunities2023),
                        TotalOpportunities2024 = coeData.Sum(d => d.Opportunities2024),
                        TotalOpportunities2025 = coeData.Sum(d => d.Opportunities2025),
                        TotalSubmitted2023 = coeData.Sum(d => d.Submitted2023),
                        TotalSubmitted2024 = coeData.Sum(d => d.Submitted2024),
                        TotalSubmitted2025 = coeData.Sum(d => d.Submitted2025)
                    },
                    Message = "Data fetch test completed"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestCSVFetch()
        {
            try
            {
                var httpClient = new HttpClient();
                var csvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQTDN-9jb83h9QCsWOT0V4yNHyPjgvn6HDH_caabIyqC5CJRYLZiQPheZdB0NGpixDurjSJbpBIA3I6/pub?gid=1490740400&single=true&output=csv";

                var csvContent = await httpClient.GetStringAsync(csvUrl);
                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                // Parse header to show column mapping
                var headerValues = ParseCSVLine(lines[0]);
                var firstDataLine = lines.Length > 1 ? ParseCSVLine(lines[1]) : new List<string>();

                return Json(new
                {
                    TotalLines = lines.Length,
                    HeaderCount = headerValues.Count,
                    Headers = headerValues.Select((h, i) => new { Index = i, Header = h }),
                    FirstDataRow = firstDataLine.Select((v, i) => new { Index = i, Value = v }),
                    FirstFewLines = lines.Take(3).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        private List<string> ParseCSVLine(string line)
        {
            var values = new List<string>();
            var current = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            values.Add(current);
            return values;
        }

        [HttpPost]
        public async Task<IActionResult> RefreshData()
        {
            _cache.Remove(CACHE_KEY);
            return RedirectToAction(nameof(Index));
        }
    }
}