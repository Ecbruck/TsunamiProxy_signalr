using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TsunamiProxy.Cloud.Hubs
{
    public static class HubData
    {
        public static ConcurrentDictionary<string, DateTime> LastBeat { get; set; }
        public static ConcurrentDictionary<string, string> Names { get; set; }
        public static List<TunnelInfo> Tunnels { get; set; }
        static HubData()
        {
            Names = new ConcurrentDictionary<string, string>();
            Tunnels = new List<TunnelInfo>();
        }
    }
}