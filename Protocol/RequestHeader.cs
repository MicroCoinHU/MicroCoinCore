//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// RequestHeader.cs - Copyright (c) 2018 Németh Péter
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


using MicroCoin.Net;
using System.IO;
using System.Text;

namespace MicroCoin.Protocol
{
    public class RequestHeader
    {
        public static int Size = 4 + 2 + 2 + 2 + 4 + 2 + 2 + 4;

        public uint Magic { get; set; } = Node.NetParams.NetworkPacketMagic;
        public RequestType RequestType { get; set; } = RequestType.Request;
        public NetOperationType Operation { get; set; }
        public ushort Error { get; set; }
        private static uint _requestId = 1;
        public uint RequestId
        {
            get;
            set;
        }
        public ushort ProtocolVersion { get; set; }
        public ushort AvailableProtocol { get; set; }
        public int DataLength { get; set; }
        public static readonly object RequestLock = new object();

        public RequestHeader()
        {
            lock(RequestLock) {
                RequestId = _requestId++;
            }
            Operation = NetOperationType.Hello;
            ProtocolVersion = Node.NetParams.NetworkProtocolVersion;
            AvailableProtocol = Node.NetParams.NetworkProtocolAvailable;
            Error = 0;
        }

        internal virtual void SaveToStream(Stream s)
        {
            using (BinaryWriter br = new BinaryWriter(s, Encoding.ASCII, true))
            {
                br.Write(Magic);
                br.Write((ushort)RequestType);
                br.Write((ushort)Operation);
                br.Write(Error);
                br.Write(RequestId);
                br.Write(ProtocolVersion);
                br.Write(AvailableProtocol);
            }
        }
    }
}
