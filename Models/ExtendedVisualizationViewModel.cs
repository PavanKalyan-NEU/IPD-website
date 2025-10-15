// Models/ExtendedVisualizationViewModel.cs
// This extends the existing VisualizationViewModel to include undergraduate and minor programs
using System;
using System.Collections.Generic;
using System.Linq;

namespace Northeastern_Personal_Workspace.Models
{
    public class ExtendedVisualizationViewModel : VisualizationViewModel
    {
        // Undergraduate and Minor program collections
        public List<UndergraduateProgram> BachelorPrograms { get; set; } = new List<UndergraduateProgram>();
        public List<MinorProgram> MinorPrograms { get; set; } = new List<MinorProgram>();

        // Combined statistics
        public int TotalAllProgramsCount => TotalProgramsCount + BachelorProgramsCount + MinorProgramsCount;
        public int BachelorProgramsCount => BachelorPrograms?.Count ?? 0;
        public int MinorProgramsCount => MinorPrograms?.Count ?? 0;

        // Cross-level program analysis
        public Dictionary<string, CrossLevelProgramStats> CrossLevelDomainStats { get; set; } = new Dictionary<string, CrossLevelProgramStats>();

        // Methods for cross-level analysis
        public void GroupAllProgramsByDomain()
        {
            GroupProgramsByComplexity(); // Call the existing method for graduate programs

            var allDomains = new HashSet<string>();

            // Collect all domains
            if (MastersPrograms != null) allDomains.UnionWith(MastersPrograms.Where(p => !string.IsNullOrEmpty(p.AcademicDomain)).Select(p => p.AcademicDomain));
            if (CertificatePrograms != null) allDomains.UnionWith(CertificatePrograms.Where(p => !string.IsNullOrEmpty(p.AcademicDomain)).Select(p => p.AcademicDomain));
            if (PhdPrograms != null) allDomains.UnionWith(PhdPrograms.Where(p => !string.IsNullOrEmpty(p.AcademicDomain)).Select(p => p.AcademicDomain));
            if (BachelorPrograms != null) allDomains.UnionWith(BachelorPrograms.Where(p => !string.IsNullOrEmpty(p.AcademicDomain)).Select(p => p.AcademicDomain));
            if (MinorPrograms != null) allDomains.UnionWith(MinorPrograms.Where(p => !string.IsNullOrEmpty(p.AcademicDomain)).Select(p => p.AcademicDomain));

            // Create cross-level statistics for each domain
            foreach (var domain in allDomains)
            {
                var stats = new CrossLevelProgramStats
                {
                    Domain = domain,
                    MastersCount = MastersPrograms?.Count(p => p.AcademicDomain == domain) ?? 0,
                    CertificateCount = CertificatePrograms?.Count(p => p.AcademicDomain == domain) ?? 0,
                    PhdCount = PhdPrograms?.Count(p => p.AcademicDomain == domain) ?? 0,
                    BachelorCount = BachelorPrograms?.Count(p => p.AcademicDomain == domain) ?? 0,
                    MinorCount = MinorPrograms?.Count(p => p.AcademicDomain == domain) ?? 0
                };

                CrossLevelDomainStats[domain] = stats;
            }
        }

        public List<DomainPathwayAnalysis> GetDomainPathwayAnalysis()
        {
            return CrossLevelDomainStats.Values
                .Where(stats => stats.TotalPrograms > 1) // Only domains with multiple program levels
                .Select(stats => new DomainPathwayAnalysis
                {
                    Domain = stats.Domain,
                    Stats = stats,
                    HasCompletePathway = stats.BachelorCount > 0 && stats.MastersCount > 0,
                    HasMinorOption = stats.MinorCount > 0,
                    HasAdvancedDegrees = stats.PhdCount > 0,
                    PathwayComplexity = CalculatePathwayComplexity(stats)
                })
                .OrderByDescending(p => p.PathwayComplexity)
                .ToList();
        }

