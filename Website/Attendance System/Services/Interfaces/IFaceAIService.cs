using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Attendance_System.Services.Interfaces
{
    /// <summary>
    /// Describes a single recognized face returned by the AI microservice.
    /// </summary>
    public class FaceMatch
    {
        /// <summary>Matched BaseUser ID (Student or Employee).</summary>
        public Guid UserId { get; set; }

        /// <summary>Cosine similarity score (0.0 – 1.0). Higher = better match.</summary>
        public float Confidence { get; set; }

        /// <summary>Bounding box from the image: [x1, y1, x2, y2].</summary>
        public double[]? BoundingBox { get; set; }
    }

    /// <summary>
    /// Result of a single enrollment / embedding extraction call.
    /// </summary>
    public class EmbeddingResult
    {
        /// <summary>JSON-serialised float array, e.g. "[0.12, -0.45, ...]".</summary>
        public string EmbeddingJson { get; set; } = string.Empty;

        /// <summary>Model-reported quality score (0.0 – 1.0).</summary>
        public float QualityScore { get; set; }
    }

    /// <summary>
    /// Single record used when pushing known embeddings to the AI service's FAISS index.
    /// </summary>
    public class EmbeddingRecord
    {
        public Guid UserId { get; set; }
        public string EmbeddingJson { get; set; } = string.Empty;
    }

    /// <summary>
    /// Abstraction over the Python FastAPI face recognition microservice.
    /// All calls are made with the AIModelSecretKey header for authentication.
    /// </summary>
    public interface IFaceAIService
    {
        /// <summary>
        /// Sends a single base-64 image to the AI service and returns the extracted embedding.
        /// Returns null if no face is detected or quality is too low.
        /// </summary>
        Task<EmbeddingResult?> ExtractEmbeddingAsync(string base64Image);

        /// <summary>
        /// Sends a frame (base-64 image) and a list of known embeddings scoped to the relevant
        /// classroom/lesson context, then returns all recognized faces in the image.
        /// The caller is responsible for scoping which embeddings to pass (students in the lesson
        /// or all employees).
        /// </summary>
        Task<List<FaceMatch>> RecognizeFacesAsync(string base64Image, List<EmbeddingRecord> knownEmbeddings);

        /// <summary>
        /// Replaces the full FAISS index in the AI service with the supplied embedding records.
        /// Call this after bulk enrollment or when the service restarts.
        /// </summary>
        Task RebuildIndexAsync(List<EmbeddingRecord> embeddings);

        /// <summary>
        /// Pings the AI service health endpoint. Returns true when the service is ready.
        /// </summary>
        Task<bool> IsHealthyAsync();
    }
}
