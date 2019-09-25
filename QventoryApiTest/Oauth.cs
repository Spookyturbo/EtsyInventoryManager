using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;

//https://stackoverflow.com/questions/19388287/oauth-c-sharp-1-0-etsy-api
//Much help
//Using DevDefined OAuth
//https://github.com/bittercoder/DevDefined.OAuth
namespace QventoryApiTest
{
    class Oauth
    {
        private static string key;
        private static string secret;

        static Oauth()
        {
            string[] keyInfo = InventoryTools.DataSaving.ReadFromBinaryFile<string[]>("tokens.bin");
            key = keyInfo[0];
            secret = keyInfo[1];
        }

        //returns a consumer reference
        public static IOAuthSession EtsyConsumer()
        {
            //API EndPoints
            string requestUrl = "https://openapi.etsy.com/v2/oauth/request_token?scope=transactions_r%20listings_r";
            string userAuthorizeUrl = "https://www.etsy.com/oauth/signin";
            string accessUrl = "https://openapi.etsy.com/v2/oauth/access_token";

            //Dev Information
            var consumerContext = new OAuthConsumerContext
            {
                ConsumerKey = key,
                ConsumerSecret = secret,
                SignatureMethod = SignatureMethod.HmacSha1
            };

            OAuthSession session = new OAuthSession(consumerContext, requestUrl, userAuthorizeUrl, accessUrl);

            IToken requestToken = session.GetRequestToken();

            string authorizationLink = session.GetUserAuthorizationUrlForToken(requestToken, "https://bit.ly/2KQfh6x");
            
            //Redirect to the Authorization site
            System.Diagnostics.Process.Start(authorizationLink);

            Console.Write("Enter Verification String: ");
            string verification = Console.ReadLine();

            session.ExchangeRequestTokenForAccessToken(requestToken, verification);
            WriteToBinaryFile(@"saveData.txt", session.AccessToken);

            return session;
        }

        //Tokens NEVER expire
        public static IOAuthSession CurrentEtsyConsumer()
        {
            var consumerContext = new OAuthConsumerContext
            {
                ConsumerKey = key,
                ConsumerSecret = secret,
                SignatureMethod = SignatureMethod.HmacSha1
            };

            OAuthSession session = new OAuthSession(consumerContext);
            session.AccessToken = ReadFromBinaryFile<IToken>(@"saveData.txt");
            return session;
        }

        public static string RetrieveInfo(string url, IOAuthSession session)
        {
            return session.Request().Get().ForUrl(url).ToString();
        }

        //https://stackoverflow.com/questions/10337410/saving-data-to-a-file-in-c-sharp
        //Used for saving and retrieving tokens. Sorta overkill but who cares

        /// <summary>
        /// Writes the given object instance to a binary file.
        /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the XML file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the XML file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }

        /// <summary>
        /// Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the XML.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }
    }
}
