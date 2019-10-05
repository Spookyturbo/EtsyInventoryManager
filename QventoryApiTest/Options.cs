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
        Product,
        Material,
        ListingGroup,
        ProductGroup
    }

    [Verb("list", HelpText = "List specified elements.")]
    class ListOptions
    {
        [Option('v', "verbose", Default = false, HelpText = "Print all details of listing.")]
        public bool Verbose { get; set; }

        [Option('a', "all", Default = false, HelpText = "If true, will list all info in includes if one thing in filter is true.")]
        public bool FilterAll { get; set; }

        [Option('i', "include", HelpText = "What properties of element to include in listing, comma seperated")]
        public string Include { get; set; }

        [Option('l', "length", HelpText = "Set amount of elements to show")]
        public int? Length { get; set; }


        //[Value(0, MetaName = "element", HelpText = "Element to list.", Required = true)]
        //public InventoryType Element { get; set; }
        [Value(0, MetaName = "element", HelpText = "Search string, also specifies includes, lengths, and filters.", Required = true)]
        public string listString { get; set; }

        public int Execute()
        {
            string[] includes = Cmd.ParseSearchString(listString);

            Cmd.List(InventoryManager.GetInstance(), Verbose, includes, length: Length ?? int.MaxValue);

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

        [Option('q', "quantity", Default = 0, HelpText = "Quantity of element on hand (For material)")]
        public int Quantity { get; set; }

        [Value(0, MetaName = "element", HelpText = "Element to create.", Required = true)]
        public InventoryType Element { get; set; }

        public int Execute()
        {
            switch(Element)
            {
                case InventoryType.Material:
                    InventoryManager.GetInstance().AddMaterial(new Material(Name, Quantity, Description, Id));
                    break;
                case InventoryType.ListingGroup:
                    InventoryManager.GetInstance().AddListingGroup(new CraftableGroup<Listing>(Name));
                    break;
                case InventoryType.ProductGroup:
                    InventoryManager.GetInstance().AddProductGroup(new CraftableGroup<Product>(Name));
                    break;
                default:
                    Console.WriteLine("\nThat is not a creatable type.");
                    return 1;
            }
            return 0;
        }
    }

    [Verb("delete", HelpText = "Deletes a specified element.")]
    class DeleteOptions
    {
        [Value(1, MetaName = "name", HelpText = "Name of element.", Required = true)]
        public string Name { get; set; }

        [Option('i', "id", HelpText = "Using id or name. Default name", Default = false)]
        public bool UsingId { get; set; }

        [Value(0, MetaName = "element", HelpText = "Element type delete.", Required = true)]
        public InventoryType Element { get; set; }

        public int Execute()
        {
            switch (Element)
            {
                case InventoryType.Material:
                    Material material = InventoryManager.GetInstance().GetMaterial(m => (UsingId) ? m.ID.Equals(Name) : m.Name.Equals(Name));
                    if (material == null)
                        return 1;
                    InventoryManager.GetInstance().RemoveMaterial(material);
                    break;
                case InventoryType.ListingGroup:
                    InventoryManager.GetInstance().RemoveListingGroup(Name);
                    break;
                case InventoryType.ProductGroup:
                    InventoryManager.GetInstance().RemoveProductGroup(Name);
                    break;
                default:
                    Console.WriteLine("\nThat is not a deletable type.");
                    return 1;
            }
            return 0;
        }
    }

    //Handles linking and delinking
    [Verb("link", HelpText = "Supply two items to link together.")]
    class LinkOptions
    {

        [Option('i', "id", Default = false, HelpText = "Passing in ID or name to link. Name by default")]
        public bool usingId { get;set; }

        [Option("remove", Default = false, HelpText = "If true, delinks instead of linking.")]
        public bool Remove { get; set; }

        [Option('s', "sender", Required = true, HelpText = "Type of what is being linked.")]
        public InventoryType type1 { get; set; }

        [Value(0, MetaName = "Source", Required = true, HelpText = "Source ID or Name(default)")]
        public string identifier1 { get; set; }

        [Option('r', "receiver", Required = true, HelpText = "Type of what is being linked to.")]
        public InventoryType type2 { get; set; }

        [Value(1, MetaName = "Destination", Required = true, HelpText = "Destination ID or Name(default)")]
        public string identifier2 { get; set; }

        public int Execute()
        {
            InventoryManager manager = InventoryManager.GetInstance();
            ILinkable receiver = null;
            object sender = null;
            switch(type2)
            { 
                case InventoryType.Listing:
                    receiver = manager.GetListing(l => (usingId) ? l.ID.Equals(identifier2) : l.Name.Equals(identifier2));
                    break;
                case InventoryType.ListingGroup:
                    receiver = manager.GetListingGroup(lg => lg.Name.Equals(identifier2));
                    break;
                case InventoryType.ProductGroup:
                    receiver = manager.GetProductGroup(pg => pg.Name.Equals(identifier2));
                    break;
                case InventoryType.Product:
                    string[] productIds = identifier2.Split(',');
                    if (productIds.Length != 2) Console.WriteLine("\nProduct information requires comma seperate listing identifier and product identifier");
                    receiver = manager.GetProduct(productIds[0], productIds[1]);
                    break;
                default:
                    Console.WriteLine("\nReceiving type can not receive anything");
                    return 1;
            }

            if(receiver == null)
            {
                Console.WriteLine("\nCould not find requested receiver");
                return 1;
            }

            switch (type1)
            {
                case InventoryType.Listing:
                    sender = manager.GetListing(l => (usingId) ? l.ID.Equals(identifier1) : l.Name.Equals(identifier1));
                    break;
                case InventoryType.Material:
                    sender = manager.GetMaterial(m => (usingId) ? m.ID.Equals(identifier1) : m.Name.Equals(identifier1));
                    break;
                case InventoryType.Product:
                    string[] productIds = identifier1.Split(',');
                    if (productIds.Length != 2)
                    {
                        Console.WriteLine("\nProduct information requires comma seperate listing identifier and product identifier");
                        return 1;
                    }
                    sender = manager.GetProduct(productIds[0], productIds[1]);
                    break;
                default:
                    Console.WriteLine("\nLinking type can not be linked to anything.");
                    return 1;
            }

            if (sender == null)
            {
                Console.WriteLine("\nCould not find requested object to be linked.");
                return 1;
            }

            if(!Remove)
            {
                receiver.Link(sender);
            }
            else
            {
                receiver.Delink(sender);
            }
            return 0;
        }
    }
}
