// Models/UndergraduateProgram.cs
using System;

namespace Northeastern_Personal_Workspace.Models
{
    public class UndergraduateProgram
    {
        // Primary identifier and basic properties
        public int Id { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public string DegreeType { get; set; } = "Bachelor";

        // Course and credit information
        public int TotalCourses { get; set; }
        public int Required { get; set; }
        public int Elective { get; set; }
        public int Capstone { get; set; } = 0;
        public int CreditHours { get; set; } = 0; // Total credit hours for the program
        public int Level4Topics { get; set; }

        // NEW: Complexity Score from Column H
        public decimal ComplexityScore { get; set; } = 1.0m;

        // Academic and classification properties
        public string AcademicDomain { get; set; } = string.Empty;
        public bool StemDesignation { get; set; } = false;

        // Program features and requirements
        public bool CoopAvailable { get; set; } = false;
        public bool InternshipRequired { get; set; } = false;
        public string Prerequisites { get; set; } = string.Empty;
        public string CareerPaths { get; set; } = string.Empty;

        // Visualization properties
        public int BoxHeight { get; set; } = 60;
        public int BoxWidth { get; set; } = 180;
        public string BoxColor { get; set; } = "#4CAF50";

        // Calculated properties for categorization
        public bool IsLarge => TotalCourses >= 40;
        public bool IsMedium => TotalCourses >= 32 && TotalCourses < 40;
        public bool IsCompact => TotalCourses < 32;

        public bool IsHighComplexity => ComplexityScore >= 3.0m;
        public bool IsMediumComplexity => ComplexityScore >= 2.0m && ComplexityScore < 3.0m;
        public bool IsLowComplexity => ComplexityScore < 2.0m;


        // Constructor
        public UndergraduateProgram()
        {
            UpdateVisualizationProperties();
        }

        // Method to update visualization properties based on program characteristics
        public void UpdateVisualizationProperties()
        {
            // Set box height based on total courses
            if (TotalCourses >= 45)
                BoxHeight = 90;
            else if (TotalCourses >= 40)
                BoxHeight = 80;
            else if (TotalCourses >= 35)
                BoxHeight = 70;
            else if (TotalCourses >= 30)
                BoxHeight = 60;
            else
                BoxHeight = 50;

            // Set box width based on program complexity or course count
            if (ComplexityScore >= 3.5m || TotalCourses >= 45)
                BoxWidth = 200;
            else if (ComplexityScore >= 3.0m || TotalCourses >= 40)
                BoxWidth = 190;
            else if (ComplexityScore >= 2.5m || TotalCourses >= 35)
                BoxWidth = 185;
            else
                BoxWidth = 180;

            // Set color based on STEM designation
            if (StemDesignation)
                BoxColor = "#2E7D32"; // Dark green for STEM
            else
                BoxColor = "#4CAF50"; // Light green for non-STEM
        }

        // Method to calculate complexity score if not provided from data
        public void CalculateComplexityScore()
        {
            if (ComplexityScore <= 0)
            {
                decimal baseScore = 1.0m;

                // Course count factor
                if (TotalCourses >= 45) baseScore += 1.0m;
                else if (TotalCourses >= 40) baseScore += 0.8m;
                else if (TotalCourses >= 35) baseScore += 0.6m;
                else if (TotalCourses >= 32) baseScore += 0.4m;

                // Required courses factor
                if (Required >= 30) baseScore += 0.5m;
                else if (Required >= 25) baseScore += 0.3m;
                else if (Required >= 20) baseScore += 0.2m;

                // STEM programs tend to be more complex
                if (StemDesignation) baseScore += 0.3m;

                // Capstone adds complexity
                if (Capstone > 0) baseScore += 0.2m;

                // Internship requirement adds complexity
                if (InternshipRequired) baseScore += 0.1m;

                // Co-op programs might be slightly more complex due to coordination
                if (CoopAvailable) baseScore += 0.1m;

                // Ensure score is within valid range (1.0 - 4.0)
                ComplexityScore = Math.Max(1.0m, Math.Min(4.0m, baseScore));
            }
        }

        // Method to get complexity category as string
        public string GetComplexityCategory()
        {
            if (ComplexityScore >= 3.5m) return "Very High";
            if (ComplexityScore >= 3.0m) return "High";
            if (ComplexityScore >= 2.5m) return "Medium-High";
            if (ComplexityScore >= 2.0m) return "Medium";
            if (ComplexityScore >= 1.5m) return "Medium-Low";
            return "Low";
        }

        // Method to get size category as string
        public string GetSizeCategory()
        {
            if (TotalCourses >= 40) return "Large";
            if (TotalCourses >= 32) return "Medium";
            return "Compact";
        }

        public override string ToString()
        {
            return $"{ProgramName} (ID: {Id}, {DegreeType}, {TotalCourses} courses, {CreditHours} credit hours, " +
                   $"Complexity: {ComplexityScore:F1}, STEM: {StemDesignation})";
        }
    }
}