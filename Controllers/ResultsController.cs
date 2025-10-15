using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using USPTOQueryBuilder.Services;

namespace USPTOQueryBuilder.Controllers
{
    public class ResultsController : Controller
    {
        private readonly QueryStorageService _storageService;
        private readonly FileProcessingService _fileService;

        public ResultsController(QueryStorageService storageService, FileProcessingService fileService)
        {
            _storageService = storageService;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Download(string queryId)
        {
            var query = _storageService.GetQuery(queryId);
            if (query == null || string.IsNullOrEmpty(query.ResultFileName))
            {
                return NotFound();
            }

            try
            {
                var fileStream = await _fileService.GetFileStream(query.ResultFileName);
                var contentType = query.ResultFileName.EndsWith(".gz")
                    ? "application/gzip"
                    : "text/csv";

                return File(fileStream, contentType, query.ResultFileName);
            }
            catch
            {
                return NotFound();
            }
        }
    }
}