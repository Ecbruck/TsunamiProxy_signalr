using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNet.SignalR.Client;
using System.Net.Sockets;

namespace TsunamiProxy.Server
{
    /// <summary>
    /// 用于运转网站到SignalR的隧道
    /// </summary>
    class ServerTunnel
    {
        public string ID { get; private set; }
        IHubProxy hub;
        TcpClient webCon;
        public ServerTunnel(string ID, byte[] header, IHubProxy hubProxy)
        {
            this.ID = ID;
            this.hub = hubProxy;
            Task.Factory.StartNew(Process, header, TaskCreationOptions.LongRunning);
        }

        private void Process(object oheader)
        {
            byte[] buffer = new byte[10240];
            int bytesRead = 0;
            try
            {
                byte[] header = oheader as byte[];
                webCon = parseServer(Encoding.Default.GetString(header));
                if (webCon == null)
                    throw new Exception();
                webCon.GetStream().WriteAsync(header, 0, header.Length);
                while ((bytesRead = webCon.GetStream().Read(buffer, 0, buffer.Length)) > 0)
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
            webCon.GetStream().WriteAsync(buffer, 0, buffer.Length);
        }

        public void CloseTunnel()
        {
            try { webCon.Close(); }
            catch { }
            if (Closed != null)
                Closed(this, new EventArgs());
        }
        public event EventHandler Closed;

        static TcpClient parseServer(string reqHeader)
        {
            string Method, Path;
            string Host = null;
            int Port = 80;
            string[] arrHeaders = reqHeader.Split(new char[] { '\r', '\n' }, 20, StringSplitOptions.RemoveEmptyEntries);
            if (arrHeaders.Length < 1)
                return null;

            Method = arrHeaders[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
            Path = arrHeaders[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
            // Get Host from "host: xxx.com"
            for (int i = 1; i < arrHeaders.Length; i++)
            {
                string strTemp = arrHeaders[i].Trim();
                if (strTemp.StartsWith(":"))
                    strTemp = strTemp.Substring(1).Trim();
                if (strTemp.StartsWith("host", true, null))
                {
                    Host = strTemp.Substring(5).Trim();
                    break;
                }
            }
            // The case starting an SSL tunnel
            if (Method.StartsWith("CONNECT", true, null)) Port = 443;
            // Handle the header without "host: xxx.com"
            if (string.IsNullOrEmpty(Host) && Path.Length >= 10)
            {
                if (Path.IndexOf(@"://") > -1) // has protocol
                {
                    string protocol = Path.Split(':')[0];
                    Host = Path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)[1];
                    //if (protocol.StartsWith("https", true, null)) Port = 443;
                    //if (protocol.StartsWith("ftp", true, null)) Port = 21;
                }
                else
                    Host = Path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
            // Handle the case with specified port number
            int iTemp = Host.LastIndexOf(":");
            if (iTemp > -1)
            {
                Port = int.Parse(Host.Substring(iTemp + 1));
                Host = Host.Substring(0, iTemp);
            }
            // Host & Port are ready, return TcpClient to server
            TcpClient server = null;
            try
            { server = new TcpClient(Host, Port); }
            //{ server = new TcpClient("127.0.0.1", 8087); }
            catch { }
            return server;
        }
    }
}
