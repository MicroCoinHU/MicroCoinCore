//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// CheckPointRequest.cs - Copyright (c) 2018 Németh Péter
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

using MicroCoin.Util;
using System.IO;
using System.Text;

namespace MicroCoin.Protocol
{
    public class CheckPointRequest : MessageHeader
    {
        public uint CheckPointBlockCount { get; set; }
        public ByteString CheckPointHash { get; set; }
        public uint StartBlock { get; set; }
        public uint EndBlock { get; set; }
        public CheckPointRequest(Stream s, MessageHeader rp) : base(rp)
        {
            using(BinaryReader br = new BinaryReader(s, Encoding.Default, true))
            {
                CheckPointBlockCount = br.ReadUInt32();
                CheckPointHash = ByteString.ReadFromStream(br);
                StartBlock = br.ReadUInt32();
                EndBlock = br.ReadUInt32();
            }
        }
        public CheckPointRequest()
        {
        }
        internal override void SaveToStream(Stream s)
        {
            base.SaveToStream(s);
            using(BinaryWriter bw = new BinaryWriter(s,Encoding.Default, true))
            {
                DataLength = 4 + 2 + 32 + 4 + 4;
                bw.Write(DataLength);
                bw.Write(CheckPointBlockCount);
                CheckPointHash.SaveToStream(bw);
                bw.Write(StartBlock);
                bw.Write(EndBlock);
            }
        }
    }
}
