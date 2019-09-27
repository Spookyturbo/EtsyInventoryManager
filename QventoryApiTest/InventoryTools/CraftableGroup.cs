using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QventoryApiTest.InventoryTools
{
    [Serializable]
    class CraftableGroup<T> : Craftable, ILinkable, IListable where T : Craftable
    {
        [Listable(Default = true)]
        public string Name { get; set; }
        [Listable]
        public List<T> Items { get; set; }

        public CraftableGroup(string name)
        {
            Name = name;
            Items = new List<T>();
            Materials = new Dictionary<string, int>();
        }

        public override void Link<U>(U element)
        {
            base.Link(element);
            if(element is T e)
            {
                AddItem(e);
                return;
            }
        }

        public override void Delink<U>(U element)
        {
            base.Delink(element);
            if(element is T e)
            {
                RemoveItem(e);
            }
        }

        public override void AddMaterialRequirementByID(string matId, int requiredAmount)
        {
            base.AddMaterialRequirementByID(matId, requiredAmount);
            foreach(T item in Items)
            {
                item.AddMaterialRequirementByID(matId, requiredAmount);
            }
        }

        public void AddItem(T item)
        {
            if(!Items.Contains(item))
            {
                Items.Add(item);
                foreach(KeyValuePair<string, int> matInfo in Materials)
                {
                    item.AddMaterialRequirementByID(matInfo.Key, matInfo.Value);
                }
            }
        }

        public void RemoveItem(T item)
        {
            Items.Remove(item);
        }

        public void RemoveItem(Predicate<T> match)
        {
            Items.Remove(Items.Find(match));
        }

    }
}
