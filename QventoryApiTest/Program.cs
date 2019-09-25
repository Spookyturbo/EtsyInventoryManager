using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using Etsy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QventoryApiTest.InventoryTools;

/*
 * Etsy to my knowledge will NEVER allow localhost as a callback url. It's not valid
 * as an entry for website on the developer page, which I am pretty sure
 * are the only authorized callbacks. Don't mess with it unless you plan on asking
 * on stack overflow.
 */

//Product Ids appear to be unique to atleast a shop. They are atleast for the shop I currently care about
//Scrap that, while they are for the shop I care about, the getProduct method REQUIRES both a listingID and productID given
//meaning that productID must only be guranteed unique to a listing
//ListingProduct included in transaction, so can check exclusively based off product
namespace QventoryApiTest
{
    class Program
    {
        static string baseUrl = "https://openapi.etsy.com/v2";
        static void Main(string[] args)
        {
            new Program().run();   
        }

        public void run()
        {
            IOAuthSession consumer = Oauth.CurrentEtsyConsumer();
            EtsyApi api = new EtsyApi(consumer);
            InventoryManager.Load(api);
            Cmd.Instance.Run();

            
        }

        static void PrintSerialized(object obj)
        {
            Console.WriteLine(JObject.FromObject(obj).ToString());
        }
    }
}
