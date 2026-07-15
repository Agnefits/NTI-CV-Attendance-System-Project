using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Attendance_System.Services.Interfaces;

namespace Attendance_System.Services.Classes
{
    /// <summary>
    /// HTTP client wrapper for the Python FastAPI face recognition microservice.
    /// Settings are loaded from the database at runtime (AIServiceBaseUrl, AIModelSecretKey,
    /// AIModelVersion) — but the HttpClient itself is registered via HttpClientFactory.
    /// </summary>
    public class FaceAIService : IFaceAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FaceAIService> _logger;
        private readonly string _secretKey;
        private readonly string _modelVersion;

        // ─── Internal DTOs (mirrors Python FastAPI request/response bodies) ───────

        private record EmbedRequest(string image, string model_version);
        private record EmbedResponse(string embedding_json, float quality_score, bool face_found);

        private record AttendRequest(
            string image,
            string model_version,
            List<AttendEmbeddingEntry> known_embeddings);

        private record AttendEmbeddingEntry(string user_id, string embedding_json);

        private record AttendResponse(List<AttendMatch> matches);
        private record AttendMatch(string user_id, float confidence, double[]? bounding_box);

        private record RebuildRequest(
            string model_version,
            List<AttendEmbeddingEntry> embeddings);

        // ────────────────────────────────────────────────────────────────────────

        public FaceAIService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<FaceAIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // These are read from appsettings so they don't require a DB round-trip
            // on every request. The DB-stored equivalents (AIModelSecretKey, AIModelVersion)
            // are seeded and can be updated via the Settings UI, but the HttpClient base
            // address is configured at startup from appsettings.
            _secretKey    = configuration["FaceAISettings:SecretKey"]    ?? "change-me";
            _modelVersion = configuration["FaceAISettings:ModelVersion"] ?? "CVFaceRecoV1";

            // Attach the secret key to every outgoing request
            _httpClient.DefaultRequestHeaders.Remove("X-AI-Secret-Key");
            _httpClient.DefaultRequestHeaders.Add("X-AI-Secret-Key", _secretKey);
        }

        /// <inheritdoc/>
        public async Task<EmbeddingResult?> ExtractEmbeddingAsync(string base64Image)
        {
            try
            {
                var request = new EmbedRequest(base64Image, _modelVersion);
                var response = await _httpClient.PostAsJsonAsync("/api/embed", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<EmbedResponse>();
                if (result is null || !result.face_found)
                {
                    _logger.LogWarning("FaceAIService: No face found in submitted image.");
                    return null;
                }

                return new EmbeddingResult
                {
                    EmbeddingJson = result.embedding_json,
                    QualityScore  = result.quality_score
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FaceAIService: Error calling /api/embed.");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<List<FaceMatch>> RecognizeFacesAsync(
            string base64Image,
            List<EmbeddingRecord> knownEmbeddings)
        {
            try
            {
                var entries = knownEmbeddings.ConvertAll(e =>
                    new AttendEmbeddingEntry(e.UserId.ToString(), e.EmbeddingJson));

                var request  = new AttendRequest(base64Image, _modelVersion, entries);
                var response = await _httpClient.PostAsJsonAsync("/api/attend", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<AttendResponse>();
                if (result is null) return new List<FaceMatch>();

                return result.matches.ConvertAll(m => new FaceMatch
                {
                    UserId      = Guid.Parse(m.user_id),
                    Confidence  = m.confidence,
                    BoundingBox = m.bounding_box
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FaceAIService: Error calling /api/attend.");
                return new List<FaceMatch>();
            }
        }

        /// <inheritdoc/>
        public async Task RebuildIndexAsync(List<EmbeddingRecord> embeddings)
        {
            try
            {
                var entries = embeddings.ConvertAll(e =>
                    new AttendEmbeddingEntry(e.UserId.ToString(), e.EmbeddingJson));

                var request  = new RebuildRequest(_modelVersion, entries);
                var response = await _httpClient.PostAsJsonAsync("/api/index/rebuild", request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("FaceAIService: FAISS index rebuilt with {Count} embeddings.", embeddings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FaceAIService: Error calling /api/index/rebuild.");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
