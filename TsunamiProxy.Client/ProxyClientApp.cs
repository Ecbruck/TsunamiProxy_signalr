using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNet.SignalR.Client;
using System.Net.Sockets;
using System.Net;

namespace TsunamiProxy.Client
{
    class ProxyClientApp
    {
        List<ClientTunnel> tunnels;
        HubConnection hubCon;
        IHubProxy hubPxy;

        public ProxyClientApp(int port)
        {
            tunnels = new List<ClientTunnel>();
            var querystringdata = new Dictionary<string, string>();
            querystringdata.Add("name", "client");
            hubCon = new HubConnection("http://tsunamiproxy.chinacloudsites.cn", querystringdata);
            //hubCon = new HubConnection("http://localhost:5349/", querystringdata);
            hubPxy = hubCon.CreateHubProxy("ProxyHub");
            hubPxy.On<string>("CloseTunnel", CloseTunnel);
            hubPxy.On<string, byte[]>("TrafficTunnel", TrafficTunnel);
            hubCon.Start();

            Task.Factory.StartNew(() =>
            {
                var lisener = new TcpListener(IPAddress.Any, port);
                lisener.Start();
                while (true)
                {
                    var c = lisener.AcceptTcpClient();
                    if (hubCon != null && hubCon.State == ConnectionState.Connected)
                    {
                        var t = new ClientTunnel(c, hubPxy);
                        t.Closed += (_, __) => tunnels.Remove(t);
                        tunnels.Add(t);
                    }
                    else
                        c.Close();
                }
            }, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() =>
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
            }, TaskCreationOptions.LongRunning);
        }

        private void CloseTunnel(string tunnelID)
        {
            var tunnel = FindTunnel(tunnelID);
            if (tunnel != null)
                tunnel.CloseTunnel();
        }

        private void TrafficTunnel(string tunnelID, byte[] buffer)
        {
            var tunnel = FindTunnel(tunnelID);
            if (tunnel != null)
                tunnel.Traffic(buffer);
        }

        private ClientTunnel FindTunnel(string tunnelID)
        {
            var qtn = from t in tunnels
                      where t.ID == tunnelID
                      select t;
            try
            {
                var tunnel = qtn.Single();
                return tunnel;
            }
            catch
            {
                hubPxy.Invoke("CloseTunnel", tunnelID);
                return null;
            }
        }
    }
}
