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
            Regex regex = new Regex(@"([-,\[\]:\w]+)|""([,:\w\s]*)""");
            Parser parser = new Parser(config => { config.HelpWriter = null; config.CaseInsensitiveEnumValues = true; });
            Console.WriteLine("Welcome to QVentory.");
            Console.WriteLine("To exit the application, please type 'exit'");
            while (true)
            {
                string input = Console.ReadLine();
                string[] args = regex.Matches(input).Cast<Match>().Select(m => (!string.IsNullOrEmpty(m.Groups[1].Value)) ? m.Groups[1].Value : m.Groups[2].Value).ToArray();
                ParserResult<object> result = parser.ParseArguments<ListOptions, ExitOptions, CreateOptions, LinkOptions>(args);
                result.MapResult(
                    (ListOptions opts) => opts.Execute(),
                    (ExitOptions opts) => opts.Execute(),
                    (CreateOptions opts) => opts.Execute(),
                    (LinkOptions opts) => opts.Execute(),
                    errs => DoError(result, errs)
                    );
            }
        }

        //Indent = how many spaces before text
        //Add dictionary material support and then we guchi and never need to touch this again, just add attributes!
        public static void List<T>(T element, bool verbose, string[] includes, int length = int.MaxValue, int indents = 0)
        {
            //Parse the includes formatting strings
            Dictionary<string, string[]> subIncludes = new Dictionary<string, string[]>();
            if (includes != null)
            {
                //Finds square bracket sections and seperate them from the primary key
                Regex reg = new Regex(@"\[(.*)\]");

                //Splits into outermost comma seperated values
                Regex reg2 = new Regex(@"\w+(\[+.*?\]+)*");
                foreach (string include in includes)
                {
                    Match match = reg.Match(include);
                    if (match != null)
                    {
                        string primaryKey = include.Remove(match.Index, match.Length);
                        string newIncludes = match.Groups[1].Value;
                        
                        //splitting based on comma doesn't work, split based off other regex expression
                        string[] sub = (string.IsNullOrEmpty(newIncludes)) ? null : reg2.Matches(newIncludes).Cast<Match>().Select(m => m.Value).ToArray();
                        subIncludes.Add(primaryKey, sub);
                    }
                    else
                    {
                        subIncludes.Add(include, null);
                    }
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
                    Type type = property.PropertyType;
                    if (type.HasElementType)
                    {
                        int i = 0;
                        foreach (object o in (System.Collections.IEnumerable)property.GetValue(element))
                        {
                            List(o, verbose, subIncludes[attribute.Name], indents: indents + 4);
                            i++;
                            if (i >= length) break;
                        }
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
            
            //Create list of attributes
            List<Tuple<PropertyInfo, ListableAttribute>> attributes = new List<Tuple<PropertyInfo, ListableAttribute>>();
            foreach (PropertyInfo property in properties)
            {
                ListableAttribute attribute = (ListableAttribute)Attribute.GetCustomAttribute(property, typeof(ListableAttribute));
                attribute.Name = attribute.Name ?? property.Name.ToLower();
                attributes.Add(new Tuple<PropertyInfo, ListableAttribute>(property, attribute));
            }

            //Do defaults then take out of list
            Tuple<PropertyInfo, ListableAttribute>[] defaults = attributes.FindAll(t => t.Item2.Default).ToArray();
            foreach(var def in defaults)
            {
                listElements(def);
                attributes.Remove(def);
            }

            //Iterate over desired list
            //I am iterating over keys instead of properties because I want things to be listed in the order they are formated in the includes string
            foreach (string key in subIncludes.Keys)
            {
                Tuple<PropertyInfo, ListableAttribute> propertyAttribute = attributes.Find(t => t.Item2.Name.Equals(key));
                if (propertyAttribute != null)
                {
                    ListableAttribute attribute = propertyAttribute.Item2;
                    //Use method supplied in attribute
                    if(attribute.CallbackClass != null)
                    {
                        MethodInfo methodInfo = attribute.CallbackClass.GetMethod(attribute.CallbackMethodName, BindingFlags.Public | BindingFlags.Static);
                        if (methodInfo != null)
                        {
                            //These parameters should work for any method supplied in an attribute, it is just required
                            //I should probably add a check to make sure, but I don't want to so I just won't mess it up for now
                            //since I am the only one using this
                            object[] parameters = {element, propertyAttribute, subIncludes[attribute.Name], indents};
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
        }

        private int DoError(ParserResult<object> result, IEnumerable<Error> errs)
        {
            Error err = errs.First();
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
