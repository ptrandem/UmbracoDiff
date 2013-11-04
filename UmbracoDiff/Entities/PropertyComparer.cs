using System.Collections.Generic;
using UmbracoCompare;

namespace UmbracoDiff.Entities
{
    public class PropertyComparer: IEqualityComparer<PropertyType>
    {
        public bool Equals(PropertyType x, PropertyType y)
        {
            return x.Alias == y.Alias
                   && x.Name == y.Name
                   && x.Description == y.Description
                   && x.SortOrder == y.SortOrder
                   && x.ValidationRegEx == y.ValidationRegEx
                   && x.Mandatory == y.Mandatory;
        }

        public int GetHashCode(PropertyType obj)
        {
            return obj.GetHashCode();
        }
    }
}
