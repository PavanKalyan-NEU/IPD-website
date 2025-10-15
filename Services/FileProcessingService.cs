using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace USPTOQueryBuilder.Services
{
    public class FileProcessingService
    {
        private readonly string _storageBasePath;
        private readonly ILogger<FileProcessingService> _logger;
        private const long MAX_FILE_SIZE = 1073741824; // 1GB in bytes

        public FileProcessingService(IConfiguration configuration, ILogger<FileProcessingService> logger)
        {
            _storageBasePath = configuration["Storage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "QueryResults");
            Directory.CreateDirectory(_storageBasePath);
            _logger = logger;
        }

        public async Task<(string fileName, long fileSize)> SaveQueryResults(string queryId, string csvContent)
        {
            var fileName = $"query_{queryId}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            var filePath = Path.Combine(_storageBasePath, fileName);

            // Check estimated size
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            if (bytes.Length > MAX_FILE_SIZE)
            {
                throw new InvalidOperationException($"Result size ({bytes.Length / (1024 * 1024)}MB) exceeds 1GB limit");
            }

            // If file is large, compress it
            if (bytes.Length > 100 * 1024 * 1024) // 100MB
            {
                fileName += ".gz";
                filePath += ".gz";
                await SaveCompressedFile(filePath, csvContent);
            }
            else
            {
                await File.WriteAllTextAsync(filePath, csvContent);
            }

            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation($"Saved query results to {fileName}, size: {fileInfo.Length} bytes");

            return (fileName, fileInfo.Length);
        }

        private async Task SaveCompressedFile(string filePath, string content)
        {
            using var fileStream = File.Create(filePath);
            using var compressionStream = new GZipStream(fileStream, CompressionLevel.Optimal);
            using var writer = new StreamWriter(compressionStream);
            await writer.WriteAsync(content);
        }

        public async Task<Stream> GetFileStream(string fileName)
        {
            var filePath = Path.Combine(_storageBasePath, fileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Query result file not found");
            }

            return File.OpenRead(filePath);
        }

        public void CleanupOldFiles(int daysToKeep = 7)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var files = Directory.GetFiles(_storageBasePath, "query_*.csv*");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc < cutoffDate)
                    {
                        File.Delete(file);
                        _logger.LogInformation($"Deleted old query file: {fileInfo.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file cleanup");
            }
        }
    }
}