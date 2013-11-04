using System;

namespace UmbracoDiff.Entities
{
    public abstract class CmsNode
    {
        public Guid UniqueId { get; set; }
        public Guid NodeObjectType { get; set; }
        public int ContentTypeId { get; set; }
        public short Level { get; set; }
        public string Path { get; set; }
        public int ParentId { get; set; }
        public string Text { get; set; }
        public int SortOrder { get; set; }
        public int UserId { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsTrashed { get; set; }
    }
}
