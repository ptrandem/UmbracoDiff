using System.Collections.Generic;
using System.Linq;
using UmbracoCompare;

namespace UmbracoDiff.Entities
{
    public class DocType : CmsNode
    {
        public DocType()
        {
            Properties = new List<PropertyType>();
        }

        public List<PropertyType> Properties { get; set; }

        public bool PropertiesAreEqual(DocType otherType)
        {
            return Properties.SequenceEqual(otherType.Properties, new PropertyComparer());
        }
    }
}
