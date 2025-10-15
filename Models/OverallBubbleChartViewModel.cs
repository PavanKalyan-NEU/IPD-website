// Models/BubbleChartModels.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Northeastern_Personal_Workspace.Models
{
    public class OverallBubbleChartViewModel
    {
        public List<OverallBubbleChartProgram> Programs { get; set; } = new List<OverallBubbleChartProgram>();
        public int TotalProgramsCount => Programs.Count;
        public string DataSource { get; set; }
        public string LastUpdated { get; set; }

        // Additional aggregated properties
        public Dictionary<string, int> ProgramsByDomain { get; set; }
        public Dictionary<string, int> ProgramsByDegreeLevel { get; set; }
        public Dictionary<string, int> ProgramsByComplexity { get; set; }
        public Dictionary<string, decimal> AverageCoursesByDomain { get; set; }
        public Dictionary<string, decimal> AverageComplexityByLevel { get; set; }

        public void ProcessData()
        {
            ProgramsByDomain = Programs
                .GroupBy(p => p.AcademicDomain)
                .ToDictionary(g => g.Key, g => g.Count());

            ProgramsByDegreeLevel = Programs
                .GroupBy(p => p.DegreeLevel)
                .ToDictionary(g => g.Key, g => g.Count());

            ProgramsByComplexity = Programs
                .GroupBy(p => p.ComplexityCategory)
                .ToDictionary(g => g.Key, g => g.Count());

            

            AverageComplexityByLevel = Programs
                .GroupBy(p => p.DegreeLevel)
                .ToDictionary(g => g.Key, g => g.Average(p => p.ComplexityScore));
        }
    }

    public class OverallBubbleChartProgram
    {
        public int Id { get; set; }
        public string ProgramName { get; set; }
        public string DegreeLevel { get; set; }
        public string DegreeType { get; set; }
        public string AcademicDomain { get; set; }
        public int TotalCourses { get; set; }
        public int RequiredCourses { get; set; }
        public int ElectiveCourses { get; set; }
        public int CapstoneCourses { get; set; }
        public decimal ComplexityScore { get; set; }
        public int AdvancedTopicsCount { get; set; }
        public string AdvancedTopicsCoverage { get; set; }
        public string ProgramStrengths { get; set; }
        public bool IsPhd { get; set; }
        public bool IsCertificate { get; set; }
        public bool IsMinor { get; set; }
        public string ProgramUrl { get; set; }
        public string Department { get; set; }
        public int CreditHours { get; set; }
        public bool OnlineAvailable { get; set; }
        public bool StemDesignated { get; set; }
        public string ProgramDuration { get; set; }
        public decimal JobPlacementRate { get; set; }
        public decimal AverageSalary { get; set; }

        public bool IsGraduate => DegreeLevel.Equals("Graduate", StringComparison.OrdinalIgnoreCase);

        public string ComplexityCategory => ComplexityScore switch
        {
            < 2.0m => "Basic",
            >= 2.0m and < 3.0m => "Intermediate",
            >= 3.0m and < 4.0m => "Advanced",
            >= 4.0m => "Expert"
        };
    }
}