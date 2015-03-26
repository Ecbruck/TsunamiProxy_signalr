using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNet.SignalR.Client;

namespace TsunamiProxy.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            new ProxyServerApp();

            Console.ReadLine();
        }
    }
}
