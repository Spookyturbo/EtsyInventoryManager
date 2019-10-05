using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace QventoryApiTest
{
    class Cmd
    {
        private static readonly Cmd instance = new Cmd();
        public static Cmd Instance
        {
            get
            {
                return instance;
            }
        }
        static Cmd() { }
        private Cmd() { }

        public void Run()
        {
            Regex regex = new Regex(@"([-,=><\[\]:\w]+)|""(.*?)""");
            Parser parser = new Parser(config => { config.HelpWriter = null; config.CaseInsensitiveEnumValues = true; });
            Console.WriteLine("Welcome to QVentory.");
            Console.WriteLine("To exit the application, please type 'exit'");
            while (true)
            {
                string input = Console.ReadLine();
                string[] args = regex.Matches(input).Cast<Match>().Select(m => (!string.IsNullOrEmpty(m.Groups[1].Value)) ? m.Groups[1].Value : m.Groups[2].Value).ToArray();
                ParserResult<object> result = parser.ParseArguments<ListOptions, ExitOptions, CreateOptions, DeleteOptions, LinkOptions>(args);
                result.MapResult(
                    (ListOptions opts) => opts.Execute(),
                    (ExitOptions opts) => opts.Execute(),
                    (CreateOptions opts) => opts.Execute(),
                    (DeleteOptions opts) => opts.Execute(),
                    (LinkOptions opts) => opts.Execute(),
                    errs => DoError(result, errs)
                    );
            }
        }

        //This function is pretty recursive
        //Indent = how many spaces before text
        //Add dictionary material support and then we guchi and never need to touch this again, just add attributes!
        //Example includes: listings[id],listinggroups
        public static bool List<T>(T element, bool verbose, string[] includes, string[] filterStrings = null, int length = int.MaxValue, int indents = 0)
        {
            //Parse the includes formatting strings. Key = Current Primary element. Tuple<lenofpelementifenumerable, subincludes>
            //Format: (currentElementName, (subIncludes, lenToList, filterString)
            Dictionary<string, Tuple<string[], int, string[]>> subIncludes = new Dictionary<string, Tuple<string[], int, string[]>>();
            if (includes != null)
            {
                //Finds square bracket sections and seperate them from the primary key
                Regex reg = new Regex(@"\[(.*)\]");

                //Parse include strings
                for(int i = 0; i < includes.Length; i++)
                {
                    Match match = reg.Match(includes[i]);
                    string primaryKey = includes[i].Remove(match.Index, match.Length);

                    string[] primaryInfo = primaryKey.Split(':');
                    primaryKey = primaryInfo[0];

                    int sublength = (primaryInfo.Length > 1) ? Int32.Parse(primaryInfo[1]) : length;
                    string newIncludes = match.Groups[1].Value;
                    string filter = null;
                    if(filterStrings != null)
                    {
                        filter = filterStrings[i];
                    }

                    //splitting based on comma doesn't work, split based off other regex expression
                    
                    string[] sub = (string.IsNullOrEmpty(newIncludes)) ? null : ParseSearchString(newIncludes);
                    Tuple<string[], int, string[]> keyInfo = new Tuple<string[], int, string[]>(sub, sublength, ParseSearchString(filter));
                    subIncludes.Add(primaryKey, keyInfo);
                }
            }

            Action<Tuple<PropertyInfo, ListableAttribute>> listElements = (propertyAttribute) =>
            {
                PropertyInfo property = propertyAttribute.Item1;
                ListableAttribute attribute = propertyAttribute.Item2;
                //If included, check if ienumerable or not
                bool isEnumerable = typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string);
                if (isEnumerable)
                {
                    //Set length for this
                    length = subIncludes[attribute.Name].Item2;
                    //Print header
                    string title = string.Format("{0}:", attribute.Name);
                    Console.WriteLine(title.PadLeft(title.Length + indents));
                    //Manage when stop listing
                    int i = 0;
                    System.Collections.IEnumerator enumerator = ((System.Collections.IEnumerable)(property.GetValue(element))).GetEnumerator();
                    bool hasNext = enumerator.MoveNext();
                    while (hasNext)
                    {
                        if (Filter(enumerator.Current, subIncludes[attribute.Name].Item1))
                        {
                            List(enumerator.Current, verbose, subIncludes[attribute.Name].Item1, indents: indents + 4);
                            //If there is another item to print, put a spacer line in between them
                            if (hasNext && i != length - 1)
                            {
                                Console.WriteLine();
                            }
                            //Check if list is over
                            i++;
                            if (i >= length) break;
                        }
                        hasNext = enumerator.MoveNext();
                    }
                }
                else
                {
                    //Works fine until we get a non array non basic data type passed in (Like Product or Material)
                    string toPrint = string.Format("{0}: {1}\n", attribute.Name, property.GetValue(element));
                    toPrint = toPrint.PadLeft(toPrint.Length + indents);
                    Console.Write(toPrint);
                }
            };


            //Get properties that are listable
            MemberInfo[] properties = element.GetType().GetProperties();
            properties = properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(ListableAttribute))).ToArray();

            //Create list of attributes for those properties
            List<Tuple<PropertyInfo, ListableAttribute>> attributes = new List<Tuple<PropertyInfo, ListableAttribute>>();
            foreach (PropertyInfo property in properties)
            {
                ListableAttribute attribute = (ListableAttribute)Attribute.GetCustomAttribute(property, typeof(ListableAttribute));
                attribute.Name = attribute.Name ?? property.Name.ToLower();
                attributes.Add(new Tuple<PropertyInfo, ListableAttribute>(property, attribute));
            }

            //Do default properties then take out of list so as to not be repeated
            Tuple<PropertyInfo, ListableAttribute>[] defaults = attributes.FindAll(t => t.Item2.Default).ToArray();
            foreach (var def in defaults)
            {
                listElements(def);
                attributes.Remove(def);
            }

            //Iterate over desired 
            //I am iterating over keys instead of properties because I want things to be listed in the order they are formated in the includes string
            foreach (string key in subIncludes.Keys)
            {
                Tuple<PropertyInfo, ListableAttribute> propertyAttribute = attributes.Find(t => t.Item2.Name.Equals(key));
                if (propertyAttribute != null)
                {
                    ListableAttribute attribute = propertyAttribute.Item2;
                    //Use method supplied in attribute
                    if (attribute.CallbackClass != null)
                    {
                        MethodInfo methodInfo = attribute.CallbackClass.GetMethod(attribute.CallbackMethodName, BindingFlags.Public | BindingFlags.Static);
                        if (methodInfo != null)
                        {
                            //These parameters should work for any method supplied in an attribute, it is just required
                            //I should probably add a check to make sure, but I don't want to so I just won't mess it up for now
                            //since I am the only one using this
                            object[] parameters = { element, propertyAttribute, subIncludes[attribute.Name], indents };
                            methodInfo = methodInfo.MakeGenericMethod(element.GetType());
                            methodInfo.Invoke(null, parameters);
                        }
                        else
                        {
                            Console.WriteLine("Supplied callback method in class {0} name {1} does not exist or is not public and static", attribute.CallbackClass.GetType().ToString(), attribute.CallbackMethodName);
                        }
                    }
                    //Use this default list implementation
                    else
                    {
                        listElements(propertyAttribute);
                    }
                }
            }

            return false;
        }

        //Example filter string listings[id=x] where type T is ListingGroup
        //id=x for type t = listings
        //More complicated filter string would be listings[products[productid=x]]
        //Even more complicated filter listings[id=x,products[productid=x]]
        //Element is an instance of T
        //Returns true only if the given element is true for every check. If a check is against an element contained in an 
        //enumerable in T, returns true if atleast one is true
        public static bool Filter<T>(T element, string[] filterElements)
        {
            //Name of property to be compared as key, with a tuple of the comparison type and what it is being compared to or if unknown the sub filter
            Dictionary<string, Tuple<char, string>> comparisons = new Dictionary<string, Tuple<char, string>>();
            char[] validComparisons = { '=', '>', '<' };

            if (filterElements == null)
                return true;
            filterElements = filterElements.Where(s => s.IndexOfAny(validComparisons) > 0).ToArray();
            if(filterElements.Length == 0)
                return true;

            Regex reg = new Regex(@"\[(.*)\]");

            //Pull out valid comparisons from the filterElements parsed
            foreach (string filterElement in filterElements)
            {
                //products[id=x]
                Match match = reg.Match(filterElement);
                if(match.Success)
                {
                    string primaryKey = filterElement.Remove(match.Index, match.Length);
                    string subFilter = match.Groups[1].Value;
                    Tuple<char, string> mockComparison = new Tuple<char, string>(' ', subFilter);
                    comparisons.Add(primaryKey, mockComparison);
                }
                else
                {
                    //Safe to do a comparison on this item, does not require going deeper
                    int index = filterElement.IndexOfAny(validComparisons);
                    char comparator = filterElement[index];
                    string[] comparisonInfo = filterElement.Split(comparator);
                    //Will still compare if the comparison is malformated, just it will default to true
                    if (comparisonInfo.Length > 1)
                    {
                        Tuple<char, string> comparison = new Tuple<char, string>(comparator, comparisonInfo[1]);
                        comparisons.Add(comparisonInfo[0], comparison);
                    }
                    else
                    {
                        Console.WriteLine("WARNING: Malformated Comparison");
                    }
                }
            }

            //Get properties for all elements in our desired comparisons
            PropertyInfo[] properties = element.GetType().GetProperties();
            properties = properties.Where(p => comparisons.Keys.Contains(p.Name.ToLower())).ToArray();

            //Check each property to see if it succeeds in its comparison
            foreach (PropertyInfo property in properties)
            {

                bool isEnumerable = typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string);
                //Realistically there should be an if class object in here since those can't be filtered, however currently all of those
                //are stored as ienumerable and I have no intent for them not to be stored that way, so for now this is fine
                if (isEnumerable)
                {
                    //Get enumerator for this property
                    System.Collections.IEnumerator enumerator = ((System.Collections.IEnumerable)(property.GetValue(element))).GetEnumerator();
                    bool anyTrue = false;
                    while (enumerator.MoveNext())
                    {
                        //If any element in enumerable returns true, count as true in filter
                        if (Filter(enumerator.Current, ParseSearchString(comparisons[property.Name.ToLower()].Item2)))
                        {
                            anyTrue = true;
                            break;
                        }
                    }
                    if (!anyTrue)
                        return false;
                }
                else
                {
                    //Can do a direct comparison to the property
                    string comparison = comparisons[property.Name.ToLower()].Item2;
                    object propVal = property.GetValue(element);
                    int comp;
                    switch(comparisons[property.Name.ToLower()].Item1)
                    {
                        case '=':
                            if (!propVal.Equals(comparison))
                                return false;
                            break;
                        case '>':
                            comp = Int32.Parse(comparison);
                            if (!((int) propVal > comp))
                                return false;
                            break;
                        case '<':
                            comp = Int32.Parse(comparison);
                            if (!((int)propVal < comp))
                                return false;
                            break;
                        default:
                            Console.WriteLine("This is literally impossible, I validate these so this is literally impossible");
                            break;
                    }
                }
            }

            //It never failed a check hurray it is valid!
            return true;
        }

        //Regex was weird and not doing what I wanted, this was quicker to make, and my understanding
        //Is regex is not good for nested organization
        public static string[] ParseSearchString(string searchString)
        {
            List<string> test = new List<string>();
            int nestLevel = 0;
            int lastStart = 0;
            for (int i = 0; i < searchString.Length; i++)
            {
                char c = searchString[i];
                if (c.Equals('['))
                    nestLevel++;
                else if (c.Equals(']'))
                    nestLevel--;
                if (c.Equals(',') && nestLevel == 0)
                {
                    test.Add(searchString.Substring(lastStart, i - lastStart).ToLower());
                    lastStart = i + 1;
                }
            }
            if (lastStart < searchString.Length)
                test.Add(searchString.Substring(lastStart, searchString.Length - lastStart).ToLower());

            return test.ToArray();
        }

        //Returns a dictionary that turns x[y...] into { x : [y, ..., ...] }
        public static Dictionary<string, string[]> ParseSearchStringToDictionary(string searchString)
        {
            Dictionary<string, string[]> searchDictionary = new Dictionary<string, string[]>();
            string[] commaSeperated = ParseSearchString(searchString);
            Regex reg = new Regex(@"\[(.*)\]");

            foreach (string search in commaSeperated)
            {

                Match match = reg.Match(search);
                if(match.Success)
                {
                    string primaryKey = search.Remove(match.Index, match.Length);
                    string newIncludes = match.Groups[1].Value;

                    searchDictionary.Add(primaryKey, ParseSearchString(newIncludes));
                }
                else
                {
                    searchDictionary.Add(search, null);
                }
            }

            return searchDictionary;
        }

        private int DoError(ParserResult<object> result, IEnumerable<Error> errs)
        {
            Error err = errs.First();
            if (err is NoVerbSelectedError)
                return 1;
            var helpText = HelpText.AutoBuild(result);
            helpText.AutoHelp = true;
            helpText.AddEnumValuesToHelpText = true;
            if (err.Tag != ErrorType.HelpVerbRequestedError)
                helpText.AddOptions(result);
            Console.Error.WriteLine(helpText);
            return 1;
        }
    }
}
