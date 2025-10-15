using System;
namespace Northeastern_Personal_Workspace.Models
{
    // Models/OutreachRecord.cs
    public class OutreachRecord
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public string Organization { get; set; }
        public System.DateTime OutreachDate { get; set; }
        public string OutreachEffort { get; set; }
        public string OutreachType { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public string OutreachBy { get; set; }
        public System.DateTime? FollowUpDate { get; set; }
    }
}

