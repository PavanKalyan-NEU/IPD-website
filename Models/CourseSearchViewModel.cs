using Northeastern_Personal_Workspace.Models;
using System.Collections.Generic;

namespace Northeastern_Personal_Workspace.Models
{
    public class CourseSearchViewModel
    {
        public string SearchQuery { get; set; } = string.Empty;
        public List<CourseResult> Results { get; set; } = new();
        public int TotalResults { get; set; }
        public string SearchTime { get; set; } = string.Empty;
    }
}