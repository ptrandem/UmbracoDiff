namespace UmbracoDiff.Entities
{
    public class PropertyType
    {
        public bool Mandatory { get; set; }
        public int Id { get; set; }
        public int SortOrder { get; set; }
        public string Alias { get; set; }
        public string Name { get; set; }
        public string ValidationRegEx { get; set; }
        public int DataTypeId { get; set; }
        public int ContentTypeId {get; set; }
        public string Description {get; set; }
    }
}
