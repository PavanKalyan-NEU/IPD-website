using System.Collections.Generic;
using System.Linq;

namespace Northeastern_Personal_Workspace.Models
{
    public class VisualizationViewModel
    {
        public List<GraduateProgram> MastersPrograms { get; set; }
        public List<GraduateProgram> CertificatePrograms { get; set; }
        public List<GraduateProgram> PhdPrograms { get; set; } // Added PhD programs list

        public Dictionary<string, List<GraduateProgram>> MastersByComplexity { get; set; }
        public Dictionary<string, List<GraduateProgram>> CertificatesByComplexity { get; set; }
        public Dictionary<string, List<GraduateProgram>> PhdsByComplexity { get; set; } // Added PhD complexity grouping

        public List<string> ComplexityBins { get; set; }
        public int TotalProgramsCount { get; set; }

        // Added expected counts for validation
        public int ExpectedMastersPrograms { get; set; }
        public int ExpectedCertificatePrograms { get; set; }
        public int ExpectedPhdPrograms { get; set; }
        public int ExpectedTotalPrograms { get; set; }

        public VisualizationViewModel()
        {
            ComplexityBins = new List<string> { "1.0", "1.5", "2.0", "2.5", "3.0", "3.5" };
            MastersByComplexity = new Dictionary<string, List<GraduateProgram>>();
            CertificatesByComplexity = new Dictionary<string, List<GraduateProgram>>();
            PhdsByComplexity = new Dictionary<string, List<GraduateProgram>>(); // Initialize PhD complexity dictionary
            MastersPrograms = new List<GraduateProgram>();
            CertificatePrograms = new List<GraduateProgram>();
            PhdPrograms = new List<GraduateProgram>(); // Initialize PhD programs list
        }

        public void GroupProgramsByComplexity()
        {
            // Initialize all bins for all program types
            foreach (var bin in ComplexityBins)
            {
                MastersByComplexity[bin] = new List<GraduateProgram>();
                CertificatesByComplexity[bin] = new List<GraduateProgram>();
                PhdsByComplexity[bin] = new List<GraduateProgram>(); // Initialize PhD bins
            }

            // Group Masters programs by complexity
            foreach (var program in MastersPrograms)
            {
                var bin = GetComplexityBin(program.ComplexityScore);
                if (MastersByComplexity.ContainsKey(bin))
                {
                    MastersByComplexity[bin].Add(program);
                }
            }

            // Group Certificate programs by complexity
            foreach (var program in CertificatePrograms)
            {
                var bin = GetComplexityBin(program.ComplexityScore);
                if (CertificatesByComplexity.ContainsKey(bin))
                {
                    CertificatesByComplexity[bin].Add(program);
                }
            }

            // Group PhD programs by complexity
            foreach (var program in PhdPrograms)
            {
                var bin = GetComplexityBin(program.ComplexityScore);
                if (PhdsByComplexity.ContainsKey(bin))
                {
                    PhdsByComplexity[bin].Add(program);
                }
            }
        }

        private string GetComplexityBin(decimal complexityScore)
        {
            if (complexityScore < 1.5m) return "1.0";
            if (complexityScore < 2.0m) return "1.5";
            if (complexityScore < 2.5m) return "2.0";
            if (complexityScore < 3.0m) return "2.5";
            if (complexityScore < 3.5m) return "3.0";
            return "3.5";
        }

        // Helper properties for easy access to counts
        public int ActualMastersCount => MastersPrograms?.Count ?? 0;
        public int ActualCertificateCount => CertificatePrograms?.Count ?? 0;
        public int ActualPhdCount => PhdPrograms?.Count ?? 0;

        // Validation properties
        public bool HasExpectedMasters => ActualMastersCount == ExpectedMastersPrograms;
        public bool HasExpectedCertificates => ActualCertificateCount == ExpectedCertificatePrograms;
        public bool HasExpectedPhds => ActualPhdCount == ExpectedPhdPrograms;
        public bool HasExpectedTotal => TotalProgramsCount == ExpectedTotalPrograms;
    }

    // Additional model classes that might be referenced in your controller
 
}