//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// BlockResponse.cs - Copyright (c) 2018 Németh Péter
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

using MicroCoin.Chain;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MicroCoin.Protocol
{
    public class BlockResponse : MessageHeader
    {
        public List<Block> Blocks { get; set; }
        public uint TransactionCount { get; set; }
        public BlockResponse(Stream stream) : base(stream)
        {
        }
        public BlockResponse()
        {
            Operation = Net.NetOperationType.Blocks;
            RequestType = Net.RequestType.Response;
        }

        internal override void SaveToStream(Stream s)
        {
            MemoryStream ms = new MemoryStream();
            base.SaveToStream(s);
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((uint)Blocks.Count);
                foreach (var b in Blocks)
                {
                    b.SaveToStream(ms);
                }
                ms.Position = 0;
                DataLength = (int)ms.Length;
                using (BinaryWriter bw2 = new BinaryWriter(s, Encoding.Default, true))
                {
                    bw2.Write(DataLength);
                }
                ms.CopyTo(s);
            }
        }

        internal BlockResponse(Stream stream, MessageHeader rp) :base(rp)
        {
            Blocks = new List<Block>();
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                uint BlockCount = br.ReadUInt32();
                //for(int i = 0; i < BlockCount; i++)
                while(true)
                {
                    if (stream.Position >= stream.Length - 1) {
                        break;
                    }
                    try
                    {
                        long pos = stream.Position;
                        Block op = new Block(stream);
                    
                        Blocks.Add(op);
                    }catch(EndOfStreamException e)
                    {
                        throw e;
                    }
                }
            }
        }
    }
}
