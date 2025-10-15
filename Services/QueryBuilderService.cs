using System;
using System.Collections.Generic;
using USPTOQueryBuilder.Models;

namespace USPTOQueryBuilder.Services
{
    public class QueryBuilderService
    {
        public List<QueryTemplate> GetQueryTemplates()
        {
            return new List<QueryTemplate>
            {
                // Date-based templates (no search criteria to avoid API errors)
                new QueryTemplate
                {
                    Id = "2024-patents",
                    Name = "All 2024 Patents",
                    Description = "All patents from 2024 - filter by keywords after download",
                    Category = "Patents",
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = new DateTime(2024, 12, 31),
                    DefaultCriteria = new List<SearchCriteria>(),
                    RecommendedFields = new List<string>
                    {
                        "patent_id", "patent_date", "patent_title", "patent_abstract",
                        "inventors.inventor_name_first", "inventors.inventor_name_last",
                        "assignees.assignee_organization"
                    }
                },

                new QueryTemplate
                {
                    Id = "recent-patents",
                    Name = "Recent Patents (2022-2025)",
                    Description = "All patents from 2022 onwards for emerging technology analysis",
                    Category = "Patents",
                    StartDate = new DateTime(2022, 1, 1),
                    EndDate = DateTime.Now,
                    DefaultCriteria = new List<SearchCriteria>(),
                    RecommendedFields = new List<string>
                    {
                        "patent_id", "patent_date", "patent_title", "patent_abstract",
                        "inventors.inventor_name_first", "inventors.inventor_name_last",
                        "assignees.assignee_organization", "cpc_current.cpc_group_id"
                    }
                },
                
                // Technology-specific templates
                new QueryTemplate
                {
                    Id = "ai-search",
                    Name = "Artificial Intelligence Patents",
                    Description = "Patents mentioning 'artificial intelligence' in abstract",
                    Category = "Patents",
                    StartDate = new DateTime(2022, 1, 1),
                    EndDate = DateTime.Now,
                    DefaultCriteria = new List<SearchCriteria>
                    {
                        new SearchCriteria
                        {
                            Field = "patent_abstract",
                            Operator = "contains",
                            Value = "artificial intelligence"
                        }
                    },
                    RecommendedFields = new List<string>
                    {
                        "patent_id", "patent_date", "patent_title", "patent_abstract",
                        "inventors.inventor_name_first", "inventors.inventor_name_last",
                        "assignees.assignee_organization", "patent_num_times_cited_by_us_patents"
                    }
                },

                new QueryTemplate
                {
                    Id = "ml-search",
                    Name = "Machine Learning Patents",
                    Description = "Patents mentioning 'machine learning' in abstract",
                    Category = "Patents",
                    StartDate = new DateTime(2022, 1, 1),
                    EndDate = DateTime.Now,
                    DefaultCriteria = new List<SearchCriteria>
                    {
                        new SearchCriteria
                        {
                            Field = "patent_abstract",
                            Operator = "contains",
                            Value = "machine learning"
                        }
                    },
                    RecommendedFields = new List<string>
                    {
                        "patent_id", "patent_date", "patent_title", "patent_abstract",
                        "inventors.inventor_name_first", "inventors.inventor_name_last",
                        "assignees.assignee_organization", "patent_num_times_cited_by_us_patents"
                    }
                },

                new QueryTemplate
                {
                    Id = "renewable-energy",
                    Name = "Renewable Energy Patents",
                    Description = "Patents mentioning 'renewable energy' in abstract",
                    Category = "Patents",
                    StartDate = new DateTime(2022, 1, 1),
                    EndDate = DateTime.Now,
                    DefaultCriteria = new List<SearchCriteria>
                    {
                        new SearchCriteria
                        {
                            Field = "patent_abstract",
                            Operator = "contains",
                            Value = "renewable energy"
                        }
                    },
                    RecommendedFields = new List<string>
                    {
                        "patent_id", "patent_date", "patent_title", "patent_abstract",
                        "inventors.inventor_name_first", "inventors.inventor_name_last",
                        "assignees.assignee_organization", "cpc_current.cpc_group_id"
                    }
                },

                new QueryTemplate
                {
                    Id = "quantum-computing",
                    Name = "Quantum Computing Patents",
                    Description = "Patents mentioning 'quantum computing' in abstract",
                    Category = "Patents",
                    StartDate = new DateTime(2020, 1, 1),
                    EndDate = DateTime.Now,
                    DefaultCriteria = new List<SearchCriteria>
                    {
                        new SearchCriteria
                        {
                            Field = "patent_abstract",
                            Operator = "contains",
                            Value = "quantum computing"
                        }
                    },
                    RecommendedFields = new List<string>
                    {
                        "patent_id", "patent_date", "patent_title", "patent_abstract",
                        "inventors.inventor_name_first", "inventors.inventor_name_last",
                        "assignees.assignee_organization", "patent_num_times_cited_by_us_patents"
                    }
                },

                // Inventor-based templates
                new QueryTemplate
                {
                    Id = "prolific-inventors",
                    Name = "Prolific Inventors Search",
                    Description = "Search for patents by specific inventors",
                    Category = "Inventors",
                    StartDate = new DateTime(2020, 1, 1),
                    EndDate = DateTime.Now,
                    DefaultCriteria = new List<SearchCriteria>(),
                    RecommendedFields = new List<string>
                    {
                        "patent_id", "patent_date", "patent_title",
                        "inventors.inventor_name_first", "inventors.inventor_name_last",
                        "inventors.inventor_city", "inventors.inventor_state",
                        "assignees.assignee_organization"
                    }
                },

                // Company/Assignee templates
                new QueryTemplate
                {
                    Id = "company-patents",
                    Name = "Company Patent Portfolio",
                    Description = "Patents assigned to specific companies",
                    Category = "Assignees",
                    StartDate = new DateTime(2022, 1, 1),
                    EndDate = DateTime.Now,
                    DefaultCriteria = new List<SearchCriteria>(),
                    RecommendedFields = new List<string>
                    {
                        "patent_id", "patent_date", "patent_title", "patent_abstract",
                        "assignees.assignee_organization", "assignees.assignee_city",
                        "assignees.assignee_state", "patent_num_times_cited_by_us_patents"
                    }
                },

                // Citation analysis template
                new QueryTemplate
                {
                    Id = "highly-cited",
                    Name = "Highly Cited Patents",
                    Description = "Patents with significant citation impact",
                    Category = "Patents",
                    StartDate = new DateTime(2015, 1, 1),
                    EndDate = DateTime.Now,
                    DefaultCriteria = new List<SearchCriteria>(),
                    RecommendedFields = new List<string>
                    {
                        "patent_id", "patent_date", "patent_title",
                        "patent_num_times_cited_by_us_patents",
                        "inventors.inventor_name_last",
                        "assignees.assignee_organization"
                    }
                },

                // Geographic templates
                new QueryTemplate
                {
                    Id = "geographic-analysis",
                    Name = "Geographic Innovation Analysis",
                    Description = "Patents by inventor location",
                    Category = "Geographic",
                    StartDate = new DateTime(2022, 1, 1),
                    EndDate = DateTime.Now,
                    DefaultCriteria = new List<SearchCriteria>(),
                    RecommendedFields = new List<string>
                    {
                        "patent_id", "patent_date", "patent_title",
                        "inventors.inventor_city", "inventors.inventor_state",
                        "inventors.inventor_country", "assignees.assignee_organization"
                    }
                }
            };
        }

