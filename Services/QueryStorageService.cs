using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using USPTOQueryBuilder.Models;

namespace USPTOQueryBuilder.Services
{
    public class QueryStorageService
    {
        private readonly string _storageBasePath;
        private readonly ConcurrentDictionary<string, PatentQuery> _queryCache = new();
        private readonly string _queryMetadataFile;

        public QueryStorageService(IConfiguration configuration)
        {
            _storageBasePath = configuration["Storage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "QueryData");
            Directory.CreateDirectory(_storageBasePath);
            _queryMetadataFile = Path.Combine(_storageBasePath, "queries.json");
            LoadQueries();
        }

        public async Task<string> SaveQuery(PatentQuery query)
        {
            _queryCache[query.QueryId] = query;
            await SaveQueriesToFile();
            return query.QueryId;
        }

        public PatentQuery GetQuery(string queryId)
        {
            return _queryCache.TryGetValue(queryId, out var query) ? query : null;
        }

        public async Task UpdateQuery(PatentQuery query)
        {
            if (_queryCache.ContainsKey(query.QueryId))
            {
                _queryCache[query.QueryId] = query;
                await SaveQueriesToFile();
            }
        }

        public List<PatentQuery> GetRecentQueries(int count = 10)
        {
            return _queryCache.Values
                .OrderByDescending(q => q.CreatedAt)
                .Take(count)
                .ToList();
        }

        public List<PatentQuery> GetQueriesByEmail(string email)
        {
            return _queryCache.Values
                .Where(q => q.UserEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(q => q.CreatedAt)
                .ToList();
        }

        private void LoadQueries()
        {
            if (File.Exists(_queryMetadataFile))
            {
                try
                {
                    var json = File.ReadAllText(_queryMetadataFile);
                    var queries = JsonSerializer.Deserialize<List<PatentQuery>>(json);
                    foreach (var query in queries)
                    {
                        _queryCache[query.QueryId] = query;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue - start with empty cache
                    Console.WriteLine($"Error loading queries: {ex.Message}");
                }
            }
        }

        private async Task SaveQueriesToFile()
        {
            try
            {
                var queries = _queryCache.Values.ToList();
                var json = JsonSerializer.Serialize(queries, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(_queryMetadataFile, json);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error saving queries: {ex.Message}");
            }
        }

        public void CleanupOldQueries(int daysToKeep = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var queriesToRemove = _queryCache.Values
                .Where(q => q.CreatedAt < cutoffDate)
                .Select(q => q.QueryId)
                .ToList();

            foreach (var queryId in queriesToRemove)
            {
                _queryCache.TryRemove(queryId, out _);
            }

            SaveQueriesToFile().Wait();
        }
    }
}