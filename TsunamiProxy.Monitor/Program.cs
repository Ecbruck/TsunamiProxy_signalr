using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsunamiProxy.Monitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var hubCon = new HubConnection("http://tsunamiproxy.chinacloudsites.cn");
            var hub = hubCon.CreateHubProxy("ProxyHub");
            hubCon.Start().Wait();

            while(true)
            {
                Task.Delay(2000).Wait();
                Console.WriteLine("Tunnels:");
                foreach (var s in hub.Invoke<string[]>("GetAllTunnels").Result)
                    Console.WriteLine("\t\t" + s);
                Console.WriteLine();
            }
        }
    }
}
