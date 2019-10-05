using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Etsy;

namespace QventoryApiTest.InventoryTools
{
    //Wrapper class for the Etsy.Listing object
    [Serializable]
    class Listing : Craftable, IListable
    {
        [NonSerialized]
        Etsy.Listing data;
        [Listable(Default = true)]
        public string Name { get; set; }
        [Listable]
        public string ID { get; set; }
        public string Image { get; set; }
        [Listable]
        public Product[] Products { get; set; }

        public Listing(EtsyApi api, string listingId) 
            : this(api.getListing(listingId, includes: "Inventory")[0]) { }
        public Listing(Etsy.Listing data)
        {
            Materials = new Dictionary<string, int>();
            this.data = data;
            Name = data.Title;
            ID = data.Listing_Id.ToString();
            if (data.MainImage != null)
                Image = data.MainImage.Url_Fullxfull;

            //Add products
            if(data.Inventory != null)
            {
                Products = new Product[data.Inventory.Products.Length];
                for(int i = 0; i < Products.Length; i++)
                {
                    Products[i] = new Product(data.Inventory.Products[i], ID);
                }
            }
            else
            {
                Products = null;
            }
        }

        public override void AddMaterialRequirementByID(string matId, int requiredAmount)
        {
            base.AddMaterialRequirementByID(matId, requiredAmount);
            if (Products == null)
            {
                Console.WriteLine("Products is null for listing {0} and that is a problem", ID);
                return;
            }

            foreach(Product product in Products)
            {
                product.AddMaterialRequirementByID(matId, requiredAmount);
            }
        }

        public static Listing GetListing(EtsyApi api, string listingId)
        {
            Etsy.Listing data = api.getListing(listingId, includes: "Inventory")[0];
            return new Listing(data);
        }
    }
}
