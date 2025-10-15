using System;
using System.Collections.Generic;

namespace USPTOQueryBuilder.Models
{
    // View Models
    public class StrategicDashboardViewModel
    {
        public int StartYear { get; set; }
        public int EndYear { get; set; }
        public string SelectedTimeframe { get; set; }
    }

    // Request Models
    public class MarketAnalysisRequest
    {
        public int StartYear { get; set; }
        public int EndYear { get; set; }
        public List<string> TechnologyAreas { get; set; }
        public string AnalysisType { get; set; }
    }

    public class SkillsAnalysisRequest
    {
        public int StartYear { get; set; }
        public int EndYear { get; set; }
        public string FocusArea { get; set; }
    }

    public class EmergingTechRequest
    {
        public int TimeHorizon { get; set; } // Years forward
        public double MinimumGrowthRate { get; set; }
        public int MinimumPatentCount { get; set; }
    }

    // Response Models
    public class MarketDynamicsResponse
    {
        public List<TimeSeriesDataPoint> TimeSeriesData { get; set; }
        public List<GrowthAreaMetrics> TopGrowthAreas { get; set; }
        public List<CompanyPatentActivity> CompanyLeaderboard { get; set; }
        public List<GeographicActivity> GeographicHeatmap { get; set; }
    }

    public class SkillsClustersResponse
    {
        public List<SkillCluster> SkillClusters { get; set; }
        public List<CrossDisciplinaryMetric> CrossDisciplinaryAreas { get; set; }
    }

    public class EmergingTechnologiesResponse
    {
        public List<EmergingTechnology> EmergingAreas { get; set; }
        public List<GrowthPrediction> PredictedGrowth { get; set; }
    }

    // Data Models
    public class TimeSeriesDataPoint
    {
        public int Year { get; set; }
        public int TotalPatents { get; set; }
        public Dictionary<string, int> Categories { get; set; }
    }

    public class GrowthAreaMetrics
    {
        public string TechnologyArea { get; set; }
        public int TotalPatents { get; set; }
        public double GrowthRate { get; set; }
        public List<int> YearlyTrend { get; set; }
        public List<string> TopKeywords { get; set; }
        public double MarketPotentialScore { get; set; }
    }

    public class CompanyPatentActivity
    {
        public string CompanyName { get; set; }
        public int TotalPatents { get; set; }
        public List<string> FocusAreas { get; set; }
        public double InnovationIndex { get; set; }
        public List<int> YearlyPatentCounts { get; set; }
        public bool IsUniversity { get; set; }
    }

    public class InventorActivity
    {
        public string InventorName { get; set; }
        public int PatentCount { get; set; }
        public string Location { get; set; }
        public string PrimaryAssignee { get; set; }
    }

    public class GeographicActivity
    {
        public string Country { get; set; }
        public string State { get; set; }
        public int PatentCount { get; set; }
        public double GrowthRate { get; set; }
        public List<string> DominantTechnologies { get; set; }
    }

    public class SkillCluster
    {
        public string ClusterName { get; set; }
        public List<string> CoreSkills { get; set; }
        public List<string> EmergingSkills { get; set; }
        public int DemandScore { get; set; }
        public double ProjectedGrowth { get; set; }
        public List<string> RelatedPatentClasses { get; set; }
    }

    public class CrossDisciplinaryMetric
    {
        public string PrimaryField { get; set; }
        public string SecondaryField { get; set; }
        public int IntersectionCount { get; set; }
        public double GrowthRate { get; set; }
        public List<string> ExampleApplications { get; set; }
    }

    public class EmergingTechnology
    {
        public string TechnologyName { get; set; }
        public string Description { get; set; }
        public int FirstAppearanceYear { get; set; }
        public int CurrentPatentCount { get; set; }
        public double GrowthRate { get; set; }
        public List<string> KeyPlayers { get; set; }
        public List<string> ApplicationAreas { get; set; }
    }

    public class GrowthPrediction
    {
        public string TechnologyArea { get; set; }
        public int PredictionYear { get; set; }
        public int EstimatedPatentCount { get; set; }
        public double ConfidenceLevel { get; set; }
        public string MarketReadiness { get; set; } // "Early", "Growing", "Mature"
    }

    // Supporting Models
    public class YearlyPatentData
    {
        public int Year { get; set; }
        public int TotalPatents { get; set; }
        public Dictionary<string, int> Categories { get; set; }
    }

    public class TechnologyTrend
    {
        public string Technology { get; set; }
        public List<YearlyDataPoint> YearlyData { get; set; }
        public double CompoundAnnualGrowthRate { get; set; }
        public string TrendDirection { get; set; } // "Emerging", "Growing", "Stable", "Declining"
    }

    public class YearlyDataPoint
    {
        public int Year { get; set; }
        public int PatentCount { get; set; }
        public double YearOverYearGrowth { get; set; }
    }

    public class IndustryInsight
    {
        public string Industry { get; set; }
        public string Insight { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> SupportingPatents { get; set; }
        public DateTime GeneratedDate { get; set; }
    }

    // Technology Filter Models
    public class TechnologyFilterRequest
    {
        public List<string> SelectedCategories { get; set; }
        public int StartYear { get; set; }
        public int EndYear { get; set; }
    }

    public class FilteredTechnologyResponse
    {
        public List<PatentDataPoint> PatentData { get; set; }
        public int TotalPatents { get; set; }
        public Dictionary<string, double> GrowthMetrics { get; set; }
        public List<InnovatorInfo> TopInnovators { get; set; }
        public Dictionary<int, int> YearlyTrends { get; set; }
    }

    public class PatentDataPoint
    {
        public string PatentId { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public string Assignee { get; set; }
    }

    public class InnovatorInfo
    {
        public string Name { get; set; }
        public int PatentCount { get; set; }
        public string Type { get; set; } // University, Company, Other
    }
}