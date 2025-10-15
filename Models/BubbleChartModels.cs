// Models/BubbleChartModels.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Northeastern_Personal_Workspace.Models
{
    public class BubbleChartProgram
    {
        public int Id { get; set; }

        [Display(Name = "Program Name")]
        public string ProgramName { get; set; } = "";

        [Display(Name = "Degree Level")]
        public string DegreeLevel { get; set; } = "";

        [Display(Name = "Degree Type")]
        public string DegreeType { get; set; } = "";

        [Display(Name = "Total AI/ML Courses")]
        public int TotalAiMlCourses { get; set; }

        [Display(Name = "Required Courses")]
        public int RequiredCourses { get; set; }

        [Display(Name = "Elective Courses")]
        public int ElectiveCourses { get; set; }

        [Display(Name = "Capstone Courses")]
        public int CapstoneCourses { get; set; }

        [Display(Name = "Complexity Score")]
        public decimal ComplexityScore { get; set; }

        [Display(Name = "Complexity Level")]
        public string ComplexityLevel { get; set; } = "";

        [Display(Name = "Advanced Topics Count")]
        public int AdvancedTopicsCount { get; set; }

        [Display(Name = "Advanced Topics Coverage")]
        public string AdvancedTopicsCoverage { get; set; } = "";

        [Display(Name = "Program Strengths")]
        public string ProgramStrengths { get; set; } = "";

        [Display(Name = "Academic Domain")]
        public string AcademicDomain { get; set; } = "";

        [Display(Name = "Y-Coordinate")]
        public decimal YCoordinate { get; set; }

        [Display(Name = "Color")]
        public string Color { get; set; } = "";

        // Calculated properties
        public int BubbleSize => CalculateBubbleSize();
        public bool IsPhd => DegreeType.Contains("PhD") || DegreeType.Contains("PharmD") ||
                            DegreeType.Contains("DMSc") || DegreeType.Contains("EdD") ||
                            DegreeType.Contains("Doctor");

        public bool IsCertificate => DegreeType.Contains("Certificate");

        public bool IsMinor => DegreeType.Contains("Minor");

        public bool IsUndergraduate => DegreeLevel.Equals("Undergraduate", StringComparison.OrdinalIgnoreCase);

        public bool IsGraduate => DegreeLevel.Equals("Graduate", StringComparison.OrdinalIgnoreCase);

        private int CalculateBubbleSize()
        {
            if (TotalAiMlCourses >= 20) return 60;
            if (TotalAiMlCourses >= 10) return 45;
            if (TotalAiMlCourses >= 5) return 30;
            if (TotalAiMlCourses >= 3) return 22;
            return 15;
        }

        public decimal CalculateYCoordinate()
        {
            decimal baseY = GetBaseYPosition();
            decimal domainJitter = GetDomainJitter();
            decimal randomJitter = GetRandomJitter();
            decimal scalingFactor = GetScalingFactor();

            YCoordinate = baseY + (domainJitter + randomJitter) * scalingFactor;
            return YCoordinate;
        }

        private decimal GetBaseYPosition()
        {
            if (IsGraduate)
            {
                if (IsPhd) return 9.0m; // PhD range: 8.0 to 10.0
                if (IsCertificate) return 1.25m; // Certificate range: 0.5 to 2.0
                return 5.0m; // Masters range: 3.0 to 7.0
            }
            else // Undergraduate
            {
                if (IsMinor) return -9.0m; // Minor range: -10.0 to -8.0
                return -3.75m; // Major range: -7.0 to -0.5
            }
        }

        private decimal GetDomainJitter()
        {
            return AcademicDomain?.ToLower() switch
            {
                "science" => 0.32m,
                "engineering" => 0.24m,
                "computerscience" => 0.16m,
                "professionalstudies" => 0.08m,
                "law" => 0m,
                "healthsciences" => -0.08m,
                "business" => -0.16m,
                "socialscience" => -0.24m,
                "artsmedia" => -0.32m,
                "education" => -0.40m,
                _ => 0m
            };
        }

        private decimal GetRandomJitter()
        {
            // Use program ID to create deterministic "random" jitter
            var hash = Math.Abs(ProgramName.GetHashCode()) % 19;
            return (hash / 10.0m) - 0.9m; // Range: -0.9 to 0.9
        }

        private decimal GetScalingFactor()
        {
            // Masters and Undergraduate Majors get 1.5x scaling
            if ((IsGraduate && !IsPhd && !IsCertificate) ||
                (IsUndergraduate && !IsMinor))
            {
                return 1.5m;
            }
            return 1.0m;
        }

        public void SetColor()
        {
            if (IsGraduate)
            {
                if (IsPhd)
                    Color = "#8B0000"; // Very dark red for doctoral programs
                else if (IsCertificate)
                    Color = "#DC143C"; // Medium red for certificate programs
                else
                    Color = "#B22222"; // Dark red for master's programs
            }
            else // Undergraduate
            {
                if (IsMinor)
                    Color = "#FFA07A"; // Very light red for undergraduate minors
                else
                    Color = "#FF6347"; // Light red for undergraduate majors
            }
        }

        public void SetComplexityLevel()
        {
            ComplexityLevel = ComplexityScore switch
            {
                >= 3.5m => "Expert",
                >= 2.5m => "Advanced",
                >= 1.5m => "Intermediate",
                _ => "Foundational"
            };
        }
    }

    public class BubbleChartViewModel
    {
        public List<BubbleChartProgram> Programs { get; set; } = new List<BubbleChartProgram>();
        public BubbleChartStatistics Statistics { get; set; } = new BubbleChartStatistics();
        public List<DomainGroup> DomainGroups { get; set; } = new List<DomainGroup>();
        public List<ComplexityGroup> ComplexityGroups { get; set; } = new List<ComplexityGroup>();

        public int TotalProgramsCount => Programs.Count;
        public string LastUpdated { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string DataSource { get; set; } = "Google Sheets";

        public void ProcessData()
        {
            CalculateStatistics();
            GroupByDomain();
            GroupByComplexity();
            SetCoordinatesAndColors();
        }

        private void CalculateStatibilities()
        {
            Statistics = new BubbleChartStatistics
            {
                TotalPrograms = Programs.Count,
                PhdPrograms = Programs.Count(p => p.IsPhd),
                MastersPrograms = Programs.Count(p => p.IsGraduate && !p.IsPhd && !p.IsCertificate),
                CertificatePrograms = Programs.Count(p => p.IsCertificate),
                UndergraduatePrograms = Programs.Count(p => p.IsUndergraduate && !p.IsMinor),
                MinorPrograms = Programs.Count(p => p.IsMinor),
                AverageComplexityScore = Programs.Any() ? Programs.Average(p => p.ComplexityScore) : 0,
                AverageCourseCount = Programs.Any() ? (decimal)Programs.Average(p => p.TotalAiMlCourses) : 0,
                MaxCourseCount = Programs.Any() ? Programs.Max(p => p.TotalAiMlCourses) : 0,
                MinCourseCount = Programs.Any() ? Programs.Min(p => p.TotalAiMlCourses) : 0
            };
        }

        private void CalculateStatistics()
        {
            Statistics = new BubbleChartStatistics
            {
                TotalPrograms = Programs.Count,
                PhdPrograms = Programs.Count(p => p.IsPhd),
                MastersPrograms = Programs.Count(p => p.IsGraduate && !p.IsPhd && !p.IsCertificate),
                CertificatePrograms = Programs.Count(p => p.IsCertificate),
                UndergraduatePrograms = Programs.Count(p => p.IsUndergraduate && !p.IsMinor),
                MinorPrograms = Programs.Count(p => p.IsMinor),
                AverageComplexityScore = Programs.Any() ? Programs.Average(p => p.ComplexityScore) : 0,
                AverageCourseCount = Programs.Any() ? (decimal)Programs.Average(p => p.TotalAiMlCourses) : 0,
                MaxCourseCount = Programs.Any() ? Programs.Max(p => p.TotalAiMlCourses) : 0,
                MinCourseCount = Programs.Any() ? Programs.Min(p => p.TotalAiMlCourses) : 0
            };
        }

        private void GroupByDomain()
        {
            DomainGroups = Programs
                .GroupBy(p => p.AcademicDomain)
                .Select(g => new DomainGroup
                {
                    DomainName = g.Key,
                    ProgramCount = g.Count(),
                    Programs = g.ToList(),
                    AverageCourses = g.Average(p => p.TotalAiMlCourses),
                    //AverageComplexity = g.Average(p => p.ComplexityScore)
                })
                .OrderByDescending(g => g.ProgramCount)
                .ToList();
        }

        private void GroupByComplexity()
        {
            ComplexityGroups = Programs
                .GroupBy(p => p.ComplexityLevel)
                .Select(g => new ComplexityGroup
                {
                    ComplexityLevel = g.Key,
                    ProgramCount = g.Count(),
                    Programs = g.ToList(),
                    AverageCourses = g.Average(p => p.TotalAiMlCourses),
                    ScoreRange = $"{g.Min(p => p.ComplexityScore):F1} - {g.Max(p => p.ComplexityScore):F1}"
                })
                .OrderBy(g => g.ComplexityLevel == "Foundational" ? 1 :
                            g.ComplexityLevel == "Intermediate" ? 2 :
                            g.ComplexityLevel == "Advanced" ? 3 : 4)
                .ToList();
        }

        private void SetCoordinatesAndColors()
        {
            foreach (var program in Programs)
            {
                program.CalculateYCoordinate();
                program.SetColor();
                program.SetComplexityLevel();
            }
        }
    }

    public class BubbleChartStatistics
    {
        public int TotalPrograms { get; set; }
        public int PhdPrograms { get; set; }
        public int MastersPrograms { get; set; }
        public int CertificatePrograms { get; set; }
        public int UndergraduatePrograms { get; set; }
        public int MinorPrograms { get; set; }
        public decimal AverageComplexityScore { get; set; }
        public decimal AverageCourseCount { get; set; }
        public int MaxCourseCount { get; set; }
        public int MinCourseCount { get; set; }

        public string FormattedAverageComplexity => AverageComplexityScore.ToString("F2");
        public string FormattedAverageCourses => AverageCourseCount.ToString("F1");
    }

    public class DomainGroup
    {
        public string DomainName { get; set; } = "";
        public int ProgramCount { get; set; }
        public List<BubbleChartProgram> Programs { get; set; } = new List<BubbleChartProgram>();
        public double AverageCourses { get; set; }
        public double AverageComplexity { get; set; }

        public string FormattedAverageCourses => AverageCourses.ToString("F1");
        public string FormattedAverageComplexity => AverageComplexity.ToString("F2");
    }

    public class ComplexityGroup
    {
        public string ComplexityLevel { get; set; } = "";
        public int ProgramCount { get; set; }
        public List<BubbleChartProgram> Programs { get; set; } = new List<BubbleChartProgram>();
        public double AverageCourses { get; set; }
        public string ScoreRange { get; set; } = "";

        public string FormattedAverageCourses => AverageCourses.ToString("F1");
    }

    // Filter classes for API endpoints
    public class BubbleChartFilter
    {
        public string DegreeLevel { get; set; } = "all";
        public string DegreeType { get; set; } = "all";
        public string AcademicDomain { get; set; } = "all";
        public int MinCourses { get; set; } = 1;
        public int MaxCourses { get; set; } = 100;
        public decimal MinComplexity { get; set; } = 0;
        public decimal MaxComplexity { get; set; } = 5;
    }

    public class BubbleChartApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public BubbleChartViewModel Data { get; set; } = new BubbleChartViewModel();
        public string LastUpdated { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string DataSource { get; set; } = "Google Sheets";
        public int TotalRecords { get; set; }
        public int FilteredRecords { get; set; }
    }
}