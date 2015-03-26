using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNet.SignalR.Client;

namespace TsunamiProxy.Client
{
    /// <summary>
    /// 用于运转浏览器到SignalR的隧道
    /// </summary>
    public class ClientTunnel
    {
        public string ID { get; private set; }
        IHubProxy hub;
        TcpClient browsCon;
        public ClientTunnel(TcpClient browserConnection, IHubProxy hubProxy)
        {
            this.browsCon = browserConnection;
            this.hub = hubProxy;
            Task.Factory.StartNew(Process, TaskCreationOptions.LongRunning);
        }

        private void Process()
        {
            byte[] buffer = new byte[10240];
            int bytesRead = 0;
            try
            {
                ID = hub.Invoke<string>("NewTunnel", "server").Result;
                if (!string.IsNullOrWhiteSpace(ID))
                    while ((bytesRead = browsCon.GetStream().Read(buffer, 0, buffer.Length)) > 0)
                        hub.Invoke("TrafficTunnel", ID, buffer.Take(bytesRead).ToArray());
            }
            finally
            {
                hub.Invoke("CloseTunnel", ID);
                CloseTunnel();
            }
        }

        public void Traffic(byte[] buffer)
        {
            browsCon.GetStream().WriteAsync(buffer, 0, buffer.Length);
        }

        public void CloseTunnel()
        {
            try { browsCon.Close(); }
            catch { }
            if (Closed != null)
                Closed(this, new EventArgs());
        }

        public event EventHandler Closed;
    }
}
