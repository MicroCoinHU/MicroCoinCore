//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// NewBlockRequest.cs - Copyright (c) 2018 Németh Péter
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
using System.IO;
using System.Text;

namespace MicroCoin.Protocol
{
    public class NewBlockRequest : MessageHeader
    {

        public Block Block { get; set; }

        public NewBlockRequest() : base()
        {
            this.Operation = Net.NetOperationType.NewBlock;
            this.RequestType = Net.RequestType.AutoSend;            
        }

        public NewBlockRequest(Block block) : base()
        {
            this.Operation = Net.NetOperationType.NewBlock;
            this.RequestType = Net.RequestType.AutoSend;
            this.Block = Block;            
        }



        internal NewBlockRequest(Stream stream) : base(stream)
        {
        }

        internal override void SaveToStream(Stream s)
        {
            base.SaveToStream(s);
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                using (BinaryWriter bw = new BinaryWriter(memoryStream))
                {
                    Block.SaveToStream(memoryStream);
                    using (BinaryWriter bw2 = new BinaryWriter(s, Encoding.Default, true))
                    {
                        memoryStream.Position = 0;
                        bw2.Write((int)memoryStream.Length);
                        memoryStream.CopyTo(s);
                        s.Position = 0;
                    }
                }
            }
            finally
            {
                memoryStream.Dispose();
            }
        }

        internal NewBlockRequest(Stream stream, MessageHeader rp) :base(rp)
        {
            Block = new Block(stream);
        }
    }
}
