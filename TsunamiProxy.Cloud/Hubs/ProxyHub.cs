using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace TsunamiProxy.Cloud.Hubs
{
    public class ProxyHub : Hub
    {
        public override System.Threading.Tasks.Task OnConnected()
        {
            var name = Context.QueryString["name"];
            if (!string.IsNullOrWhiteSpace(name))
            {
                var conIds = from n in HubData.Names
                             where n.Value == name
                             select n.Key;
                string outname;
                foreach (var ci in conIds)
                    HubData.Names.TryRemove(ci, out outname);
                HubData.Names.TryAdd(Context.ConnectionId, name);
            }

            return base.OnConnected();
        }

        public void AcceptHeartBeat()
        {
            HubData.LastBeat.AddOrUpdate(Context.ConnectionId, DateTime.UtcNow, (_, __) => DateTime.UtcNow);
        }

        public string[] GetAllClients()
        {
            var query = from n in HubData.Names
                        select n.Value;
            return query.ToArray();
        }
        public string[] GetAllTunnels()
        {
            var query = from t in HubData.Tunnels//, Server = {1}, Client = {2}"
                        select string.Format("TunnelID = {0}", t.ID, t.ServerConnectionID, t.ClientConnectionID);
            return query.ToArray();
        }
        public string NewTunnel(string serverName)
        {
            // 生成TunnelID
            var tunnelID = Guid.NewGuid().ToString();
            // 寻找服务器端
            var server = from n in HubData.Names
                         where n.Value == serverName && n.Key != Context.ConnectionId
                         select n;
            try
            {
                // 建立TunnelID到Client端和Server端的索引
                HubData.Tunnels.Add(new TunnelInfo
                {
                    ID = tunnelID,
                    ClientConnectionID = Context.ConnectionId,
                    ServerConnectionID = server.Single().Key
                });
                return tunnelID;
            }
            catch
            {
                return null;
            }
        }
        #region Client端和Server端都要使用
        public bool TrafficTunnel(string tunnelID, byte[] buffer)
        {
            // 寻找隧道记录
            var tunnel = findTunnel(tunnelID);
            if (tunnel == null)
                return false;
            // 寻找另一端
            var opposite = findOpposite(tunnel, Context.ConnectionId);
            if (string.IsNullOrWhiteSpace(opposite))
                return false;
            Task.Factory.StartNew(() => Clients.Client(opposite).TrafficTunnel(tunnelID, buffer));
            return true;
        }
        public bool CloseTunnel(string tunnelID)
        {
            // 寻找隧道记录
            var tunnel = findTunnel(tunnelID);
            if (tunnel == null)
                return false;

            HubData.Tunnels.Remove(tunnel);
            // 寻找另一端
            var opposite = findOpposite(tunnel, Context.ConnectionId);
            if (string.IsNullOrWhiteSpace(opposite))
                return false;
            Task.Factory.StartNew(() => Clients.Client(opposite).CloseTunnel(tunnelID));
            return true;
        }
        private TunnelInfo findTunnel(string tunnelID)
        {
            var tunnel = from t in HubData.Tunnels
                         where t.ID == tunnelID
                         select t;
            try { return tunnel.Single(); }
            catch { return null; }
        }
        private string findOpposite(TunnelInfo tunnel, string self)
        {
            if (self == tunnel.ClientConnectionID)
                return tunnel.ServerConnectionID;
            if (self == tunnel.ServerConnectionID)
                return tunnel.ClientConnectionID;
            throw new Exception();
        }
        #endregion
    }
}