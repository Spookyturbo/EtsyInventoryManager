using Etsy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QventoryApiTest.InventoryTools
{
    [Serializable]
    class InventoryManager
    {
        static string savePath = @"inventoryManager.txt";
        static InventoryManager instance = null;

        [Listable]
        public List<Material> Materials { get; set; }
        [Listable]
        public List<Listing> Listings { get; set; }

        [Listable]
        public List<CraftableGroup<Listing>> ListingGroups { get; set; }
        [Listable]
        public List<CraftableGroup<Product>> ProductGroups { get; set; }

        private InventoryManager()
        {
            Materials = new List<Material>();
            Listings = new List<Listing>();

            ListingGroups = new List<CraftableGroup<Listing>>();
            ProductGroups = new List<CraftableGroup<Product>>();
        }

        public static InventoryManager GetInstance()
        {
            if (instance == null)
                instance = new InventoryManager();
            return instance;
        }

        public void Save()
        {
            DataSaving.WriteToBinaryFile(savePath, this);
        }

        public static void Load(EtsyApi api)
        {
            if (File.Exists(savePath))
            {
                instance = DataSaving.ReadFromBinaryFile<InventoryManager>(savePath);
            }
            else
            {
                instance = new InventoryManager();
                instance.AddAllListings(api);
            }
        }


        public Material GetMaterial(Predicate<Material> match)
        {
            Material[] materials = Materials.FindAll(match).ToArray();
            if (materials.Length == 1)
                return materials[0];
            else
                Console.WriteLine("Error: {0} materials match that identifier", materials.Length);
            return null;
        }

        public void AddMaterial(Material mat)
        {
            Material duplicate = Materials.Find(m => m.ID.Equals(mat.ID));
            if(duplicate != null)
            {
                Console.WriteLine("Material not added. Trying to add mat with duplicate ID.\nConflicting Materials:\nName: {0} ID: {1}\nName: {2} ID: {3}", mat.Name, mat.ID, duplicate.Name, duplicate.ID);
                return;
            }
            Materials.Add(mat);
        }

        public void RemoveMaterial(Material mat)
        {
            RemoveMaterial(mat.ID);
        }

        public void RemoveMaterial(string matID)
        {
            int index = Materials.FindIndex(m => m.ID.Equals(matID));
            if (index < 0)
            {
                Console.WriteLine("A material with that name does not exist.");
                return;
            }
            Materials.RemoveAt(index);
        }

        public Product GetProduct(string listingId, string productId)
        {
            Product product = Listings.Find(l => l.ID.Equals(listingId) || l.Name.Equals(listingId)).Products.Where(p=>p.ProductID.Equals(productId)).First();
            if (product == null)
                Console.WriteLine("Product with listing identifier {0} and product id {1} does not exist.", listingId, productId);
            return product;
        }

        public Listing GetListing(Predicate<Listing> match)
        {
            return Listings.Find(match);
        }

        public void AddListing(Listing listing)
        {
            Listing duplicate = Listings.Find(li => li.ID.Equals(listing.ID));
            if(duplicate != null)
            {
                Console.WriteLine("Listing not added. Trying to add listing with duplicate ID. Did you mean to update?");
                Console.WriteLine("Conflicting Materials:\nName: {0} ID: {1}\nName: {2} ID: {3}", listing.Name, listing.ID, duplicate.Name, duplicate.ID);
                return;
            }
            Listings.Add(listing);
        }

        public void RemoveListing(Listing listing)
        {
            RemoveListing(listing.ID);
        }

        public void RemoveListing(string listingId)
        {
            int index = Listings.FindIndex(li => li.ID.Equals(listingId));
            if (index < 0)
                return;
            Listings.RemoveAt(index);
        }

        private void AddAllListings(EtsyApi api)
        {
            int totalListings = api.findAllShopListingsActiveCount("__SELF__");

            for (int offset = 0; offset < totalListings; offset += 100)
            {

                Etsy.Listing[] listings = api.findAllShopListingsActive("__SELF__", limit: 100, offset: offset, fields: "listing_id");
                string listingIds = "";
                foreach (Etsy.Listing listing in listings)
                {
                    listingIds += listing.Listing_Id.ToString() + ",";
                }
                //remove last comma
                listingIds = listingIds.Remove(listingIds.Length - 1, 1);

                listings = api.getListing(listingIds, includes: "Inventory", fields: "title,listing_id");
                foreach (Etsy.Listing listing in listings)
                {
                    Listing list = new Listing(listing);
                    InventoryManager.GetInstance().AddListing(list);
                }
            }
        }

        public CraftableGroup<Listing> GetListingGroup(Predicate<CraftableGroup<Listing>> match)
        {
            return ListingGroups.Find(match);
        }

        public void AddListingGroup(CraftableGroup<Listing> group)
        {
            CraftableGroup<Listing> duplicate = ListingGroups.Find(li => li.Name.Equals(group.Name));
            if (duplicate != null)
            {
                Console.WriteLine("ListingGroup not added. Trying to add ListingGroup with duplicate Name. Did you mean to update?");
                Console.WriteLine("Conflicting Groups:\nName: {0}\nName: {2}", group.Name, duplicate.Name);
                return;
            }
            ListingGroups.Add(group);
        }

        public void RemoveListingGroup(CraftableGroup<Listing> group)
        {
            ListingGroups.Remove(group);
        }

        public void RemoveListingGroup(string name)
        {
            int index = ListingGroups.FindIndex(li => li.Name.Equals(name));
            ListingGroups.RemoveAt(index);
        }

        public CraftableGroup<Product> GetProductGroup(Predicate<CraftableGroup<Product>> match)
        {
            return ProductGroups.Find(match);
        }

        public void AddProductGroup(CraftableGroup<Product> group)
        {
            CraftableGroup<Product> duplicate = ProductGroups.Find(pg => pg.Name.Equals(group.Name));
            if (duplicate != null)
            {
                Console.WriteLine("ProductGroup not added. Trying to add ProductGroup with duplicate Name. Did you mean to update?");
                Console.WriteLine("Conflicting Groups:\nName: {0}\nName: {2}", group.Name, duplicate.Name);
                return;
            }
            ProductGroups.Add(group);
        }

        public void RemoveProductGroup(CraftableGroup<Product> group)
        {
            ProductGroups.Remove(group);
        }

        public void RemoveProductGroup(string name)
        {
            int index = ProductGroups.FindIndex(pg => pg.Name.Equals(name));
            ListingGroups.RemoveAt(index);
        }
    }
}