        public List<string> GetAvailableFields(string category)
        {
            return category switch
            {
                "Patents" => new List<string>
                {
                    // Basic patent fields
                    "patent_id",
                    "patent_date",
                    "patent_title",
                    "patent_abstract",
                    "patent_type",
                    "patent_number",
                    "patent_kind",
                    "patent_num_claims",
                    "patent_num_times_cited_by_us_patents",
                    "patent_firstnamed_assignee_id",
                    "patent_firstnamed_inventor_id",
                    
                    // Nested inventor fields
                    "inventors.inventor_id",
                    "inventors.inventor_name_first",
                    "inventors.inventor_name_last",
                    "inventors.inventor_city",
                    "inventors.inventor_state",
                    "inventors.inventor_country",
                    "inventors.inventor_sequence",
                    
                    // Nested assignee fields
                    "assignees.assignee_id",
                    "assignees.assignee_organization",
                    "assignees.assignee_individual_name_first",
                    "assignees.assignee_individual_name_last",
                    "assignees.assignee_city",
                    "assignees.assignee_state",
                    "assignees.assignee_country",
                    "assignees.assignee_sequence",
                    "assignees.assignee_type",
                    
                    // Classification fields
                    "cpc_current.cpc_section_id",
                    "cpc_current.cpc_subsection_id",
                    "cpc_current.cpc_group_id",
                    "cpc_current.cpc_subgroup_id",
                    "ipc.ipc_section",
                    "ipc.ipc_class",
                    "ipc.ipc_subclass",
                    "ipc.ipc_main_group",
                    "ipc.ipc_subgroup",
                    "wipo.wipo_field_id",
                    "wipo.wipo_field_title",
                    
                    // Application fields
                    "applications.application_id",
                    "applications.application_number",
                    "applications.application_date",
                    
                    // Attorney fields
                    "attorneys.attorney_name_first",
                    "attorneys.attorney_name_last",
                    "attorneys.attorney_organization"
                },

                "Inventors" => new List<string>
                {
                    "inventor_id",
                    "inventor_name_first",
                    "inventor_name_last",
                    "inventor_city",
                    "inventor_state",
                    "inventor_country",
                    "inventor_num_patents",
                    "inventor_num_assignees",
                    "inventor_lastknown_city",
                    "inventor_lastknown_state",
                    "inventor_lastknown_country",
                    "inventor_first_seen_date",
                    "inventor_last_seen_date"
                },

                "Assignees" => new List<string>
                {
                    "assignee_id",
                    "assignee_organization",
                    "assignee_individual_name_first",
                    "assignee_individual_name_last",
                    "assignee_city",
                    "assignee_state",
                    "assignee_country",
                    "assignee_type",
                    "assignee_num_patents",
                    "assignee_num_inventors",
                    "assignee_first_seen_date",
                    "assignee_last_seen_date"
                },

                "Geographic" => new List<string>
                {
                    "location_id",
                    "location_city",
                    "location_state",
                    "location_country",
                    "location_latitude",
                    "location_longitude",
                    "location_county",
                    "location_state_fips",
                    "location_county_fips"
                },

                "Classifications" => new List<string>
                {
                    // CPC fields
                    "cpc_section_id",
                    "cpc_subsection_id",
                    "cpc_group_id",
                    "cpc_subgroup_id",
                    "cpc_category",
                    
                    // IPC fields
                    "ipc_section",
                    "ipc_class",
                    "ipc_subclass",
                    "ipc_main_group",
                    "ipc_subgroup",
                    
                    // WIPO fields
                    "wipo_field_id",
                    "wipo_field_title",
                    "wipo_sector_title",
                    
                    // USPC fields
                    "uspc_mainclass_id",
                    "uspc_mainclass_title",
                    "uspc_subclass_id",
                    "uspc_subclass_title"
                },

                _ => new List<string>()
            };
        }

