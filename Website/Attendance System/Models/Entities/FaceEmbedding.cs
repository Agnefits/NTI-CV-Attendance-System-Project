using System;
using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Models.Entities
{
    public class FaceEmbedding : BaseModel
    {
        public Guid BaseUserId { get; set; }
        public virtual BaseUser? BaseUser { get; set; }
        public required string Embedding { get; set; }
    }
}
