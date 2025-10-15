using Microsoft.AspNetCore.Mvc;
using Northeastern_Personal_Workspace.Models;
using Northeastern_Personal_Workspace.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Northeastern_Personal_Workspace.Controllers
{
    public class ProgramSearchController : Controller
    {
        private readonly ProgramParsingService _programParsingService;

        public ProgramSearchController(ProgramParsingService programParsingService)
        {
            _programParsingService = programParsingService;
        }

        // GET: /ProgramSearch
        public async Task<IActionResult> Index()
        {
            // Load all programs on first visit to cache them
            await _programParsingService.GetAllProgramsAsync();

            ViewBag.HasSearched = false;
            return View();
        }

        // POST: /ProgramSearch/Search
        [HttpPost]
        public async Task<IActionResult> Search(string keywords)
        {
            ViewBag.HasSearched = true;
            ViewBag.Keywords = keywords;

            if (string.IsNullOrWhiteSpace(keywords))
            {
                ViewBag.SearchResults = new List<ProgramSearchResult>();
                return View("Index");
            }

            // Parse keywords - split by comma and clean up
            var keywordList = keywords
                .Split(',')
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToList();

            if (!keywordList.Any())
            {
                ViewBag.SearchResults = new List<ProgramSearchResult>();
                return View("Index");
            }

            // Ensure programs are loaded
            await _programParsingService.GetAllProgramsAsync();

            // Search programs
            var searchResults = _programParsingService.SearchPrograms(keywordList);

            ViewBag.SearchResults = searchResults;
            return View("Index");
        }
    }
}