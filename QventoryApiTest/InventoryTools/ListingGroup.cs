using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QventoryApiTest.InventoryTools
{
    [Serializable]
    class ListingGroup : Craftable
    {
        [Listable]
        public string Name { get; set; }
        [Listable]
        public Listing[] Listings { get; set; }

        public override void AddMaterialRequirementByID(string matId, int requiredAmount)
        {
            base.AddMaterialRequirementByID(matId, requiredAmount);
            foreach(Listing listing in Listings)
            {
                listing.AddMaterialRequirementByID(matId, requiredAmount);
            }
        }

    }
}
