using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace USPTOQueryBuilder.Models
{
    public class PatentQuery
    {
        public string QueryId { get; set; } = Guid.NewGuid().ToString();
        public string? Category { get; set; }
        public string UserEmail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public QueryStatus Status { get; set; } = QueryStatus.Pending;
        public string ResultFileName { get; set; }
        public long? ResultFileSize { get; set; }


        // Query Parameters
        public string PrimaryCategory { get; set; } // Patents, Inventors, Assignees
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<SearchCriteria> SearchCriteria { get; set; } = new();
        public List<string> OutputFields { get; set; } = new();
        public string QueryType { get; set; } // Predefined query types from document
    }

    public class SearchCriteria
    {
        public string Field { get; set; }
        public string Operator { get; set; } // contains, equals, greater than, etc.
        public string Value { get; set; }
        public string LogicalOperator { get; set; } // AND, OR
    }

    public enum QueryStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        EmailSent
    }

    public class QueryBuilderViewModel
    {
        [Required]
        [Display(Name = "Primary Category")]
        public string PrimaryCategory { get; set; }

        [Display(Name = "Query Type")]
        [ValidateNever] // This will skip validation for this field
        public string QueryType { get; set; }

        [EmailAddress]
        [Display(Name = "Email Address")]
        [ValidateNever] // This will skip validation for this field
        public string UserEmail { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [ValidateNever]
        public List<SearchCriteria> SearchCriteria { get; set; } = new();

        [ValidateNever]
        public List<string> SelectedOutputFields { get; set; } = new();
    }

    // Predefined query templates based on document
    public class QueryTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public List<SearchCriteria> DefaultCriteria { get; set; }
        public List<string> RecommendedFields { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

    }
}