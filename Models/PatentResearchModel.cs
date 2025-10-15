using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace USPTOQueryBuilder.Models
{
    // Main results container for the view
    public class TechnologyAnalysisResults
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = "";
        public DateTime AnalysisDate { get; set; } = DateTime.Now;
        public List<TechnologyDomainSummary> TechnologyDomains { get; set; } = new();
        public List<UniversityInfo> TopUniversities { get; set; } = new();
        public List<CompanyInfo> TopCompanies { get; set; } = new();
        public List<GeographicCluster> InnovationHubs { get; set; } = new();
        public List<ProgramOpportunity> ProgramOpportunities { get; set; } = new();
        public MarketInsights MarketInsights { get; set; } = new();
    }

    // Technology domain processing result
    public class TechnologyDomainResult
    {
        public string DomainName { get; set; } = "";
        public int TotalPatents { get; set; }
        public List<PatentRecord> Patents { get; set; } = new();
        public bool QuerySuccess { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    // Individual patent record from API
    public class PatentRecord
    {
        public string PatentId { get; set; } = "";
        public string PatentTitle { get; set; } = "";
        public string PatentAbstract { get; set; } = "";
        public DateTime PatentDate { get; set; }
        public List<AssigneeInfo> Assignees { get; set; } = new();
        public List<InventorInfo> Inventors { get; set; } = new();
    }

    // Assignee information from patent data
    public class AssigneeInfo
    {
        public string Organization { get; set; } = "";
        public string Type { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Country { get; set; } = "";
    }

    // Inventor information from patent data
    public class InventorInfo
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Country { get; set; } = "";
    }

    // Technology domain summary for display
    public class TechnologyDomainSummary
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int TotalPatents { get; set; }
        public double GrowthRate { get; set; }
        public string MarketPotential { get; set; } = "";
        public List<string> TopCompanies { get; set; } = new();
        public List<string> LeadingUniversities { get; set; } = new();
        public List<string> GeographicHotspots { get; set; } = new();
        public string SkillGapLevel { get; set; } = "";
    }

    // University analysis results
    public class UniversityInfo
    {
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public int PatentCount { get; set; }
        public double ResearchIntensity { get; set; }
        public string Type { get; set; } = "";
        public List<string> ResearchAreas { get; set; } = new();
    }

    // Company analysis results
    public class CompanyInfo
    {
        public string Name { get; set; } = "";
        public string Industry { get; set; } = "";
        public string Size { get; set; } = "";
        public int PatentCount { get; set; }
        public double InnovationRate { get; set; }
        public string FundingStatus { get; set; } = "";
        public List<string> TechFocus { get; set; } = new();
    }

    // Geographic cluster analysis
    public class GeographicCluster
    {
        public string Location { get; set; } = "";
        public string State { get; set; } = "";
        public string Country { get; set; } = "";
        public int PatentCount { get; set; }
        public List<string> TechFocus { get; set; } = new();
        public string EcosystemStrength { get; set; } = "";
        public double TalentDensity { get; set; }
    }

    // MS program opportunity assessment
    public class ProgramOpportunity
    {
        public string TechnologyArea { get; set; } = "";
        public string ProgramFocus { get; set; } = "";
        public string MarketDemand { get; set; } = "";
        public int EstimatedJobs { get; set; }
        public double SalaryPotential { get; set; }
        public string TimeToMarket { get; set; } = "";
        public List<string> CoreCourses { get; set; } = new();
        public List<string> IndustryPartners { get; set; } = new();
        public string RecommendedAction { get; set; } = "";
    }

    // Market insights summary
    public class MarketInsights
    {
        public string OverallTrend { get; set; } = "";
        public List<string> EmergingOpportunities { get; set; } = new();
        public List<string> InvestmentHotspots { get; set; } = new();
        public string RecommendedStrategy { get; set; } = "";
        public double ConfidenceScore { get; set; }
    }

    // PatentsView API response structure
    public class PatentsViewApiResponse
    {
        [JsonProperty("error")]
        public bool Error { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("total_hits")]
        public int TotalHits { get; set; }

        [JsonProperty("patents")]
        public dynamic[]? Patents { get; set; }
    }
}