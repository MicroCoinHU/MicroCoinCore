//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// DiscoveryMessage.cs - Copyright (c) 2018 Németh Péter
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


using System.IO;
using System.Linq;
using System.Text;

namespace MicroCoin.Net.Discovery
{
    public enum DiscoveryCommand { HelloRequest, HelloResponse, NodeListResponse, NodeListRequest }
    public class DiscoveryMessage
    {
        public DiscoveryCommand Command { get; set; }
        public ushort PayloadLength
        {
            get
            {
                if(Payload!=null)
                    return (ushort) Payload.Length;
                return 0;
            }
        }
        public byte[] Payload { get; set; }
        public int Length => PayloadLength + sizeof(int) +sizeof(ushort);

        public DiscoveryMessage() { }

        public DiscoveryMessage(byte[] data)
        {
            int dt = (data[3] << 24 | data[2] << 16 | data[1] << 8 | data[0]);
            Command = (DiscoveryCommand)dt;
            ushort payloadLength =  (ushort)((data[4] << 8) | (data[2]));
            Payload = data.Skip(6).ToArray();
        }

        public byte[] ToByteArray()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((int)Command);
                bw.Write(PayloadLength);
                if (PayloadLength > 0)
                {
                    bw.Write(Payload, 0, Payload.Length);
                }
                bw.Flush();
                return ms.ToArray();
            }
        }
        public override string ToString() => Payload==null?"":Encoding.ASCII.GetString(Payload);
        public static DiscoveryMessage FromString(DiscoveryCommand command, string message)
        {
            return new DiscoveryMessage
            {
                Payload = Encoding.UTF8.GetBytes(message),
                Command = command
            };
        }
    }
}
