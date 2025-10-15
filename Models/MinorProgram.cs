// Models/MinorProgram.cs
using System;

namespace Northeastern_Personal_Workspace.Models
{
    public class MinorProgram
    {
        // ADDED: Properties that your controller expects
        public int Id { get; set; }
        public int BoxWidth { get; set; } = 150; // Default width for minor cards
        public int CreditHours { get; set; } = 0; // Credit hours for the program

        public string ProgramName { get; set; } = string.Empty;
        public string DegreeType { get; set; } = "Minor";
        public int TotalCourses { get; set; }
        public int Required { get; set; }
        public int Elective { get; set; }
        public int Capstone { get; set; } = 0; // Minors typically don't have capstones
        public int Level4Topics { get; set; }

        // NEW: Add ComplexityScore property
        public decimal ComplexityScore { get; set; } = 1.0m;

        public string AcademicDomain { get; set; } = string.Empty;
        public string Prerequisites { get; set; } = string.Empty;

        // Minor-specific properties
        public bool OnlineAvailable { get; set; } = false;
        public bool CanCompleteInSummer { get; set; } = false;
        public string ComplementaryMajors { get; set; } = string.Empty;
        public string CareerEnhancement { get; set; } = string.Empty;

        // Visualization properties
        public int BoxHeight { get; set; } = 60;
        public string BoxColor { get; set; } = "#FF9800";

        // Calculated properties
        public bool IsSubstantial => TotalCourses >= 8;
        public bool IsStandard => TotalCourses >= 6 && TotalCourses < 8;
        public bool IsCompact => TotalCourses < 6;

        // Constructor
        public MinorProgram()
        {
            // Set default box height based on course count (will be overridden later)
            UpdateVisualizationProperties();
        }

        public void UpdateVisualizationProperties()
        {
            // Set box height based on total courses (similar to other programs)
            if (TotalCourses >= 10)
                BoxHeight = 80;
            else if (TotalCourses >= 8)
                BoxHeight = 70;
            else if (TotalCourses >= 6)
                BoxHeight = 60;
            else
                BoxHeight = 50;

            // Set box width based on course count or use default
            if (TotalCourses >= 10)
                BoxWidth = 170;
            else if (TotalCourses >= 8)
                BoxWidth = 160;
            else
                BoxWidth = 150;

            // Set color based on online availability
            BoxColor = OnlineAvailable ? "#F57C00" : "#FF9800";
        }

        // Method to calculate complexity score if not provided
        public void CalculateComplexityScore()
        {
            if (ComplexityScore <= 0)
            {
                decimal baseScore = 1.0m;

                // More courses = higher complexity
                if (TotalCourses >= 8) baseScore += 0.5m;
                if (TotalCourses >= 10) baseScore += 0.5m;

                // More required courses = higher complexity
                if (Required >= 4) baseScore += 0.3m;
                if (Required >= 6) baseScore += 0.2m;

                // Online availability might indicate lower complexity
                if (OnlineAvailable) baseScore -= 0.2m;

                // Summer completion might indicate lower complexity
                if (CanCompleteInSummer) baseScore -= 0.1m;

                // Ensure score is within valid range
                ComplexityScore = Math.Max(1.0m, Math.Min(4.0m, baseScore));
            }
        }

        public override string ToString()
        {
            return $"{ProgramName} (ID: {Id}, {TotalCourses} courses, {CreditHours} credit hours, Complexity: {ComplexityScore:F1})";
        }
    }
}