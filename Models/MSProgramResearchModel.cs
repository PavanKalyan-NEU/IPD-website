using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace USPTOQueryBuilder.Models.MSProgram
{
    // Main results container - uses different namespace to avoid conflicts
    public class MSProgramAnalysisResults
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = "";
        public DateTime AnalysisDate { get; set; } = DateTime.Now;
        public List<DomainSummary> TechnologyDomains { get; set; } = new();
        public List<ResearchUniversity> TopUniversities { get; set; } = new();
        public List<InnovationCompany> TopCompanies { get; set; } = new();
        public List<InnovationHub> GeographicHubs { get; set; } = new();
        public List<MSOpportunity> ProgramOpportunities { get; set; } = new();
        public ResearchInsights MarketInsights { get; set; } = new();
    }

    // Technology domain summary
    public class DomainSummary
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

    // University research analysis
    public class ResearchUniversity
    {
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public int PatentCount { get; set; }
        public double ResearchIntensity { get; set; }
        public string Type { get; set; } = "";
        public List<string> ResearchAreas { get; set; } = new();
    }

    // Company innovation analysis
    public class InnovationCompany
    {
        public string Name { get; set; } = "";
        public string Industry { get; set; } = "";
        public string Size { get; set; } = "";
        public int PatentCount { get; set; }
        public double InnovationRate { get; set; }
        public string FundingStatus { get; set; } = "";
        public List<string> TechFocus { get; set; } = new();
    }

    // Geographic innovation hub
    public class InnovationHub
    {
        public string Location { get; set; } = "";
        public string State { get; set; } = "";
        public string Country { get; set; } = "";
        public int PatentCount { get; set; }
        public List<string> TechFocus { get; set; } = new();
        public string EcosystemStrength { get; set; } = "";
        public double TalentDensity { get; set; }
    }

    // MS program opportunity
    public class MSOpportunity
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

    // Market insights
    public class ResearchInsights
    {
        public string OverallTrend { get; set; } = "";
        public List<string> EmergingOpportunities { get; set; } = new();
        public List<string> InvestmentHotspots { get; set; } = new();
        public string RecommendedStrategy { get; set; } = "";
        public double ConfidenceScore { get; set; }
    }

    // Internal data processing models
    public class PatentQueryResult
    {
        public string DomainName { get; set; } = "";
        public int TotalPatents { get; set; }
        public List<PatentData> Patents { get; set; } = new();
        public bool QuerySuccess { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    public class PatentData
    {
        public string PatentId { get; set; } = "";
        public string PatentTitle { get; set; } = "";
        public string PatentAbstract { get; set; } = "";
        public DateTime PatentDate { get; set; }
        public List<PatentAssignee> Assignees { get; set; } = new();
        public List<PatentInventor> Inventors { get; set; } = new();
    }

    public class PatentAssignee
    {
        public string Organization { get; set; } = "";
        public string Type { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Country { get; set; } = "";
    }

    public class PatentInventor
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Country { get; set; } = "";
    }

    // USPTO API response model
    public class USPTOApiResponse
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