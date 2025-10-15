using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using USPTOQueryBuilder.Models;
using USPTOQueryBuilder.Services;
using Microsoft.Extensions.Logging;

namespace USPTOQueryBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly PatentsViewApiService _apiService;
        private readonly ILogger<TestController> _logger;

        public TestController(PatentsViewApiService apiService, ILogger<TestController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [HttpGet("simple")]
        public async Task<IActionResult> TestSimpleQuery()
        {
            try
            {
                // Create a very simple query to test basic API connectivity
                var query = new PatentQuery
                {
                    UserEmail = "test@example.com",
                    PrimaryCategory = "Patents",
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = new DateTime(2024, 1, 31), // Small date range for testing
                    SearchCriteria = new List<SearchCriteria>(), // No search criteria
                    OutputFields = new List<string> { "patent_id", "patent_date", "patent_title" }
                };

                var results = await _apiService.ExecuteFullQueryAsJson(query);
                
                return Ok(new 
                { 
                    success = results.Success,
                    message = results.Message,
                    totalRecords = results.TotalRecords,
                    displayedRecords = results.DisplayedRecords,
                    sampleData = results.Data?.Take(3) // Only show first 3 records for testing
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test query failed");
                return BadRequest(new { error = ex.Message, innerException = ex.InnerException?.Message });
            }
        }

        [HttpGet("ai")]
        public async Task<IActionResult> TestAIQuery()
        {
            try
            {
                // Test AI query with simplified criteria
                var query = new PatentQuery
                {
                    UserEmail = "test@example.com",
                    PrimaryCategory = "Patents",
                    StartDate = new DateTime(2023, 1, 1),
                    EndDate = new DateTime(2024, 1, 31),
                    SearchCriteria = new List<SearchCriteria>
                    {
                        new SearchCriteria
                        {
                            Field = "patent_title",
                            Operator = "contains",
                            Value = "artificial intelligence"
                        }
                    },
                    OutputFields = new List<string> { "patent_id", "patent_date", "patent_title", "assignees" }
                };

                var results = await _apiService.ExecuteFullQueryAsJson(query);
                
                return Ok(new 
                { 
                    success = results.Success,
                    message = results.Message,
                    totalRecords = results.TotalRecords,
                    displayedRecords = results.DisplayedRecords,
                    sampleData = results.Data?.Take(3)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI test query failed");
                return BadRequest(new { error = ex.Message, innerException = ex.InnerException?.Message });
            }
        }
    }
}