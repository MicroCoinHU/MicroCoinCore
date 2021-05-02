//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// NodeServerList.cs - Copyright (c) 2018 Németh Péter
//-----------------------------------------------------------------------
// MicroCoin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MicroCoin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU General Public License for more details.
//-----------------------------------------------------------------------
// You should have received a copy of the GNU General Public License
// along with MicroCoin. If not, see <http://www.gnu.org/licenses/>.
//-----------------------------------------------------------------------

using log4net;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace MicroCoin.Net
{
    public class NewConnectionEventArgs : EventArgs
    {
        public NodeServer Node { get; set; }

        public NewConnectionEventArgs(NodeServer node)
        {
            Node = node;
        }
    }

    public class NodeServerList : ConcurrentDictionary<string, NodeServer>, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<NewConnectionEventArgs> NewNode;
        public event EventHandler<EventArgs> NodesChanged;

        public ConcurrentDictionary<string, NodeServer> BlackList { get; set; } = new ConcurrentDictionary<string, NodeServer>();

        internal void SaveToStream(Stream s)
        {            
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write((uint)Count);
                foreach(var item in this)
                {
                    item.Value.IP.SaveToStream(bw);
                    bw.Write(item.Value.Port);
                    bw.Write(item.Value.LastConnection);
                }
            }
        }

        protected void OnNewConnection(NodeServer newNode)
        {
            NewNode?.Invoke(this, new NewConnectionEventArgs(newNode));
        }

        protected void OnNodesChanged()
        {
            NodesChanged?.Invoke(this,new EventArgs());
        }
        public void BroadCastMessage(Stream message)
        {
            foreach (var item in this)
            {
                item.Value.MicroCoinClient.SendRaw(message);
            }
        }

        internal void TryAddNew(string key, NodeServer nodeServer)
        {
            if (BlackList.ContainsKey(key)) return;
            if (ContainsKey(key)) return;
            new Thread(() =>
            {
                var microCoinClient = nodeServer.Connect();
                if (microCoinClient != null && nodeServer.Connected)
                {
                    TryAdd(key, nodeServer);
                    nodeServer.Disconnected += (o, e) =>
                    {
                        TryRemove(key, out nodeServer);
                        if (nodeServer != null)
                        {                            
                            TryAddNew(key, nodeServer);
                        }
                    };
                    OnNewConnection(nodeServer);
                }
                else
                {
                    BlackList.TryAdd(key, nodeServer);
                    TryRemove(key, out nodeServer);
                }
                OnNodesChanged();
            })
            {
                Name = nodeServer.ToString()
            }.Start();
        }

        internal static NodeServerList LoadFromStream(Stream stream, ushort serverPort)
        {
            NodeServerList ns = new NodeServerList();
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                uint serverCount = br.ReadUInt32();
                for(int i = 0; i < serverCount; i++)
                {
                    NodeServer server = new NodeServer();
                    ushort iplen = br.ReadUInt16();
                    server.IP = br.ReadBytes(iplen);
                    server.Port = br.ReadUInt16();
                    server.LastConnection = br.ReadUInt32();
                    server.ServerPort = serverPort;
                    ns.TryAdd(server.ToString(), server);
                }
            }
            return ns;
        }

        internal void UpdateNodeServers(NodeServerList nodeServers)
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var nodeServer in nodeServers)
            {
                if (IPAddress.IsLoopback(nodeServer.Value.EndPoint.Address)) continue;
                if (localIPs.Contains(nodeServer.Value.EndPoint.Address)) continue;
                if (ContainsKey(nodeServer.Value.ToString())) continue;
                if (nodeServer.Value.Port != Node.NetParams.Port) continue;
                TryAddNew(nodeServer.Value.ToString(), nodeServer.Value);
            }
            if (Count <= 100) return;
            foreach (var l in nodeServers)
            {
                TryRemove(l.Key, out NodeServer n);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            foreach (var n in this)
            {
                try
                {
                    n.Value.MicroCoinClient.Dispose();
                }
                catch { }
            }
            Clear();
        }
    }
}
