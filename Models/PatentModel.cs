namespace USPTOQueryBuilder.Models
{
    public class Patent
    {
        public string PatentNumber { get; set; }
        public DateTime PatentDate { get; set; }
        public string PatentTitle { get; set; }
        public string Abstract { get; set; }
        public string AssigneeOrganization { get; set; }
        public string CPCGroup { get; set; }
        public string CPCSubsection { get; set; }
        public List<Inventor> Inventors { get; set; }
    }

    public class Inventor
    {
        public string Name { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    public class Assignee
    {
        public string Organization { get; set; }
        public int TotalPatentCount { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    public class PreviewResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalRecords { get; set; }
        public List<Dictionary<string, object>> Data { get; set; }
    }
}