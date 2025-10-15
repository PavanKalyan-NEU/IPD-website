using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using USPTOQueryBuilder.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace USPTOQueryBuilder.Services
{
    public class PatentsViewApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<PatentsViewApiService> _logger;

        private readonly Dictionary<string, string> Endpoints = new()
        {
            { "Patents",   "https://search.patentsview.org/api/v1/patent/" },
            { "Inventors", "https://search.patentsview.org/api/v1/inventor/" },
            { "Assignees", "https://search.patentsview.org/api/v1/assignee/" },
            { "Geographic","https://search.patentsview.org/api/v1/location/" }
        };

        public PatentsViewApiService(HttpClient httpClient, IConfiguration configuration, ILogger<PatentsViewApiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["USPTO:ApiKey"];
            _logger = logger;
        }

        public async Task<PreviewResult> PreviewQuery(PatentQuery query)
        {
            try
            {
                var results = await ExecutePatentsQuery(query, 10);

                return new PreviewResult
                {
                    Success = true,
                    Data = results.Data,
                    TotalRecords = results.TotalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Preview query failed");
                return new PreviewResult
                {
                    Success = false,
                    Message = $"Preview failed: {ex.Message}"
                };
            }
        }

        public async Task<string> ExecuteFullQuery(PatentQuery query)
        {
            var results = await ExecutePatentsQuery(query, 1000);
            return ConvertToCSV(results.Data, query.OutputFields);
        }

        public async Task<QueryResultsViewModel> ExecuteFullQueryAsJson(PatentQuery query)
        {
            try
            {
                var results = await ExecutePatentsQuery(query, 1000);

                return new QueryResultsViewModel
                {
                    Success = true,
                    TotalRecords = results.TotalCount,
                    DisplayedRecords = results.Data.Count,
                    Data = results.Data,
                    Fields = query.OutputFields?.Count > 0 ? query.OutputFields : GetFieldsFromData(results.Data)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Full query failed");
                return new QueryResultsViewModel
                {
                    Success = false,
                    Message = $"Query failed: {ex.Message}",
                    Data = new List<Dictionary<string, object>>(),
                    Fields = new List<string>()
                };
            }
        }

        private async Task<SearchResults> ExecutePatentsQuery(PatentQuery query, int limit)
        {
            var category = query.Category ?? "Patents";
            var endpoint = Endpoints.ContainsKey(category) ? Endpoints[category] : Endpoints["Patents"];

            var requestBody = new
            {
                q = BuildQueryObject(query),
                f = GetFieldsForQuery(query),
                o = new { per_page = limit }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            _logger.LogInformation($"Executing query on {endpoint}: {json}");

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Api-Key", _apiKey);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Response Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API Error: {responseContent}");
                throw new Exception($"API request failed: {responseContent}");
            }

            // Parse as JObject to handle dynamic response structure
            var jsonResponse = JObject.Parse(responseContent);
            var totalHits = jsonResponse["total_hits"]?.Value<int>() ?? 0;

            var results = new SearchResults
            {
                TotalCount = totalHits,
                Data = new List<Dictionary<string, object>>()
            };

            // Determine which array to use based on endpoint
            JArray dataArray = null;
            switch (category)
            {
                case "Patents":
                    dataArray = jsonResponse["patents"] as JArray;
                    break;
                case "Inventors":
                    dataArray = jsonResponse["inventors"] as JArray;
                    break;
                case "Assignees":
                    dataArray = jsonResponse["assignees"] as JArray;
                    break;
                case "Geographic":
                    dataArray = jsonResponse["locations"] as JArray;
                    break;
            }

            if (dataArray != null)
            {
                foreach (var item in dataArray)
                {
                    var flattenedItem = FlattenPatentData(item as JObject, query.OutputFields);
                    results.Data.Add(flattenedItem);
                }
            }

            return results;
        }

        private Dictionary<string, object> FlattenPatentData(JObject patent, List<string> requestedFields)
        {
            var flattened = new Dictionary<string, object>();

            // Process each token in the patent
            foreach (var property in patent.Properties())
            {
                var propertyName = property.Name;
                var propertyValue = property.Value;

                if (propertyValue.Type == JTokenType.Array &&
                    (propertyName == "inventors" || propertyName == "assignees" ||
                     propertyName == "cpc_current" || propertyName == "ipc"))
                {
                    // Handle nested arrays
                    var array = propertyValue as JArray;
                    if (array != null && array.Count > 0)
                    {
                        // For each item in the array, flatten its properties
                        for (int i = 0; i < array.Count; i++)
                        {
                            if (array[i] is JObject nestedObj)
                            {
                                foreach (var nestedProp in nestedObj.Properties())
                                {
                                    var nestedFieldName = $"{propertyName}.{nestedProp.Name}";

                                    // If multiple items exist, create indexed fields
                                    if (array.Count > 1)
                                    {
                                        var indexedFieldName = $"{propertyName}[{i}].{nestedProp.Name}";
                                        flattened[indexedFieldName] = nestedProp.Value?.ToString();
                                    }

                                    // Always set the first item without index for backward compatibility
                                    if (i == 0)
                                    {
                                        flattened[nestedFieldName] = nestedProp.Value?.ToString();
                                    }
                                }
                            }
                        }
                    }
                }
                else if (propertyValue.Type == JTokenType.Object)
                {
                    // Handle nested objects
                    var nestedObj = propertyValue as JObject;
                    foreach (var nestedProp in nestedObj.Properties())
                    {
                        var nestedFieldName = $"{propertyName}.{nestedProp.Name}";
                        flattened[nestedFieldName] = nestedProp.Value?.ToString();
                    }
                }
                else
                {
                    // Simple value
                    flattened[propertyName] = propertyValue?.ToString();
                }
            }

            // If specific fields were requested, filter the results
            if (requestedFields != null && requestedFields.Count > 0)
            {
                var filtered = new Dictionary<string, object>();
                foreach (var field in requestedFields)
                {
                    if (flattened.ContainsKey(field))
                    {
                        filtered[field] = flattened[field];
                    }
                    else
                    {
                        // Field not found, add as empty
                        filtered[field] = "";
                    }
                }
                return filtered;
            }

            return flattened;
        }

        private object BuildQueryObject(PatentQuery query)
        {
            var criteria = new List<object>();

            // Add date range criteria (only for patent endpoints)
            if (query.Category == null || query.Category == "Patents")
            {
                if (query.StartDate.HasValue)
                    criteria.Add(new { _gte = new { patent_date = query.StartDate.Value.ToString("yyyy-MM-dd") } });
                if (query.EndDate.HasValue)
                    criteria.Add(new { _lte = new { patent_date = query.EndDate.Value.ToString("yyyy-MM-dd") } });
            }

            // Add search criteria
            if (query.SearchCriteria != null)
            {
                foreach (var criterion in query.SearchCriteria)
                {
                    if (!string.IsNullOrEmpty(criterion.Field) && !string.IsNullOrEmpty(criterion.Value))
                    {
                        var criteriaObj = BuildCriteriaObject(criterion);
                        if (criteriaObj != null) criteria.Add(criteriaObj);
                    }
                }
            }

            if (criteria.Count == 0) return new { };
            if (criteria.Count == 1) return criteria[0];
            return new { _and = criteria.ToArray() };
        }

        private object BuildCriteriaObject(SearchCriteria criteria)
        {
            var actualField = criteria.Field;

            switch (criteria.Operator?.ToLower())
            {
                case "contains":
                case "text_any":
                    if (IsTextField(actualField))
                    {
                        return new Dictionary<string, object>
                        {
                            { "_text_any", new Dictionary<string, string> { { actualField, criteria.Value } } }
                        };
                    }
                    else
                    {
                        return new Dictionary<string, object>
                        {
                            { "_contains", new Dictionary<string, string> { { actualField, criteria.Value } } }
                        };
                    }

                case "text_all":
                    return new Dictionary<string, object>
                    {
                        { "_text_all", new Dictionary<string, string> { { actualField, criteria.Value } } }
                    };

                case "text_phrase":
                    return new Dictionary<string, object>
                    {
                        { "_text_phrase", new Dictionary<string, string> { { actualField, criteria.Value } } }
                    };

                case "equals":
                    return new Dictionary<string, string> { { actualField, criteria.Value } };

                case "greater_than":
                case "gt":
                    return new Dictionary<string, object>
                    {
                        { "_gt", new Dictionary<string, string> { { actualField, criteria.Value } } }
                    };

                case "less_than":
                case "lt":
                    return new Dictionary<string, object>
                    {
                        { "_lt", new Dictionary<string, string> { { actualField, criteria.Value } } }
                    };

                case "greater_than_or_equal":
                case "gte":
                    return new Dictionary<string, object>
                    {
                        { "_gte", new Dictionary<string, string> { { actualField, criteria.Value } } }
                    };

                case "less_than_or_equal":
                case "lte":
                    return new Dictionary<string, object>
                    {
                        { "_lte", new Dictionary<string, string> { { actualField, criteria.Value } } }
                    };

                case "begins_with":
                case "begins":
                    return new Dictionary<string, object>
                    {
                        { "_begins", new Dictionary<string, string> { { actualField, criteria.Value } } }
                    };

                case "not_equals":
                case "neq":
                    return new Dictionary<string, object>
                    {
                        { "_neq", new Dictionary<string, string> { { actualField, criteria.Value } } }
                    };

                default:
                    return new Dictionary<string, string> { { actualField, criteria.Value } };
            }
        }

        private bool IsTextField(string field)
        {
            return field.Contains("abstract") || field.Contains("title") ||
                   field.Contains("text") || field.Contains("description");
        }

        private string[] GetFieldsForQuery(PatentQuery query)
        {
            if (query.OutputFields?.Count > 0)
            {
                return query.OutputFields.ToArray();
            }

            // Default fields based on category
            var category = query.Category ?? "Patents";
            switch (category)
            {
                case "Inventors":
                    return new[]
                    {
                        "inventor_id", "inventor_name_first", "inventor_name_last",
                        "inventor_city", "inventor_state", "inventor_country"
                    };

                case "Assignees":
                    return new[]
                    {
                        "assignee_id", "assignee_organization",
                        "assignee_city", "assignee_state", "assignee_country"
                    };

                case "Geographic":
                    return new[]
                    {
                        "location_id", "location_city", "location_state",
                        "location_country", "location_latitude", "location_longitude"
                    };

                default: // Patents
                    return new[]
                    {
                        "patent_id", "patent_date", "patent_title", "patent_abstract",
                        "inventors", "assignees"  // Request full nested objects
                    };
            }
        }

        private List<string> GetFieldsFromData(List<Dictionary<string, object>> data)
        {
            if (data == null || data.Count == 0) return new List<string>();

            // Get all unique keys from all records
            var allKeys = new HashSet<string>();
            foreach (var record in data)
            {
                foreach (var key in record.Keys)
                {
                    allKeys.Add(key);
                }
            }

            return allKeys.OrderBy(k => k).ToList();
        }

        private string ConvertToCSV(List<Dictionary<string, object>> data, List<string> fields)
        {
            var csv = new StringBuilder();
            var csvFields = fields?.Count > 0 ? fields : GetFieldsFromData(data);

            // Headers
            csv.AppendLine(string.Join(",", csvFields.Select(f => $"\"{f}\"")));

            // Data rows
            foreach (var row in data)
            {
                var values = new List<string>();
                foreach (var field in csvFields)
                {
                    string value = "";

                    if (row.ContainsKey(field))
                    {
                        value = row[field]?.ToString() ?? "";
                    }

                    // Escape quotes and wrap in quotes
                    values.Add($"\"{value.Replace("\"", "\"\"")}\"");
                }
                csv.AppendLine(string.Join(",", values));
            }

            return csv.ToString();
        }

        private class SearchResults
        {
            public int TotalCount { get; set; }
            public List<Dictionary<string, object>> Data { get; set; }
        }

        private class PatentsViewResponse
        {
            [JsonProperty("error")]
            public bool Error { get; set; }

            [JsonProperty("count")]
            public int Count { get; set; }

            [JsonProperty("total_hits")]
            public int TotalHits { get; set; }

            [JsonProperty("patents")]
            public List<Dictionary<string, object>> Patents { get; set; }
        }
    }

    public class PreviewResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<Dictionary<string, object>> Data { get; set; }
        public int TotalRecords { get; set; }
    }

    public class QueryResultsViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalRecords { get; set; }
        public int DisplayedRecords { get; set; }
        public List<Dictionary<string, object>> Data { get; set; }
        public List<string> Fields { get; set; }
    }
}