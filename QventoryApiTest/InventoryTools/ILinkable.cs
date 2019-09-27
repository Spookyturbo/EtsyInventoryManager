using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QventoryApiTest.InventoryTools
{
    interface ILinkable
    {
        void Link<T>(T element);
        void Delink<T>(T element);
    }
}
