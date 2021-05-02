//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// MessageHeader.cs - Copyright (c) 2018 Németh Péter
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
    public class MessageHeader : RequestHeader
    {
        public MessageHeader() 
        {
            RequestType = RequestType.Response;            
        }

        public MessageHeader(Stream stream)
        {
            LoadFromStream(stream);
        }

        public MessageHeader(MessageHeader rp)
        {
            Magic = rp.Magic;
            RequestType = rp.RequestType;
            Operation = rp.Operation;
            Error = rp.Error;
            RequestId = rp.RequestId;
            ProtocolVersion = rp.ProtocolVersion;
            AvailableProtocol = rp.AvailableProtocol;
            DataLength = rp.DataLength;
        }

        private void LoadFromStream(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                Magic = br.ReadUInt32();
                RequestType = (RequestType)br.ReadUInt16();
                Operation = (NetOperationType)br.ReadUInt16();
                Error = br.ReadUInt16();
                RequestId = br.ReadUInt32();
                ProtocolVersion = br.ReadUInt16();
                AvailableProtocol = br.ReadUInt16();
                DataLength = br.ReadInt32();
            }
        }
    }
}
