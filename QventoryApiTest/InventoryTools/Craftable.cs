using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QventoryApiTest.InventoryTools
{
    //Class that is extended for if the object is "Craftable" AKA has materials associated with it
    [Serializable]
    abstract class Craftable
    {
        [Listable]
        public Dictionary<string, int> Materials { get; set; }

        public void AddMaterialRequirementBy(Material mat, int requiredAmount)
        {
            AddMaterialRequirementByID(mat.ID, requiredAmount);
        }

        public virtual void AddMaterialRequirementByID(string matId, int requiredAmount)
        {
            if (Materials.ContainsKey(matId))
            {
                Materials[matId] = requiredAmount;
            }
            else
            {
                Materials.Add(matId, requiredAmount);
            }
        }
    }
}
