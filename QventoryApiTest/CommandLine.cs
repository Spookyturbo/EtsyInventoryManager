﻿using System;
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
            Regex regex = new Regex(@"([-,\[\]:\w]+)|""(.*?)""");
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

        //Indent = how many spaces before text
        //Add dictionary material support and then we guchi and never need to touch this again, just add attributes!
        public static void List<T>(T element, bool verbose, string[] includes, int length = int.MaxValue, int indents = 0)
        {
            //Parse the includes formatting strings. Key = Current Primary element. Tuple<lenofpelementifenumerable, subincludes>
            Dictionary<string, Tuple<string[], int>> subIncludes = new Dictionary<string, Tuple<string[], int>>();
            if (includes != null)
            {
                //Finds square bracket sections and seperate them from the primary key
                Regex reg = new Regex(@"\[(.*)\]");

                //Parse include strings
                foreach (string include in includes)
                {
                    Match match = reg.Match(include);
                    if (match != null)
                    {
                        string primaryKey = include.Remove(match.Index, match.Length);
                        string[] primaryInfo = primaryKey.Split(':');
                        primaryKey = primaryInfo[0];
                        int sublength = (primaryInfo.Length > 1) ? Int32.Parse(primaryInfo[1]) : length;
                        string newIncludes = match.Groups[1].Value;

                        //splitting based on comma doesn't work, split based off other regex expression
                        string[] sub = (string.IsNullOrEmpty(newIncludes)) ? null : ParseSearchString(newIncludes);
                        Tuple<string[], int> keyInfo = new Tuple<string[], int>(sub, sublength);
                        subIncludes.Add(primaryKey, keyInfo);
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
                        List(enumerator.Current, verbose, subIncludes[attribute.Name].Item1, indents: indents + 4);
                        //If there is another item to print, put a spacer line in between them
                        if (hasNext && i != length - 1)
                        {
                            Console.WriteLine();
                        }
                        //Check if list is over
                        i++;
                        if (i >= length) break;
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
