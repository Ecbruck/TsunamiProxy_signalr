using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsunamiProxy.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            new ProxyClientApp(13000);
            Console.ReadLine();
        }

    }
}
