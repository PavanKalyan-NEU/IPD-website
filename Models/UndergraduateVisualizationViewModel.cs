// Models/UndergraduateVisualizationViewModel.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace Northeastern_Personal_Workspace.Models
{
    public class UndergraduateVisualizationViewModel
    {
        public List<UndergraduateProgram> BachelorPrograms { get; set; } = new List<UndergraduateProgram>();
        public List<MinorProgram> MinorPrograms { get; set; } = new List<MinorProgram>();

        // NEW: Complexity Score Binning (matching graduate programs)
        public List<string> ComplexityBins { get; set; } = new List<string> { "1.0", "1.5", "2.0", "2.5", "3.0", "3.5" };
        public Dictionary<string, List<UndergraduateProgram>> BachelorsByComplexity { get; set; } = new Dictionary<string, List<UndergraduateProgram>>();
        public Dictionary<string, List<MinorProgram>> MinorsByComplexity { get; set; } = new Dictionary<string, List<MinorProgram>>();

        public int TotalProgramsCount { get; set; }
        public int BachelorProgramsCount => BachelorPrograms?.Count ?? 0;
        public int MinorProgramsCount => MinorPrograms?.Count ?? 0;

        // Bachelor program categorization
        public List<UndergraduateProgram> STEMPrograms => BachelorPrograms?.Where(p => p.StemDesignation).ToList() ?? new List<UndergraduateProgram>();
        public List<UndergraduateProgram> NonSTEMPrograms => BachelorPrograms?.Where(p => !p.StemDesignation).ToList() ?? new List<UndergraduateProgram>();

        // Program complexity groupings
        public List<UndergraduateProgram> HighComplexityPrograms => BachelorPrograms?.Where(p => p.ComplexityScore >= 4.0m).ToList() ?? new List<UndergraduateProgram>();
        public List<UndergraduateProgram> MediumComplexityPrograms => BachelorPrograms?.Where(p => p.ComplexityScore >= 2.0m && p.ComplexityScore < 4.0m).ToList() ?? new List<UndergraduateProgram>();
        public List<UndergraduateProgram> LowComplexityPrograms => BachelorPrograms?.Where(p => p.ComplexityScore < 2.0m).ToList() ?? new List<UndergraduateProgram>();

        // Domain groupings
        public Dictionary<string, List<UndergraduateProgram>> ProgramsByDomain { get; set; } = new Dictionary<string, List<UndergraduateProgram>>();
        public Dictionary<string, List<MinorProgram>> MinorsByDomain { get; set; } = new Dictionary<string, List<MinorProgram>>();

        // Size categorization (keeping for backward compatibility)
        public List<UndergraduateProgram> LargePrograms => BachelorPrograms?.Where(p => p.TotalCourses >= 40).ToList() ?? new List<UndergraduateProgram>();
        public List<UndergraduateProgram> MediumPrograms => BachelorPrograms?.Where(p => p.TotalCourses >= 32 && p.TotalCourses < 40).ToList() ?? new List<UndergraduateProgram>();
        public List<UndergraduateProgram> CompactPrograms => BachelorPrograms?.Where(p => p.TotalCourses < 32).ToList() ?? new List<UndergraduateProgram>();

        // Minor categorization
        public List<MinorProgram> SubstantialMinors => MinorPrograms?.Where(p => p.TotalCourses >= 8).ToList() ?? new List<MinorProgram>();
        public List<MinorProgram> StandardMinors => MinorPrograms?.Where(p => p.TotalCourses >= 6 && p.TotalCourses < 8).ToList() ?? new List<MinorProgram>();
        public List<MinorProgram> CompactMinors => MinorPrograms?.Where(p => p.TotalCourses < 6).ToList() ?? new List<MinorProgram>();

        // Statistics
        public decimal AverageCoursesPerBachelor => BachelorPrograms?.Count > 0 ? (decimal)BachelorPrograms.Average(p => p.TotalCourses) : 0;
        public decimal AverageCoursesPerMinor => MinorPrograms?.Count > 0 ? (decimal)MinorPrograms.Average(p => p.TotalCourses) : 0;
        public decimal AverageComplexityScore => BachelorPrograms?.Count > 0 ? BachelorPrograms.Average(p => p.ComplexityScore) : 0;

        // Co-op and Internship statistics
        public int ProgramsWithCoopCount => BachelorPrograms?.Count(p => p.CoopAvailable) ?? 0;
        public int ProgramsWithInternshipCount => BachelorPrograms?.Count(p => p.InternshipRequired) ?? 0;
        public int OnlineMinorsCount => MinorPrograms?.Count(p => p.OnlineAvailable) ?? 0;

        // Constructor
        public UndergraduateVisualizationViewModel()
        {
            InitializeComplexityBins();
        }

        // NEW: Initialize complexity bins
        private void InitializeComplexityBins()
        {
            foreach (var bin in ComplexityBins)
            {
                BachelorsByComplexity[bin] = new List<UndergraduateProgram>();
                MinorsByComplexity[bin] = new List<MinorProgram>();
            }
        }

        // NEW: Method to bin programs by complexity score
        public void BinProgramsByComplexity()
        {
            // Clear existing bins
            foreach (var bin in ComplexityBins)
            {
                BachelorsByComplexity[bin].Clear();
                MinorsByComplexity[bin].Clear();
            }

            // Bin bachelor programs
            if (BachelorPrograms != null)
            {
                foreach (var program in BachelorPrograms)
                {
                    string complexityBin = GetComplexityBin(program.ComplexityScore);
                    if (BachelorsByComplexity.ContainsKey(complexityBin))
                    {
                        BachelorsByComplexity[complexityBin].Add(program);
                    }
                }
            }

            // Bin minor programs - FIXED: Only if MinorProgram has ComplexityScore property
            if (MinorPrograms != null)
            {
                foreach (var program in MinorPrograms)
                {
                    // Use ComplexityScore if available, otherwise default to 1.0
                    decimal complexityScore = GetMinorComplexityScore(program);
                    string complexityBin = GetComplexityBin(complexityScore);
                    if (MinorsByComplexity.ContainsKey(complexityBin))
                    {
                        MinorsByComplexity[complexityBin].Add(program);
                    }
                }
            }
        }

        // Helper method to get complexity score from minor program
        private decimal GetMinorComplexityScore(MinorProgram program)
        {
            // Try to get ComplexityScore property using reflection if it exists
            var complexityProperty = typeof(MinorProgram).GetProperty("ComplexityScore");
            if (complexityProperty != null)
            {
                var value = complexityProperty.GetValue(program);
                if (value != null && decimal.TryParse(value.ToString(), out decimal score))
                {
                    return score;
                }
            }

            // Fallback: Calculate complexity based on course count and requirements
            return CalculateMinorComplexity(program);
        }

        // Fallback method to calculate minor complexity if property doesn't exist
        private decimal CalculateMinorComplexity(MinorProgram program)
        {
            // Simple complexity calculation based on course requirements
            decimal baseScore = 1.0m;

            // More courses = higher complexity
            if (program.TotalCourses >= 8) baseScore += 0.5m;
            if (program.TotalCourses >= 10) baseScore += 0.5m;

            // More required courses = higher complexity
            if (program.Required >= 4) baseScore += 0.3m;
            if (program.Required >= 6) baseScore += 0.2m;

            // Online availability might indicate lower complexity
            if (program.OnlineAvailable) baseScore -= 0.2m;

            // Summer completion might indicate lower complexity
            if (program.CanCompleteInSummer) baseScore -= 0.1m;

            return Math.Max(1.0m, Math.Min(4.0m, baseScore));
        }

        // NEW: Helper method to determine complexity bin (matching graduate logic)
        private string GetComplexityBin(decimal complexityScore)
        {
            if (complexityScore >= 3.5m) return "3.5";
            if (complexityScore >= 3.0m) return "3.0";
            if (complexityScore >= 2.5m) return "2.5";
            if (complexityScore >= 2.0m) return "2.0";
            if (complexityScore >= 1.5m) return "1.5";
            return "1.0";
        }

        // Methods to group data
        public void GroupProgramsByDomain()
        {
            if (BachelorPrograms != null)
            {
                ProgramsByDomain = BachelorPrograms
                    .Where(p => !string.IsNullOrEmpty(p.AcademicDomain))
                    .GroupBy(p => p.AcademicDomain)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }

            if (MinorPrograms != null)
            {
                MinorsByDomain = MinorPrograms
                    .Where(p => !string.IsNullOrEmpty(p.AcademicDomain))
                    .GroupBy(p => p.AcademicDomain)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
        }

        // NEW: Get complexity distribution for bins
        public Dictionary<string, int> GetComplexityBinDistribution()
        {
            var distribution = new Dictionary<string, int>();

            foreach (var bin in ComplexityBins)
            {
                var bachelorsInBin = BachelorsByComplexity[bin].Count;
                var minorsInBin = MinorsByComplexity[bin].Count;
                var binLabel = GetComplexityBinLabel(bin);
                distribution[binLabel] = bachelorsInBin + minorsInBin;
            }

            return distribution;
        }

        // NEW: Get complexity bin label for display
        private string GetComplexityBinLabel(string bin)
        {
            return bin switch
            {
                "1.0" => "1.0-1.49",
                "1.5" => "1.5-1.99",
                "2.0" => "2.0-2.49",
                "2.5" => "2.5-2.99",
                "3.0" => "3.0-3.49",
                "3.5" => "3.5-4.0",
                _ => bin
            };
        }

        public Dictionary<string, int> GetComplexityDistribution()
        {
            if (BachelorPrograms == null) return new Dictionary<string, int>();

            return new Dictionary<string, int>
            {
                ["High (4.0+)"] = HighComplexityPrograms.Count,
                ["Medium-High (3.0-3.9)"] = BachelorPrograms.Count(p => p.ComplexityScore >= 3.0m && p.ComplexityScore < 4.0m),
                ["Medium (2.0-2.9)"] = BachelorPrograms.Count(p => p.ComplexityScore >= 2.0m && p.ComplexityScore < 3.0m),
                ["Low-Medium (<2.0)"] = LowComplexityPrograms.Count
            };
        }

        public Dictionary<string, int> GetSizeDistribution()
        {
            if (BachelorPrograms == null) return new Dictionary<string, int>();

            return new Dictionary<string, int>
            {
                ["Large (40+ courses)"] = LargePrograms.Count,
                ["Medium (32-39 courses)"] = MediumPrograms.Count,
                ["Compact (<32 courses)"] = CompactPrograms.Count
            };
        }

        public Dictionary<string, int> GetMinorSizeDistribution()
        {
            if (MinorPrograms == null) return new Dictionary<string, int>();

            return new Dictionary<string, int>
            {
                ["Substantial (8+ courses)"] = SubstantialMinors.Count,
                ["Standard (6-7 courses)"] = StandardMinors.Count,
                ["Compact (<6 courses)"] = CompactMinors.Count
            };
        }

        public List<string> GetTopDomains(int count = 5)
        {
            return ProgramsByDomain
                .OrderByDescending(kvp => kvp.Value.Count)
                .Take(count)
                .Select(kvp => $"{kvp.Key} ({kvp.Value.Count})")
                .ToList();
        }

        public List<string> GetTopMinorDomains(int count = 5)
        {
            return MinorsByDomain
                .OrderByDescending(kvp => kvp.Value.Count)
                .Take(count)
                .Select(kvp => $"{kvp.Key} ({kvp.Value.Count})")
                .ToList();
        }

        // Summary statistics
        public ProgramSummaryStats GetSummaryStats()
        {
            return new ProgramSummaryStats
            {
                TotalBachelorPrograms = BachelorProgramsCount,
                TotalMinorPrograms = MinorProgramsCount,
                STEMProgramsCount = STEMPrograms.Count,
                NonSTEMProgramsCount = NonSTEMPrograms.Count,
                ProgramsWithCoop = ProgramsWithCoopCount,
                ProgramsWithInternships = ProgramsWithInternshipCount,
                OnlineMinors = OnlineMinorsCount,
                AverageCoursesPerBachelor = AverageCoursesPerBachelor,
                AverageCoursesPerMinor = AverageCoursesPerMinor,
                AverageComplexityScore = AverageComplexityScore,
                TopDomain = ProgramsByDomain.OrderByDescending(kvp => kvp.Value.Count).FirstOrDefault().Key,
                TopMinorDomain = MinorsByDomain.OrderByDescending(kvp => kvp.Value.Count).FirstOrDefault().Key
            };
        }

        // NEW: Method to validate complexity score data
        public ComplexityValidationReport ValidateComplexityScores()
        {
            var report = new ComplexityValidationReport();

            if (BachelorPrograms != null)
            {
                report.TotalBachelorPrograms = BachelorPrograms.Count;
                report.BachelorsWithValidComplexity = BachelorPrograms.Count(p => p.ComplexityScore > 0);
                report.BachelorsWithoutComplexity = BachelorPrograms.Count(p => p.ComplexityScore <= 0);
                report.MinComplexityScore = BachelorPrograms.Count > 0 ? BachelorPrograms.Min(p => p.ComplexityScore) : 0;
                report.MaxComplexityScore = BachelorPrograms.Count > 0 ? BachelorPrograms.Max(p => p.ComplexityScore) : 0;
            }

            if (MinorPrograms != null)
            {
                report.TotalMinorPrograms = MinorPrograms.Count;
                // Check if MinorProgram has ComplexityScore property
                var hasComplexityScore = typeof(MinorProgram).GetProperty("ComplexityScore") != null;
                report.MinorsWithValidComplexity = hasComplexityScore ? MinorPrograms.Count(p => GetMinorComplexityScore(p) > 0) : 0;
                report.MinorsWithoutComplexity = hasComplexityScore ? MinorPrograms.Count(p => GetMinorComplexityScore(p) <= 0) : MinorPrograms.Count;
            }

            return report;
        }
    }

    public class ProgramSummaryStats
    {
        public int TotalBachelorPrograms { get; set; }
        public int TotalMinorPrograms { get; set; }
        public int STEMProgramsCount { get; set; }
        public int NonSTEMProgramsCount { get; set; }
        public int ProgramsWithCoop { get; set; }
        public int ProgramsWithInternships { get; set; }
        public int OnlineMinors { get; set; }
        public decimal AverageCoursesPerBachelor { get; set; }
        public decimal AverageCoursesPerMinor { get; set; }
        public decimal AverageComplexityScore { get; set; }
        public string TopDomain { get; set; }
        public string TopMinorDomain { get; set; }

        public decimal STEMPercentage => TotalBachelorPrograms > 0 ? (decimal)STEMProgramsCount / TotalBachelorPrograms * 100 : 0;
        public decimal CoopPercentage => TotalBachelorPrograms > 0 ? (decimal)ProgramsWithCoop / TotalBachelorPrograms * 100 : 0;
        public decimal InternshipPercentage => TotalBachelorPrograms > 0 ? (decimal)ProgramsWithInternships / TotalBachelorPrograms * 100 : 0;
        public decimal OnlineMinorPercentage => TotalMinorPrograms > 0 ? (decimal)OnlineMinors / TotalMinorPrograms * 100 : 0;
    }

    // NEW: Complexity validation report class
    public class ComplexityValidationReport
    {
        public int TotalBachelorPrograms { get; set; }
        public int BachelorsWithValidComplexity { get; set; }
        public int BachelorsWithoutComplexity { get; set; }

        public int TotalMinorPrograms { get; set; }
        public int MinorsWithValidComplexity { get; set; }
        public int MinorsWithoutComplexity { get; set; }

        public decimal MinComplexityScore { get; set; }
        public decimal MaxComplexityScore { get; set; }

        public bool HasComplexityIssues => BachelorsWithoutComplexity > 0 || MinorsWithoutComplexity > 0;
        public decimal ComplexityDataQuality => TotalBachelorPrograms + TotalMinorPrograms > 0
            ? (decimal)(BachelorsWithValidComplexity + MinorsWithValidComplexity) / (TotalBachelorPrograms + TotalMinorPrograms) * 100
            : 0;
    }
}