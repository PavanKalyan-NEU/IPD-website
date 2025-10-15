// Models/EnrollmentModels.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrollmentDashboard.Models
{
    public class EnrollmentData
    {
        public string ProgramName { get; set; }
        public string Department { get; set; }

        // 2023 Data
        public int Opportunities2023 { get; set; }
        public int Submitted2023 { get; set; }
        public int Completed2023 { get; set; }
        public int Admitted2023 { get; set; }
        public int Confirmed2023 { get; set; }
        public int Registered2023 { get; set; }
        public int RegisteredInternational2023 { get; set; }
        public int RegisteredDomestic2023 { get; set; }

        // 2024 Data
        public int Opportunities2024 { get; set; }
        public int Submitted2024 { get; set; }
        public int Completed2024 { get; set; }
        public int Admitted2024 { get; set; }
        public int Confirmed2024 { get; set; }
        public int Registered2024 { get; set; }
        public int RegisteredInternational2024 { get; set; }
        public int RegisteredDomestic2024 { get; set; }

        // 2025 Data
        public int Opportunities2025 { get; set; }
        public int Submitted2025 { get; set; }
        public int Completed2025 { get; set; }
        public int Admitted2025 { get; set; }
        public int Confirmed2025 { get; set; }
        public int Registered2025 { get; set; }
        public int RegisteredInternational2025 { get; set; }
        public int RegisteredDomestic2025 { get; set; }

        // Calculated Properties
        public int TotalOpportunities => Opportunities2023 + Opportunities2024 + Opportunities2025;
        public int TotalRegistered => Registered2023 + Registered2024 + Registered2025;
        public double ConversionRate2023 => Opportunities2023 > 0 ? (double)Registered2023 / Opportunities2023 * 100 : 0;
        public double ConversionRate2024 => Opportunities2024 > 0 ? (double)Registered2024 / Opportunities2024 * 100 : 0;
        public double ConversionRate2025 => Opportunities2025 > 0 ? (double)Registered2025 / Opportunities2025 * 100 : 0;
        public double InternationalPercentage2023 => Registered2023 > 0 ? (double)RegisteredInternational2023 / Registered2023 * 100 : 0;
        public double InternationalPercentage2024 => Registered2024 > 0 ? (double)RegisteredInternational2024 / Registered2024 * 100 : 0;
        public double InternationalPercentage2025 => Registered2025 > 0 ? (double)RegisteredInternational2025 / Registered2025 * 100 : 0;
    }

    public class DashboardViewModel
    {
        public List<EnrollmentData> AllData { get; set; }
        public List<EnrollmentData> COEData { get; set; }
        public List<EnrollmentData> COSData { get; set; }
        public DashboardFilters Filters { get; set; }
        public DashboardMetrics Metrics { get; set; }
        public List<ProgramSummary> TopPrograms { get; set; }
    }

    public class DashboardFilters
    {
        public string Department { get; set; } = "All";
        public string Year { get; set; } = "All";
        public string SortBy { get; set; } = "TotalRegistered";
        public string SortOrder { get; set; } = "Desc";
        public int TopN { get; set; } = 10;
        public bool ShowInternationalBreakdown { get; set; } = true;
        public string MetricType { get; set; } = "Registered";

        // New filter options
        public int? MinOpportunities { get; set; }
        public int? MaxOpportunities { get; set; }
        public int? MinRegistered { get; set; }
        public int? MaxRegistered { get; set; }
        public double? MinConversionRate { get; set; }
        public double? MaxConversionRate { get; set; }
        public string SearchTerm { get; set; } = "";
        public List<string> SelectedPrograms { get; set; } = new List<string>();
    }

    public class DashboardMetrics
    {
        public int TotalOpportunities { get; set; }
        public int TotalSubmitted { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalAdmitted { get; set; }
        public int TotalConfirmed { get; set; }
        public int TotalRegistered { get; set; }
        public int TotalInternational { get; set; }
        public int TotalDomestic { get; set; }
        public double OverallConversionRate { get; set; }
        public double InternationalPercentage { get; set; }
        public Dictionary<string, int> YearlyBreakdown { get; set; }
    }

    public class ProgramSummary
    {
        public string ProgramName { get; set; }
        public string Department { get; set; }
        public int Value { get; set; }
        public double Percentage { get; set; }
        public double ConversionRate { get; set; }
        public double InternationalPercentage { get; set; }
    }
}