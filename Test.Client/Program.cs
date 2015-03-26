using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test.Client
{
    class Program
    {
        static readonly int ProxyPort = 13000;
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, ProxyPort);
            listener.Start();

            while (true)
            {
                TcpClient browserConnection = listener.AcceptTcpClient();
                Console.WriteLine("+++ Accepted a TCP client +++");

                //Task.Factory.StartNew(ProcessIncomingRequest, browserConnection, TaskCreationOptions.LongRunning);
                Thread t = new Thread(new ParameterizedThreadStart(ProcessIncomingRequest));
                t.Start(browserConnection);
            }
        }

        static void ProcessIncomingRequest(object browserConnection)
        {
            TcpClient connection = browserConnection as TcpClient;
            if (connection != null)
                ProcessIncomingRequest(connection);
        }

        static void ProcessIncomingRequest(TcpClient browserConnection)
        {
            byte[] connectEstBuf = Encoding.Default.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
            byte[] buffer = new byte[10240];
            TcpClient serverConnection = null;
            try
            {
                int bytesRead = browserConnection.GetStream().Read(buffer, 0, buffer.Length);

                serverConnection = parseServer(Encoding.Default.GetString(buffer, 0, bytesRead));
                if (serverConnection == null)
                {
                    browserConnection.Close();
                    Console.WriteLine("---- Removed client before Header ---");
                    return;
                }
                if (((IPEndPoint)serverConnection.Client.RemoteEndPoint).Port == 443)
                    browserConnection.GetStream().Write(connectEstBuf, 0, connectEstBuf.Length);
                else
                    serverConnection.GetStream().Write(buffer, 0, bytesRead);

                Task.WaitAny(
                    Task.Factory.StartNew(() => TcpCopy(browserConnection, serverConnection), TaskCreationOptions.LongRunning),
                    Task.Factory.StartNew(() => TcpCopy(serverConnection, browserConnection), TaskCreationOptions.LongRunning)
                );
            }
            finally
            {
                if (serverConnection != null) serverConnection.Close();
                browserConnection.Close();
            }
        }

        static void TcpCopy(TcpClient from, TcpClient to)
        {
            byte[] buffer = new byte[10240];
            int bytecount;
            while ((bytecount = from.GetStream().Read(buffer, 0, buffer.Length)) > 0)
                to.GetStream().Write(buffer, 0, bytecount);
        }


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
            {server= new TcpClient(Host, Port);}
            //{ server = new TcpClient("127.0.0.1", 8087); }
            catch { }
            return server;
        }
    }
}
