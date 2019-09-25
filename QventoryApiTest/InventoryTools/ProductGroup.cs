using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QventoryApiTest.InventoryTools
{
    [Serializable]
    class ProductGroup : Craftable
    {
        [Listable]
        public string Name { get; set; }
        [Listable]
        public Product[] Products { get; set; }

        public ProductGroup(string name)
        {
            Name = name;
        }

        public override void AddMaterialRequirementByID(string matId, int requiredAmount)
        {
            base.AddMaterialRequirementByID(matId, requiredAmount);
            foreach(Product product in Products)
            {
                product.AddMaterialRequirementByID(matId, requiredAmount);
            }
        }
    }
}