        private string CalculatePathwayComplexity(CrossLevelProgramStats stats)
        {
            var levels = 0;
            if (stats.MinorCount > 0) levels++;
            if (stats.BachelorCount > 0) levels++;
            if (stats.CertificateCount > 0) levels++;
            if (stats.MastersCount > 0) levels++;
            if (stats.PhdCount > 0) levels++;

            return levels switch
            {
                5 => "Complete Pathway",
                4 => "Comprehensive",
                3 => "Well-Developed",
                2 => "Basic Pathway",
                _ => "Limited"
            };
        }

        public UniversityProgramSummary GetUniversityProgramSummary()
        {
            return new UniversityProgramSummary
            {
                TotalPrograms = TotalAllProgramsCount,
                GraduatePrograms = new GraduateProgramSummary
                {
                    Total = TotalProgramsCount,
                    Masters = MastersPrograms?.Count ?? 0,
                    Certificates = CertificatePrograms?.Count ?? 0,
                    Phd = PhdPrograms?.Count ?? 0
                },
                UndergraduatePrograms = new UndergraduateProgramSummary
                {
                    Total = BachelorProgramsCount,
                    STEM = BachelorPrograms?.Count(p => p.StemDesignation) ?? 0,
                    NonSTEM = BachelorPrograms?.Count(p => !p.StemDesignation) ?? 0,
                    WithCoop = BachelorPrograms?.Count(p => p.CoopAvailable) ?? 0,
                    WithInternships = BachelorPrograms?.Count(p => p.InternshipRequired) ?? 0
                },
                MinorPrograms = new MinorProgramSummary
                {
                    Total = MinorProgramsCount,
                    Online = MinorPrograms?.Count(p => p.OnlineAvailable) ?? 0,
                    SummerAvailable = MinorPrograms?.Count(p => p.CanCompleteInSummer) ?? 0
                },
                TopDomains = CrossLevelDomainStats.Values
                    .OrderByDescending(s => s.TotalPrograms)
                    .Take(5)
                    .Select(s => new DomainCoverage
                    {
                        Domain = s.Domain,
                        TotalPrograms = s.TotalPrograms,
                        LevelsCovered = s.GetLevelsCovered()
                    })
                    .ToList()
            };
        }
    }

    public class CrossLevelProgramStats
    {
        public string Domain { get; set; }
        public int MastersCount { get; set; }
        public int CertificateCount { get; set; }
        public int PhdCount { get; set; }
        public int BachelorCount { get; set; }
        public int MinorCount { get; set; }

        public int TotalPrograms => MastersCount + CertificateCount + PhdCount + BachelorCount + MinorCount;
        public int GraduateTotal => MastersCount + CertificateCount + PhdCount;
        public int UndergraduateTotal => BachelorCount + MinorCount;

        public List<string> GetLevelsCovered()
        {
            var levels = new List<string>();
            if (MinorCount > 0) levels.Add("Minor");
            if (BachelorCount > 0) levels.Add("Bachelor");
            if (CertificateCount > 0) levels.Add("Certificate");
            if (MastersCount > 0) levels.Add("Masters");
            if (PhdCount > 0) levels.Add("PhD");
            return levels;
        }

        public bool HasVerticalIntegration => GetLevelsCovered().Count >= 3;
    }

    public class DomainPathwayAnalysis
    {
        public string Domain { get; set; }
        public CrossLevelProgramStats Stats { get; set; }
        public bool HasCompletePathway { get; set; }
        public bool HasMinorOption { get; set; }
        public bool HasAdvancedDegrees { get; set; }
        public string PathwayComplexity { get; set; }

