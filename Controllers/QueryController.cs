using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using USPTOQueryBuilder.Models;
using USPTOQueryBuilder.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace USPTOQueryBuilder.Controllers
{
    public class QueryController : Controller
    {
        private readonly QueryBuilderService _queryBuilderService;
        private readonly PatentsViewApiService _apiService;
        private readonly QueryStorageService _storageService;
        private readonly FileProcessingService _fileService;
        private readonly ILogger<QueryController> _logger;

        public QueryController(
            QueryBuilderService queryBuilderService,
            PatentsViewApiService apiService,
            QueryStorageService storageService,
            FileProcessingService fileService,
            ILogger<QueryController> logger)
        {
            _queryBuilderService = queryBuilderService;
            _apiService = apiService;
            _storageService = storageService;
            _fileService = fileService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Builder()
        {
            ViewBag.QueryTemplates = _queryBuilderService.GetQueryTemplates();
            ViewBag.Categories = new[] { "Patents", "Inventors", "Assignees" };
            return View(new QueryBuilderViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteQuery(QueryBuilderViewModel model)
        {
            try
            {
                // Remove validation for optional fields
                ModelState.Remove("QueryType");
                ModelState.Remove("UserEmail");

                if (!ModelState.IsValid)
                {
                    ViewBag.QueryTemplates = _queryBuilderService.GetQueryTemplates();
                    ViewBag.Categories = new[] { "Patents", "Inventors", "Assignees" };
                    return View("Builder", model);
                }

                // Apply template settings if selected
                string queryDescription = "Custom Query";
                if (!string.IsNullOrEmpty(model.QueryType))
                {
                    var templates = _queryBuilderService.GetQueryTemplates();
                    var selectedTemplate = templates.FirstOrDefault(t => t.Id == model.QueryType);
                    if (selectedTemplate != null)
                    {
                        queryDescription = selectedTemplate.Name;
                        model.StartDate = model.StartDate ?? selectedTemplate.StartDate;
                        model.EndDate = model.EndDate ?? selectedTemplate.EndDate;

                        // Apply template criteria if no custom criteria provided
                        if (model.SearchCriteria == null || !model.SearchCriteria.Any())
                        {
                            model.SearchCriteria = selectedTemplate.DefaultCriteria;
                        }

                        // Apply recommended fields if none selected
                        if (model.SelectedOutputFields == null || !model.SelectedOutputFields.Any())
                        {
                            model.SelectedOutputFields = selectedTemplate.RecommendedFields;
                        }
                    }
                }

                var query = MapViewModelToQuery(model);

                _logger.LogInformation($"Executing query: {queryDescription}");

                // Execute query
                var results = await _apiService.ExecuteFullQueryAsJson(query);

                // Add query metadata to results
                ViewBag.QueryDescription = queryDescription;
                ViewBag.DateRange = $"{query.StartDate:MMM d, yyyy} - {query.EndDate:MMM d, yyyy}";
                ViewBag.QueryCriteria = query.SearchCriteria;
                ViewBag.QueryId = query.QueryId;

                // Save query for download functionality
                await _storageService.SaveQuery(query);

                // Generate and save CSV for download if results exist
                if (results.Success && results.Data != null && results.Data.Any())
                {
                    try
                    {
                        var csvContent = await _apiService.ExecuteFullQuery(query);
                        var (fileName, fileSize) = await _fileService.SaveQueryResults(query.QueryId, csvContent);
                        query.ResultFileName = fileName;
                        query.ResultFileSize = fileSize;
                        query.Status = QueryStatus.Completed;
                        await _storageService.UpdateQuery(query);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save CSV file");
                        // Continue showing results even if CSV save fails
                    }
                }

                return View("Results", results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Query execution failed");
                TempData["Error"] = $"Query failed: {ex.Message}";
                return RedirectToAction("Builder");
            }
        }

        [HttpGet]
        public IActionResult GetFieldsForCategory(string category)
        {
            var fields = _queryBuilderService.GetAvailableFields(category);
            return Json(fields);
        }

        [HttpGet]
        public IActionResult GetQueryTemplate(string templateId)
        {
            var templates = _queryBuilderService.GetQueryTemplates();
            var template = templates.Find(t => t.Id == templateId);
            return Json(template);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadResults(string queryId)
        {
            try
            {
                var query = _storageService.GetQuery(queryId);
                if (query == null || string.IsNullOrEmpty(query.ResultFileName))
                {
                    return NotFound();
                }

                var fileStream = await _fileService.GetFileStream(query.ResultFileName);
                var contentType = query.ResultFileName.EndsWith(".gz")
                    ? "application/gzip"
                    : "text/csv";

                return File(fileStream, contentType, query.ResultFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download failed");
                return NotFound();
            }
        }

        private PatentQuery MapViewModelToQuery(QueryBuilderViewModel model)
        {
            return new PatentQuery
            {
                UserEmail = model.UserEmail ?? "user@example.com",
                PrimaryCategory = model.PrimaryCategory ?? "Patents",
                QueryType = model.QueryType,
                StartDate = model.StartDate ?? new DateTime(2024, 1, 1),
                EndDate = model.EndDate ?? DateTime.Now,
                SearchCriteria = model.SearchCriteria ?? new List<SearchCriteria>(),
                OutputFields = model.SelectedOutputFields ?? new List<string>()
            };
        }
    }
}