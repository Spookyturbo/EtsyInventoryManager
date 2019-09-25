using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Etsy;

namespace QventoryApiTest.InventoryTools
{
    //An instance of a material used for creating anything
    [Serializable]
    class Material
    {
        [Listable]
        public string ID { get; }
        [Listable(Default = true)]
        public string Name { get; set; }
        [Listable]
        public string Descriptor { get; set; }
        //Amount on hand
        [Listable]
        public int Quantity { get; set; }

        //Associations it has
        //Will include listings in listingsGroups

        public Listing[] Listings { get; set; }
        public ListingGroup[] ListingGroups { get; set; }

        //Will include products in productGroups
        public Product[] Products { get; set; }
        public ProductGroup[] ProductGroups { get; set; }

        public Material(string name) : this(name, Guid.NewGuid().ToString()) { }
        public Material(string name, string id) : this(name, 0, "", id) { }
        public Material(string name, int quantity) : this(name, quantity, "", Guid.NewGuid().ToString()) { }
        public Material(string name, int quantity, string descriptor) : this(name, quantity, descriptor, Guid.NewGuid().ToString()) { }
        public Material(string name, int quantity, string descriptor, string id)
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();

            Name = name;
            Quantity = quantity;
            ID = id;
            Descriptor = descriptor;

            InventoryManager.GetInstance().AddMaterial(this);
        }
    }
}
