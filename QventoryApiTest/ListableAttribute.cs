using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QventoryApiTest
{

    [AttributeUsage(AttributeTargets.Property)]
    class ListableAttribute : Attribute
    {
        private string name;
        //Should be printed no matter what
        private bool @default;

        public ListableAttribute(string name)
        {
            if (name == null)
                this.name = null;
            else
                this.name = name.ToLower();
        }

        public ListableAttribute() 
            : this(null)
        {
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public bool Default
        {
            get { return @default; }
            set { @default = value; }
        }

    }
}
