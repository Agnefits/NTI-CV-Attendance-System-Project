using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Attendance_System.Services.Interfaces;
using Attendance_System.UnitOfWork.Interfaces;

namespace Attendance_System.Services.Classes
{
    /// <summary>
    /// HTTP client wrapper for the Python FastAPI face recognition microservice.
    /// Settings are loaded from the database dynamically at runtime (AIServiceBaseUrl, AIModelSecretKey,
    /// AIModelVersion).
    /// </summary>
    public class FaceAIService : IFaceAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FaceAIService> _logger;

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
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<FaceAIService> logger)
        {
            _httpClient = httpClient;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<(string baseUrl, string secretKey, string modelVersion)> GetAISettingsAsync()
        {
            string baseUrl = "http://localhost:8000";
            string secretKey = "change-me";
            string modelVersion = "CVFaceRecoV1";

            try
            {
                var settings = await _unitOfWork.Settings.GetAllAsync();
                var settingsList = settings.ToList();

                var baseUrlSetting = settingsList.FirstOrDefault(s => s.Key == "AIServiceBaseUrl")?.Value;
                var secretKeySetting = settingsList.FirstOrDefault(s => s.Key == "AIModelSecretKey")?.Value;
                var modelVersionSetting = settingsList.FirstOrDefault(s => s.Key == "AIModelVersion")?.Value;

                if (!string.IsNullOrWhiteSpace(baseUrlSetting)) baseUrl = baseUrlSetting;
                if (!string.IsNullOrWhiteSpace(secretKeySetting)) secretKey = secretKeySetting;
                if (!string.IsNullOrWhiteSpace(modelVersionSetting)) modelVersion = modelVersionSetting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FaceAIService: Error reading AI settings from database, falling back to configuration.");
                // Fallback to configuration
                baseUrl = _configuration["FaceAISettings:BaseUrl"] ?? baseUrl;
                secretKey = _configuration["FaceAISettings:SecretKey"] ?? secretKey;
                modelVersion = _configuration["FaceAISettings:ModelVersion"] ?? modelVersion;
            }

            return (baseUrl, secretKey, modelVersion);
        }

        /// <inheritdoc/>
        public async Task<EmbeddingResult?> ExtractEmbeddingAsync(string base64Image)
        {
            try
            {
                var (baseUrl, secretKey, modelVersion) = await GetAISettingsAsync();

                _httpClient.DefaultRequestHeaders.Remove("X-AI-Secret-Key");
                _httpClient.DefaultRequestHeaders.Add("X-AI-Secret-Key", secretKey);

                var baseUriStr = baseUrl.TrimEnd('/') + "/";
                var targetUri = new Uri(new Uri(baseUriStr), "api/embed");

                var request = new EmbedRequest(base64Image, modelVersion);
                var response = await _httpClient.PostAsJsonAsync(targetUri, request);
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
                var (baseUrl, secretKey, modelVersion) = await GetAISettingsAsync();

                _httpClient.DefaultRequestHeaders.Remove("X-AI-Secret-Key");
                _httpClient.DefaultRequestHeaders.Add("X-AI-Secret-Key", secretKey);

                var baseUriStr = baseUrl.TrimEnd('/') + "/";
                var targetUri = new Uri(new Uri(baseUriStr), "api/attend");

                var entries = knownEmbeddings.ConvertAll(e =>
                    new AttendEmbeddingEntry(e.UserId.ToString(), e.EmbeddingJson));

                var request  = new AttendRequest(base64Image, modelVersion, entries);
                var response = await _httpClient.PostAsJsonAsync(targetUri, request);
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
                var (baseUrl, secretKey, modelVersion) = await GetAISettingsAsync();

                _httpClient.DefaultRequestHeaders.Remove("X-AI-Secret-Key");
                _httpClient.DefaultRequestHeaders.Add("X-AI-Secret-Key", secretKey);

                var baseUriStr = baseUrl.TrimEnd('/') + "/";
                var targetUri = new Uri(new Uri(baseUriStr), "api/index/rebuild");

                var entries = embeddings.ConvertAll(e =>
                    new AttendEmbeddingEntry(e.UserId.ToString(), e.EmbeddingJson));

                var request  = new RebuildRequest(modelVersion, entries);
                var response = await _httpClient.PostAsJsonAsync(targetUri, request);
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
                var (baseUrl, secretKey, _) = await GetAISettingsAsync();

                _httpClient.DefaultRequestHeaders.Remove("X-AI-Secret-Key");
                _httpClient.DefaultRequestHeaders.Add("X-AI-Secret-Key", secretKey);

                var baseUriStr = baseUrl.TrimEnd('/') + "/";
                var targetUri = new Uri(new Uri(baseUriStr), "api/health");

                var response = await _httpClient.GetAsync(targetUri);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
