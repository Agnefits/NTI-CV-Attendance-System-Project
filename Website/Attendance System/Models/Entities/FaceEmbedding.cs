using System;
using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Models.Entities
{
    /// <summary>
    /// Stores a single face embedding vector (as a JSON float array string) for
    /// any system user — Student, Employee, Teacher, or Admin.
    /// Multiple embeddings can be stored per user to capture different angles/lighting.
    /// </summary>
    public class FaceEmbedding : BaseModel
    {
        /// <summary>Foreign key to BaseUsers (works for Student, Employee, Teacher, Admin).</summary>
        public Guid BaseUserId { get; set; }
        public virtual BaseUser? BaseUser { get; set; }

        /// <summary>
        /// JSON-serialised float array representing the 512-dimensional ArcFace embedding vector.
        /// Example: "[0.123, -0.456, 0.789, ...]"
        /// </summary>
        public required string EmbeddingJson { get; set; }

        /// <summary>Model-reported quality score (0.0 – 1.0). Embeddings below 0.6 are typically discarded.</summary>
        public float QualityScore { get; set; } = 1.0f;

        /// <summary>Optional capture angle label e.g. "front", "slight-left", "slight-right".</summary>
        public string? CaptureAngle { get; set; }

        /// <summary>Human-readable label/description for this embedding sample (e.g. "Enrollment shot 1").</summary>
        public string? Label { get; set; }
    }
}
