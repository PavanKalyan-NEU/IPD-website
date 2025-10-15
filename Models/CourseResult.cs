using Northeastern_Personal_Workspace.Models;
using System.Collections.Generic;

namespace Northeastern_Personal_Workspace.Models
{
    public class CourseResult : Course
    {
        public string WhyRelevant { get; set; } = string.Empty;
        public List<string> MatchedTerms { get; set; } = new();
        public double RelevanceScore { get; set; }
    }
}