using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QventoryApiTest.InventoryTools
{
    [Serializable]
    class Product : Craftable
    {
        [NonSerialized]
        public Etsy.ListingProduct data;
        [Listable(Default = true)]
        public string ListingID { get; set; }
        [Listable(Default = true)]
        public string ProductID { get; set; }
        [Listable]
        public PropertyValue[] Properties { get; set; }

        public Product(Etsy.ListingProduct data, string listingId)
        {
            this.data = data;
            ProductID = data.Product_Id;
            ListingID = listingId;

            Properties = new PropertyValue[data.Property_Values.Length];
            for (int i = 0; i < data.Property_Values.Length; i++)
            {
                Properties[i] = new PropertyValue(data.Property_Values[i]);
            }
        }

        [Serializable]
        public class PropertyValue
        {
            [Listable]
            public string PropertyID { get; set; }
            [Listable(Default = true)]
            public string Name { get; set; }
            [Listable]
            public string ValueID { get; set; }
            [Listable(Default = true)]
            public string Value { get; set; }

            public PropertyValue(Etsy.PropertyValue property)
            {
                PropertyID = property.Property_Id;
                Name = property.Property_Name;
                ValueID = property.Value_Ids[0];
                Value = property.Values[0];
            }
        }
    }
}