        public List<string> GetPossibleProgressions()
        {
            var progressions = new List<string>();

            if (Stats.MinorCount > 0 && Stats.BachelorCount > 0)
                progressions.Add("Minor → Bachelor");
            if (Stats.BachelorCount > 0 && Stats.MastersCount > 0)
                progressions.Add("Bachelor → Masters");
            if (Stats.MastersCount > 0 && Stats.PhdCount > 0)
                progressions.Add("Masters → PhD");
            if (Stats.BachelorCount > 0 && Stats.CertificateCount > 0)
                progressions.Add("Bachelor → Certificate");
            if (Stats.BachelorCount > 0 && Stats.PhdCount > 0)
                progressions.Add("Bachelor → PhD");

            return progressions;
        }
    }

    public class UniversityProgramSummary
    {
        public int TotalPrograms { get; set; }
        public GraduateProgramSummary GraduatePrograms { get; set; }
        public UndergraduateProgramSummary UndergraduatePrograms { get; set; }
        public MinorProgramSummary MinorPrograms { get; set; }
        public List<DomainCoverage> TopDomains { get; set; }

        public decimal GraduatePercentage => TotalPrograms > 0 ? (decimal)GraduatePrograms.Total / TotalPrograms * 100 : 0;
        public decimal UndergraduatePercentage => TotalPrograms > 0 ? (decimal)UndergraduatePrograms.Total / TotalPrograms * 100 : 0;
        public decimal MinorPercentage => TotalPrograms > 0 ? (decimal)MinorPrograms.Total / TotalPrograms * 100 : 0;
    }

    public class GraduateProgramSummary
    {
        public int Total { get; set; }
        public int Masters { get; set; }
        public int Certificates { get; set; }
        public int Phd { get; set; }

        public decimal MastersPercentage => Total > 0 ? (decimal)Masters / Total * 100 : 0;
        public decimal CertificatePercentage => Total > 0 ? (decimal)Certificates / Total * 100 : 0;
        public decimal PhdPercentage => Total > 0 ? (decimal)Phd / Total * 100 : 0;
    }

    public class UndergraduateProgramSummary
    {
        public int Total { get; set; }
        public int STEM { get; set; }
        public int NonSTEM { get; set; }
        public int WithCoop { get; set; }
        public int WithInternships { get; set; }

        public decimal STEMPercentage => Total > 0 ? (decimal)STEM / Total * 100 : 0;
        public decimal CoopPercentage => Total > 0 ? (decimal)WithCoop / Total * 100 : 0;
        public decimal InternshipPercentage => Total > 0 ? (decimal)WithInternships / Total * 100 : 0;
    }

    public class MinorProgramSummary
    {
        public int Total { get; set; }
        public int Online { get; set; }
        public int SummerAvailable { get; set; }

        public decimal OnlinePercentage => Total > 0 ? (decimal)Online / Total * 100 : 0;
        public decimal SummerPercentage => Total > 0 ? (decimal)SummerAvailable / Total * 100 : 0;
    }

    public class DomainCoverage
    {
        public string Domain { get; set; }
        public int TotalPrograms { get; set; }
        public List<string> LevelsCovered { get; set; }

        public string CoverageDescription => $"{Domain}: {TotalPrograms} programs across {string.Join(", ", LevelsCovered)}";
    }

    // Enhanced Program Comparison Models
    public class ProgramComparisonModel
    {
        public string ProgramName { get; set; }
        public string Level { get; set; } // "Bachelor", "Masters", "PhD", "Certificate", "Minor"
        public string Domain { get; set; }
        public int TotalCourses { get; set; }
        public decimal ComplexityScore { get; set; }
        public string BoxColor { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
    }

    public class ProgramSearchFilters
    {
        public List<string> Levels { get; set; } = new List<string>();
        public List<string> Domains { get; set; } = new List<string>();
        public int? MinCourses { get; set; }
        public int? MaxCourses { get; set; }
        public decimal? MinComplexity { get; set; }
        public decimal? MaxComplexity { get; set; }
        public bool? STEMOnly { get; set; }
        public bool? CoopAvailable { get; set; }
        public bool? OnlineAvailable { get; set; }
        public string SearchTerm { get; set; }
    }
}