using System.Collections.Generic;

namespace Northeastern_Personal_Workspace.Models
{
    public class Course
    {
        public string CourseNumber { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Credits { get; set; } = string.Empty;
        public string Program { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string College { get; set; } = string.Empty;
        public List<string> Concentrations { get; set; } = new();
        public string CourseType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Prerequisites { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
    }
}