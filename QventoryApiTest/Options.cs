using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using QventoryApiTest.InventoryTools;

namespace QventoryApiTest
{

    public enum InventoryType
    {
        Listing,
        Material,
        ListingGroup,
        ProductGroup
    }

    [Verb("list", HelpText = "List specified elements.")]
    class ListOptions
    {
        [Option('v', "verbose", Default = false, HelpText = "Print all details of listing.")]
        public bool Verbose { get; set; }

        [Option('i', "include", HelpText = "What properties of element to include in listing, comma seperated")]
        public string Include { get; set; }

        [Option('l', "length", HelpText = "Set amount of elements to show")]
        public int? Length { get; set; }



        [Value(0, MetaName = "element", HelpText = "Element to list.", Required = true)]
        public InventoryType Element { get; set; }

        public int Execute()
        {
            Regex reg = new Regex(@"\w+(\[+.*?\]+)*");
            string[] includes = (string.IsNullOrEmpty(Include)) ? null : reg.Matches(Include).Cast<Match>().Select(m=>m.Value).ToArray();

            switch(Element)
            {
                case InventoryType.Listing:
                    Listing[] listings = InventoryManager.GetInstance().Listings.ToArray();
                    for (int i = 0; i < (Length ?? listings.Length); i++)
                        Cmd.List<Listing>(listings[i], Verbose, includes);
                    break;
                case InventoryType.Material:
                    Material[] materials = InventoryManager.GetInstance().Materials.ToArray();
                    for (int i = 0; i < (Length ?? materials.Length); i++)
                        Cmd.List<Material>(materials[i], Verbose, includes);
                    break;
                case InventoryType.ListingGroup:
                    ListingGroup[] listingGroups = InventoryManager.GetInstance().ListingGroups.ToArray();
                    for(int i = 0; i < (Length ?? listingGroups.Length); i++)
                    {
                        Cmd.List<ListingGroup>(listingGroups[i], Verbose, includes);
                    }
                    break;
                case InventoryType.ProductGroup:
                    ProductGroup[] productGroups = InventoryManager.GetInstance().ProductGroups.ToArray();
                    for(int i = 0; i < (Length ?? productGroups.Length); i++)
                    {
                        Cmd.List<ProductGroup>(productGroups[i], Verbose, includes);
                    }
                    break;
            }

            return 0;
        }
    }

    [Verb("exit", HelpText = "Exits the application.")]
    class ExitOptions
    {
        public int Execute()
        {
            InventoryManager.GetInstance().Save();
            System.Environment.Exit(0);
            return 0;
        }
    }

    [Verb("create", HelpText = "Creates a specified element.")]
    class CreateOptions
    {
        [Value(1, MetaName = "name", HelpText = "Name of element.", Required = true)]
        public string Name { get; set; }

        [Option('d', "description", HelpText = "Description of element.")]
        public string Description { get; set; }

        [Option("id", HelpText = "Id of element.")]
        public string Id { get; set; }

        [Option('q', "quantity", Default = 0, HelpText = "Quantity of element on hand.")]
        public int Quantity { get; set; }

        [Value(0, MetaName = "element", HelpText = "Element to create.", Required = true)]
        public InventoryType Element { get; set; }

        public int Execute()
        {
            switch(Element)
            {
                case InventoryType.Material:
                        new Material(Name, Quantity, Id, Description);
                    break;
                case InventoryType.ListingGroup:
                    break;
                case InventoryType.ProductGroup:
                    break;
            }
            return 0;
        }
    }

    [Verb("link", HelpText = "Supply two items to link together.")]
    class LinkOptions
    {

    }
}