        public List<string> GetOperators(string fieldType)
        {
            // Enhanced operators based on field type
            if (fieldType.Contains("date"))
            {
                return new List<string> { "equals", "before", "after", "between" };
            }
            else if (fieldType.Contains("num") || fieldType.Contains("count"))
            {
                return new List<string> { "equals", "greater_than", "less_than", "between" };
            }
            else if (fieldType.Contains("abstract") || fieldType.Contains("title") || fieldType.Contains("text"))
            {
                return new List<string> { "contains", "text_all", "text_any", "text_phrase" };
            }
            else
            {
                return new List<string> { "contains", "equals", "begins_with" };
            }
        }

        public Dictionary<string, string> GetFieldDescriptions()
        {
            return new Dictionary<string, string>
            {
                // Patent fields
                { "patent_id", "Unique patent identifier" },
                { "patent_date", "Date patent was granted" },
                { "patent_title", "Title of the patent" },
                { "patent_abstract", "Abstract text of the patent" },
                { "patent_type", "Type of patent (utility, design, plant, etc.)" },
                { "patent_num_claims", "Number of claims in the patent" },
                { "patent_num_times_cited_by_us_patents", "Number of times cited by other US patents" },
                
                // Inventor fields
                { "inventors.inventor_name_first", "Inventor's first name" },
                { "inventors.inventor_name_last", "Inventor's last name" },
                { "inventors.inventor_city", "Inventor's city" },
                { "inventors.inventor_state", "Inventor's state" },
                { "inventors.inventor_country", "Inventor's country" },
                
                // Assignee fields
                { "assignees.assignee_organization", "Organization that owns the patent" },
                { "assignees.assignee_type", "Type of assignee (1: Unassigned, 2: US Company, 3: Foreign Company, etc.)" },
                
                // Classification fields
                { "cpc_current.cpc_group_id", "CPC classification group" },
                { "wipo.wipo_field_title", "WIPO technology field" }
            };
        }
    }

    public class QueryTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<SearchCriteria> DefaultCriteria { get; set; }
        public List<string> RecommendedFields { get; set; }
    }
}