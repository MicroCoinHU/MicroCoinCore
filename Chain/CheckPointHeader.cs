//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// CheckPointHeader.cs - Copyright (c) 2018 Németh Péter
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

namespace MicroCoin.Chain
{
    public class CheckPointHeader
    {
        public ByteString Magic { get; set; }
        public ushort Protocol { get; set; }
        public ushort Version { get; set; }
        public uint BlockCount { get; set; }
        public uint StartBlock { get; set; }
        public uint EndBlock { get; set; }
        public Hash Hash { get; set; }
        public long HeaderEnd { get; set; }
        public string MagicString => Encoding.ASCII.GetString(Magic);

        public uint[] Offsets { get; set; }

        public uint BlockOffset(uint blockNumber)
        {
            if (blockNumber > Offsets.Length) return uint.MaxValue;
            return Offsets[blockNumber];    // + (int)HeaderEnd;
        }
        public CheckPointHeader() { }
        public CheckPointHeader(Stream s)
        {
            LoadFromStream(s);            
        }

        public void SaveToStream(BinaryWriter bw)
        {                    
            Magic.SaveToStream(bw);
            bw.Write(Protocol);
            bw.Write(Version);
            bw.Write(BlockCount);
            bw.Write(StartBlock);
            bw.Write(EndBlock);
            if (Offsets == null) return;
            foreach (var b in Offsets)
            {
                if (b > 27)
                {
                    bw.Write(b-27);
                }
                else
                {
                    bw.Write(b);
                }
            }
        }

        public void SaveToStream(Stream s)
        {
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.Default, true))
            {
                SaveToStream(bw);
            }
        }

        public void LoadFromStream(Stream s)
        {
            using (BinaryReader br = new BinaryReader(s, Encoding.Default, true))
            {
                long position = s.Position;
                s.Position = s.Length - 34;
                Hash = ByteString.ReadFromStream(br);
                s.Position = position;
                ushort len = br.ReadUInt16();
                Magic = br.ReadBytes(len);
                Protocol = br.ReadUInt16();
                Version = br.ReadUInt16();
                BlockCount = br.ReadUInt32();
                StartBlock = br.ReadUInt32();
                EndBlock = br.ReadUInt32();
                long pos = s.Position;
                HeaderEnd = pos;
                Offsets = new uint[(EndBlock-StartBlock+2)];
                for (int i = 0; i < Offsets.Length; i++)
                {
                    Offsets[i] = (uint)(br.ReadUInt32() + pos);
                }
                long pos1 = s.Position;
                s.Position = s.Length - 32;
                Hash = br.ReadBytes(32);
                s.Position = pos1;
            }
        }
    }
}
