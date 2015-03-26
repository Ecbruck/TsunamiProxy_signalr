using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using Microsoft.AspNet.SignalR.Client;

namespace TsunamiProxy.Server
{
    class ProxyServerApp
    {
        List<ServerTunnel> tunnels;
        HubConnection hubCon;
        IHubProxy hubPxy;

        public ProxyServerApp()
        {
            tunnels = new List<ServerTunnel>();
            var querystringdata = new Dictionary<string, string>();
            querystringdata.Add("name", "server");
            hubCon = new HubConnection("http://tsunamiproxy.chinacloudsites.cn", querystringdata);
            //hubCon = new HubConnection("http://localhost:5349/", querystringdata);
            hubPxy = hubCon.CreateHubProxy("ProxyHub");
            hubPxy.On<string, byte[]>("TrafficTunnel", TrafficTunnel);
            hubPxy.On<string>("CloseTunnel", CloseTunnel);
            hubCon.Start().ContinueWith(task =>
            {
                while (true)
                {
                    Task.Delay(1000).Wait();
                    Console.WriteLine("Tunnels:");
                    foreach (var t in tunnels)
                        Console.WriteLine("\t\t" + t.ID);
                    Console.WriteLine();
                    hubPxy.Invoke("AcceptHeartBeat");
                }
            }, TaskContinuationOptions.LongRunning);
        }

        private void TrafficTunnel(string tunnelID, byte[] buffer)
        {
            ServerTunnel tunnel = FindTunnel(tunnelID);
            if (tunnel == null)
            {
                var t = new ServerTunnel(tunnelID, buffer, hubPxy);
                t.Closed += (_, __) => tunnels.Remove(t);
                tunnels.Add(t);
            }
            else
                tunnel.Traffic(buffer);
        }
        private void CloseTunnel(string tunnelID)
        {
            ServerTunnel tunnel = FindTunnel(tunnelID);
            if (tunnel == null)
                hubPxy.Invoke("CloseTunnel", tunnelID);
            else
                tunnel.CloseTunnel();
        }
        private ServerTunnel FindTunnel(string tunnelID)
        {
            var qtn = from t in tunnels
                      where t.ID == tunnelID
                      select t;
            try
            {
                return qtn.Single();
            }
            catch
            {
                return null;
            }
        }
    }
}
