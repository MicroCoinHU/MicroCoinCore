//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// Discovery.cs - Copyright (c) 2018 Németh Péter
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


using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;

namespace MicroCoin.Net.Discovery
{
    public class Discovery
    {
        private readonly UdpClient udp;
        
        private readonly string[] _fixSeedIPs = { "185.33.146.44" };
        public List<IPEndPoint> endPoints { get; set; } = new List<IPEndPoint>();
        private readonly object lockObject = new object();
        public Discovery()
        {
            try
            {
                udp = new UdpClient(new IPEndPoint(IPAddress.Any, 15000));
            }
            catch
            {
                udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));       
                
            }
            StartListening();
            Discover();
            
        }

        public List<IPEndPoint> GetEndPoints()
        {
            lock (lockObject)
            {
                return new List<IPEndPoint>(endPoints);                
            }
        }

        private void StartListening()
        {
            udp.AllowNatTraversal(true);            
            udp.BeginReceive(Receive, new object());
        }

        private void Receive(IAsyncResult ar)
        {          
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 15000);
            byte[] bytes = udp.EndReceive(ar, ref ip);
            DiscoveryMessage message = new DiscoveryMessage(bytes);
            DiscoveryMessage response = new DiscoveryMessage();
            if (message.Command==DiscoveryCommand.HelloRequest)
            {
                // byte[] send = Encoding.ASCII.GetBytes("server");
                response.Command = DiscoveryCommand.NodeListRequest;
                response.Payload = null;
                udp.Send(response.ToByteArray(), response.Length, ip);
            }
            if (message.Command == DiscoveryCommand.HelloResponse)
            {
                response.Command = DiscoveryCommand.NodeListRequest;
                response.Payload = null;
                udp.Send(response.ToByteArray(), response.Length, ip);
            }
            if (message.Command == DiscoveryCommand.NodeListRequest)
            {
                using (MemoryStream m = new MemoryStream())
                {
                    //SslStream stream = new SslStream(m);
                    StreamWriter sw = new StreamWriter(m);                    
                    foreach (var p in GetEndPoints().ToArray()) {                        
                        sw.Write(p.Address.ToString());
                        sw.Write(':');
                        sw.Write(p.Port);
                        sw.Write(';');
                    }
                    sw.Flush();
                    response.Command = DiscoveryCommand.NodeListResponse;
                    response.Payload = m.ToArray();
                    udp.Send(response.ToByteArray(), response.Length, ip);
                    sw.Dispose();
                }
            }
            else if (message.Command == DiscoveryCommand.NodeListResponse)
            {
                lock (lockObject)
                {
                    string[] ips = message.ToString().Split(';');
                    foreach (string p in ips)
                    {
                        if (p == "") continue;
                        string[] ps = p.Split(':');
                        if (endPoints.Count(p1 => p1.Address.ToString() == ps[0] && p1.Port==Convert.ToInt32(ps[1]))==0)
                        {
                            endPoints.Add(new IPEndPoint(IPAddress.Parse(ps[0]), Convert.ToInt32(ps[1])));
                        }
                    }
                }
            }

            lock (lockObject)
            {
                if (endPoints.Count(p => p.Address.ToString() == ip.Address.ToString() && p.Port == ip.Port) == 0)
                {
                    endPoints.Add(ip);
                }
            }

            StartListening();
        }

        public void Discover()
        {
            lock (lockObject)
            {
                DiscoveryMessage response = new DiscoveryMessage {Command = DiscoveryCommand.HelloRequest};
                foreach (var seed in _fixSeedIPs)
                {

                    var ip = new IPEndPoint(IPAddress.Parse(seed), 15000);
                    response.Command = DiscoveryCommand.HelloRequest;                                        
                    udp.Send(response.ToByteArray(), response.Length, ip);
                }
                foreach (var point in endPoints.ToArray())
                {
                    response.Command = DiscoveryCommand.HelloRequest;
                    udp.Send(response.ToByteArray(), response.Length, point);
                }
                endPoints.Clear();
            }
        }

    }
}
