// Services/EnrollmentService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EnrollmentDashboard.Models;
using System.Globalization;
using System.IO;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace EnrollmentDashboard.Services
{
    public interface IEnrollmentService
    {
        Task<List<EnrollmentData>> GetAllEnrollmentDataAsync();
        Task<List<EnrollmentData>> GetCOEDataAsync();
        Task<List<EnrollmentData>> GetCOSDataAsync();
        DashboardMetrics CalculateMetrics(List<EnrollmentData> data, DashboardFilters filters);
        List<ProgramSummary> GetTopPrograms(List<EnrollmentData> data, DashboardFilters filters);
    }

    public class EnrollmentService : IEnrollmentService
    {
        private readonly HttpClient _httpClient;
        // CSV export URLs - more reliable than HTML parsing
        private const string COE_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQTDN-9jb83h9QCsWOT0V4yNHyPjgvn6HDH_caabIyqC5CJRYLZiQPheZdB0NGpixDurjSJbpBIA3I6/pub?gid=1490740400&single=true&output=csv";
        private const string COS_CSV_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQTDN-9jb83h9QCsWOT0V4yNHyPjgvn6HDH_caabIyqC5CJRYLZiQPheZdB0NGpixDurjSJbpBIA3I6/pub?gid=1674427619&single=true&output=csv";

        public EnrollmentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<EnrollmentData>> GetAllEnrollmentDataAsync()
        {
            var coeData = await GetCOEDataAsync();
            var cosData = await GetCOSDataAsync();
            return coeData.Concat(cosData).ToList();
        }

        public async Task<List<EnrollmentData>> GetCOEDataAsync()
        {
            return await FetchAndParseCSVData(COE_CSV_URL, "COE");
        }

        public async Task<List<EnrollmentData>> GetCOSDataAsync()
        {
            return await FetchAndParseCSVData(COS_CSV_URL, "COS");
        }

        private async Task<List<EnrollmentData>> FetchAndParseCSVData(string url, string department)
        {
            var enrollmentData = new List<EnrollmentData>();

            try
            {
                Console.WriteLine($"Fetching CSV data from: {url}");
                var csvContent = await _httpClient.GetStringAsync(url);
                Console.WriteLine($"CSV received: {csvContent.Length} characters");

                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"CSV has {lines.Length} lines");

                if (lines.Length < 2)
                {
                    Console.WriteLine("Not enough lines in CSV");
                    return enrollmentData;
                }

                // Skip header row
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = ParseCSVLine(line);

                    if (values.Count < 17) // We need at least 17 columns up to Confirmed 2025
                    {
                        Console.WriteLine($"Line {i} has only {values.Count} values, expected at least 17");
                        continue;
                    }

                    var enrollment = new EnrollmentData
                    {
                        Department = department,
                        ProgramName = values[0].Trim(),

                        // Opportunities - Columns C, D, E (indices 2, 3, 4)
                        Opportunities2023 = ParseInt(values[2]),
                        Opportunities2024 = ParseInt(values[3]),
                        Opportunities2025 = ParseInt(values[4]),

                        // Submitted - Columns F, G, H (indices 5, 6, 7)
                        Submitted2023 = ParseInt(values[5]),
                        Submitted2024 = ParseInt(values[6]),
                        Submitted2025 = ParseInt(values[7]),

                        // Completed - Columns I, J, K (indices 8, 9, 10)
                        Completed2023 = ParseInt(values[8]),
                        Completed2024 = ParseInt(values[9]),
                        Completed2025 = ParseInt(values[10]),

                        // Admitted - Columns L, M, N (indices 11, 12, 13)
                        Admitted2023 = ParseInt(values[11]),
                        Admitted2024 = ParseInt(values[12]),
                        Admitted2025 = ParseInt(values[13]),

                        // Confirmed - Columns O, P, Q (indices 14, 15, 16)
                        Confirmed2023 = ParseInt(values[14]),
                        Confirmed2024 = ParseInt(values[15]),
                        Confirmed2025 = ParseInt(values[16]),

                        // Registered - Need to check if these columns exist (R, S, T - indices 17, 18, 19)
                        Registered2023 = values.Count > 17 ? ParseInt(values[17]) : 0,
                        Registered2024 = values.Count > 18 ? ParseInt(values[18]) : 0,
                        Registered2025 = values.Count > 19 ? ParseInt(values[19]) : 0,

                        // International - Need to check if these columns exist (U, V, W - indices 20, 21, 22)
                        RegisteredInternational2023 = values.Count > 20 ? ParseInt(values[20]) : 0,
                        RegisteredInternational2024 = values.Count > 21 ? ParseInt(values[21]) : 0,
                        RegisteredInternational2025 = values.Count > 22 ? ParseInt(values[22]) : 0,

                        // Domestic - Need to check if these columns exist (X, Y, Z - indices 23, 24, 25)
                        RegisteredDomestic2023 = values.Count > 23 ? ParseInt(values[23]) : 0,
                        RegisteredDomestic2024 = values.Count > 24 ? ParseInt(values[24]) : 0,
                        RegisteredDomestic2025 = values.Count > 25 ? ParseInt(values[25]) : 0
                    };

                    if (!string.IsNullOrWhiteSpace(enrollment.ProgramName))
                    {
                        enrollmentData.Add(enrollment);
                        Console.WriteLine($"Added program: {enrollment.ProgramName} with {enrollment.TotalRegistered} total registered");
                    }
                }

                Console.WriteLine($"Total programs parsed for {department}: {enrollmentData.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching CSV data from {url}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return enrollmentData;
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

        private async Task<List<EnrollmentData>> FetchAndParseData(string url, string department)
        {
            var enrollmentData = new List<EnrollmentData>();

            try
            {
                Console.WriteLine($"Fetching data from: {url}");
                var html = await _httpClient.GetStringAsync(url);
                Console.WriteLine($"HTML received: {html.Length} characters");

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Find the table containing the data
                var table = doc.DocumentNode.SelectSingleNode("//table[@class='waffle']");
                if (table == null)
                {
                    Console.WriteLine("No table with class 'waffle' found. Looking for any table...");
                    table = doc.DocumentNode.SelectSingleNode("//table");
                    if (table == null)
                    {
                        Console.WriteLine("No table found at all!");
                        return enrollmentData;
                    }
                }

                var rows = table.SelectNodes(".//tr");
                if (rows == null || rows.Count < 2)
                {
                    Console.WriteLine($"Insufficient rows: {rows?.Count ?? 0}");
                    return enrollmentData;
                }

                Console.WriteLine($"Found {rows.Count} rows in the table");

                // Skip header row and process data rows
                for (int i = 1; i < rows.Count; i++)
                {
                    var cells = rows[i].SelectNodes(".//td");
                    if (cells == null || cells.Count < 25)
                    {
                        Console.WriteLine($"Row {i} has only {cells?.Count ?? 0} cells, skipping...");
                        continue;
                    }

                    var enrollment = new EnrollmentData
                    {
                        Department = department,
                        ProgramName = CleanCellText(cells[0].InnerText),

                        // 2023 Data
                        Opportunities2023 = ParseInt(cells[1].InnerText),
                        Submitted2023 = ParseInt(cells[4].InnerText),
                        Completed2023 = ParseInt(cells[7].InnerText),
                        Admitted2023 = ParseInt(cells[10].InnerText),
                        Confirmed2023 = ParseInt(cells[13].InnerText),
                        Registered2023 = ParseInt(cells[16].InnerText),
                        RegisteredInternational2023 = ParseInt(cells[19].InnerText),
                        RegisteredDomestic2023 = ParseInt(cells[22].InnerText),

                        // 2024 Data
                        Opportunities2024 = ParseInt(cells[2].InnerText),
                        Submitted2024 = ParseInt(cells[5].InnerText),
                        Completed2024 = ParseInt(cells[8].InnerText),
                        Admitted2024 = ParseInt(cells[11].InnerText),
                        Confirmed2024 = ParseInt(cells[14].InnerText),
                        Registered2024 = ParseInt(cells[17].InnerText),
                        RegisteredInternational2024 = ParseInt(cells[20].InnerText),
                        RegisteredDomestic2024 = ParseInt(cells[23].InnerText),

                        // 2025 Data
                        Opportunities2025 = ParseInt(cells[3].InnerText),
                        Submitted2025 = ParseInt(cells[6].InnerText),
                        Completed2025 = ParseInt(cells[9].InnerText),
                        Admitted2025 = ParseInt(cells[12].InnerText),
                        Confirmed2025 = ParseInt(cells[15].InnerText),
                        Registered2025 = ParseInt(cells[18].InnerText),
                        RegisteredInternational2025 = ParseInt(cells[21].InnerText),
                        RegisteredDomestic2025 = ParseInt(cells[24].InnerText)
                    };

                    if (!string.IsNullOrWhiteSpace(enrollment.ProgramName))
                    {
                        enrollmentData.Add(enrollment);
                        Console.WriteLine($"Added program: {enrollment.ProgramName} with {enrollment.Registered2023 + enrollment.Registered2024 + enrollment.Registered2025} total registered");
                    }
                }

                Console.WriteLine($"Total programs parsed for {department}: {enrollmentData.Count}");
            }
            catch (Exception ex)
            {
                // Log error appropriately
                Console.WriteLine($"Error fetching data from {url}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return enrollmentData;
        }

        private string CleanCellText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        private int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            // Remove any non-numeric characters except negative sign
            value = Regex.Replace(value, @"[^\d-]", "");

            return int.TryParse(value, out int result) ? result : 0;
        }

        public DashboardMetrics CalculateMetrics(List<EnrollmentData> data, DashboardFilters filters)
        {
            var filteredData = FilterData(data, filters);

            var metrics = new DashboardMetrics
            {
                YearlyBreakdown = new Dictionary<string, int>()
            };

            foreach (var enrollment in filteredData)
            {
                // Aggregate totals based on selected year
                if (filters.Year == "All" || filters.Year == "2023")
                {
                    metrics.TotalOpportunities += enrollment.Opportunities2023;
                    metrics.TotalSubmitted += enrollment.Submitted2023;
                    metrics.TotalCompleted += enrollment.Completed2023;
                    metrics.TotalAdmitted += enrollment.Admitted2023;
                    metrics.TotalConfirmed += enrollment.Confirmed2023;
                    metrics.TotalRegistered += enrollment.Registered2023;
                    metrics.TotalInternational += enrollment.RegisteredInternational2023;
                    metrics.TotalDomestic += enrollment.RegisteredDomestic2023;

                    if (!metrics.YearlyBreakdown.ContainsKey("2023"))
                        metrics.YearlyBreakdown["2023"] = 0;
                    metrics.YearlyBreakdown["2023"] += enrollment.Registered2023;
                }

                if (filters.Year == "All" || filters.Year == "2024")
                {
                    metrics.TotalOpportunities += enrollment.Opportunities2024;
                    metrics.TotalSubmitted += enrollment.Submitted2024;
                    metrics.TotalCompleted += enrollment.Completed2024;
                    metrics.TotalAdmitted += enrollment.Admitted2024;
                    metrics.TotalConfirmed += enrollment.Confirmed2024;
                    metrics.TotalRegistered += enrollment.Registered2024;
                    metrics.TotalInternational += enrollment.RegisteredInternational2024;
                    metrics.TotalDomestic += enrollment.RegisteredDomestic2024;

                    if (!metrics.YearlyBreakdown.ContainsKey("2024"))
                        metrics.YearlyBreakdown["2024"] = 0;
                    metrics.YearlyBreakdown["2024"] += enrollment.Registered2024;
                }

                if (filters.Year == "All" || filters.Year == "2025")
                {
                    metrics.TotalOpportunities += enrollment.Opportunities2025;
                    metrics.TotalSubmitted += enrollment.Submitted2025;
                    metrics.TotalCompleted += enrollment.Completed2025;
                    metrics.TotalAdmitted += enrollment.Admitted2025;
                    metrics.TotalConfirmed += enrollment.Confirmed2025;
                    metrics.TotalRegistered += enrollment.Registered2025;
                    metrics.TotalInternational += enrollment.RegisteredInternational2025;
                    metrics.TotalDomestic += enrollment.RegisteredDomestic2025;

                    if (!metrics.YearlyBreakdown.ContainsKey("2025"))
                        metrics.YearlyBreakdown["2025"] = 0;
                    metrics.YearlyBreakdown["2025"] += enrollment.Registered2025;
                }
            }

            metrics.OverallConversionRate = metrics.TotalOpportunities > 0
                ? (double)metrics.TotalRegistered / metrics.TotalOpportunities * 100
                : 0;

            metrics.InternationalPercentage = metrics.TotalRegistered > 0
                ? (double)metrics.TotalInternational / metrics.TotalRegistered * 100
                : 0;

            return metrics;
        }

        public List<ProgramSummary> GetTopPrograms(List<EnrollmentData> data, DashboardFilters filters)
        {
            var filteredData = FilterData(data, filters);
            var summaries = new List<ProgramSummary>();

            foreach (var enrollment in filteredData)
            {
                var summary = new ProgramSummary
                {
                    ProgramName = enrollment.ProgramName,
                    Department = enrollment.Department
                };

                // Calculate value based on metric type and year
                switch (filters.MetricType)
                {
                    case "Opportunities":
                        summary.Value = GetMetricByYear(enrollment, filters.Year, "Opportunities");
                        break;
                    case "Submitted":
                        summary.Value = GetMetricByYear(enrollment, filters.Year, "Submitted");
                        break;
                    case "Completed":
                        summary.Value = GetMetricByYear(enrollment, filters.Year, "Completed");
                        break;
                    case "Admitted":
                        summary.Value = GetMetricByYear(enrollment, filters.Year, "Admitted");
                        break;
                    case "Confirmed":
                        summary.Value = GetMetricByYear(enrollment, filters.Year, "Confirmed");
                        break;
                    default: // Registered
                        summary.Value = GetMetricByYear(enrollment, filters.Year, "Registered");
                        break;
                }

                // Calculate conversion rate
                var opportunities = GetMetricByYear(enrollment, filters.Year, "Opportunities");
                var registered = GetMetricByYear(enrollment, filters.Year, "Registered");
                summary.ConversionRate = opportunities > 0 ? (double)registered / opportunities * 100 : 0;

                // Calculate international percentage
                var international = GetMetricByYear(enrollment, filters.Year, "RegisteredInternational");
                summary.InternationalPercentage = registered > 0 ? (double)international / registered * 100 : 0;

                summaries.Add(summary);
            }

            // Sort and take top N
            IOrderedEnumerable<ProgramSummary> sorted;

            switch (filters.SortBy)
            {
                case "ConversionRate":
                    sorted = filters.SortOrder == "Desc"
                        ? summaries.OrderByDescending(s => s.ConversionRate)
                        : summaries.OrderBy(s => s.ConversionRate);
                    break;
                case "InternationalPercentage":
                    sorted = filters.SortOrder == "Desc"
                        ? summaries.OrderByDescending(s => s.InternationalPercentage)
                        : summaries.OrderBy(s => s.InternationalPercentage);
                    break;
                default:
                    sorted = filters.SortOrder == "Desc"
                        ? summaries.OrderByDescending(s => s.Value)
                        : summaries.OrderBy(s => s.Value);
                    break;
            }

            var topPrograms = sorted.Take(filters.TopN).ToList();

            // Calculate percentages
            var total = summaries.Sum(s => s.Value);
            foreach (var program in topPrograms)
            {
                program.Percentage = total > 0 ? (double)program.Value / total * 100 : 0;
            }

            return topPrograms;
        }

        private List<EnrollmentData> FilterData(List<EnrollmentData> data, DashboardFilters filters)
        {
            if (filters.Department != "All")
            {
                data = data.Where(d => d.Department == filters.Department).ToList();
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                data = data.Where(d => d.ProgramName.Contains(filters.SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Apply advanced filters based on year selection
            if (filters.MinOpportunities.HasValue || filters.MaxOpportunities.HasValue ||
                filters.MinRegistered.HasValue || filters.MaxRegistered.HasValue ||
                filters.MinConversionRate.HasValue || filters.MaxConversionRate.HasValue)
            {
                data = data.Where(d =>
                {
                    var opportunities = GetMetricByYear(d, filters.Year, "Opportunities");
                    var registered = GetMetricByYear(d, filters.Year, "Registered");
                    var conversionRate = opportunities > 0 ? (double)registered / opportunities * 100 : 0;

                    return (!filters.MinOpportunities.HasValue || opportunities >= filters.MinOpportunities.Value) &&
                           (!filters.MaxOpportunities.HasValue || opportunities <= filters.MaxOpportunities.Value) &&
                           (!filters.MinRegistered.HasValue || registered >= filters.MinRegistered.Value) &&
                           (!filters.MaxRegistered.HasValue || registered <= filters.MaxRegistered.Value) &&
                           (!filters.MinConversionRate.HasValue || conversionRate >= filters.MinConversionRate.Value) &&
                           (!filters.MaxConversionRate.HasValue || conversionRate <= filters.MaxConversionRate.Value);
                }).ToList();
            }

            return data;
        }

        private int GetMetricByYear(EnrollmentData enrollment, string year, string metricType)
        {
            int total = 0;

            if (year == "All" || year == "2023")
            {
                switch (metricType)
                {
                    case "Opportunities": total += enrollment.Opportunities2023; break;
                    case "Submitted": total += enrollment.Submitted2023; break;
                    case "Completed": total += enrollment.Completed2023; break;
                    case "Admitted": total += enrollment.Admitted2023; break;
                    case "Confirmed": total += enrollment.Confirmed2023; break;
                    case "Registered": total += enrollment.Registered2023; break;
                    case "RegisteredInternational": total += enrollment.RegisteredInternational2023; break;
                    case "RegisteredDomestic": total += enrollment.RegisteredDomestic2023; break;
                }
            }

            if (year == "All" || year == "2024")
            {
                switch (metricType)
                {
                    case "Opportunities": total += enrollment.Opportunities2024; break;
                    case "Submitted": total += enrollment.Submitted2024; break;
                    case "Completed": total += enrollment.Completed2024; break;
                    case "Admitted": total += enrollment.Admitted2024; break;
                    case "Confirmed": total += enrollment.Confirmed2024; break;
                    case "Registered": total += enrollment.Registered2024; break;
                    case "RegisteredInternational": total += enrollment.RegisteredInternational2024; break;
                    case "RegisteredDomestic": total += enrollment.RegisteredDomestic2024; break;
                }
            }

            if (year == "All" || year == "2025")
            {
                switch (metricType)
                {
                    case "Opportunities": total += enrollment.Opportunities2025; break;
                    case "Submitted": total += enrollment.Submitted2025; break;
                    case "Completed": total += enrollment.Completed2025; break;
                    case "Admitted": total += enrollment.Admitted2025; break;
                    case "Confirmed": total += enrollment.Confirmed2025; break;
                    case "Registered": total += enrollment.Registered2025; break;
                    case "RegisteredInternational": total += enrollment.RegisteredInternational2025; break;
                    case "RegisteredDomestic": total += enrollment.RegisteredDomestic2025; break;
                }
            }

            return total;
        }
    }
}