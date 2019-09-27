using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QventoryApiTest.InventoryTools
{
    //Class that is extended for if the object is "Craftable" AKA has materials associated with it
    [Serializable]
    abstract class Craftable : ILinkable
    {
        [Listable(typeof(Craftable), "ListMaterials")]
        public Dictionary<string, int> Materials { get; set; }

        public void AddMaterialRequirement(Material mat, int requiredAmount)
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

        public void RemoveMaterial(string id)
        {
            Materials.Remove(id);
        }


        //For linking to materials
        public virtual void Link<T>(T element)
        {
            if(element is Material m)
            {
                AddMaterialRequirement(m, 0);
                return;
            }
        }

        public virtual void Delink<T>(T element)
        {
            if(element is Material m)
            {
                RemoveMaterial(m.ID);
                return;
            }
        }

        //Materials are stored on objects as Dictionaries of mat id and quantity, 
        //this converts to list of materials that can be properly listed with the List method
        public static void ListMaterials<T>(T element, Tuple<PropertyInfo, ListableAttribute> propertyAttribute, string[] includes, int indents)
        {
            Dictionary<string, int> mats = (Dictionary<string, int>) propertyAttribute.Item1.GetValue(element);
            IEnumerable<Material> materials = InventoryManager.GetInstance().Materials.Where(m => mats.Keys.Contains(m.ID));
            foreach (Material mat in materials)
            {
                Cmd.List(mat, false, includes, indents: indents);
            }
        }
    }
}
