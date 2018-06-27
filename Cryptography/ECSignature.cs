//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// ECSignature.cs - Copyright (c) 2018 Németh Péter
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
using MicroCoin.Util;

namespace MicroCoin.Cryptography
{
    public struct ECSignature
    {
        public byte[] R { get; set; }
        public byte[] S { get; set; }

        public byte[] SigCompat { get; set; }

        internal ECSignature(Stream stream)
        {
            SigCompat = new byte[0];
            using (var br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                var len = br.ReadUInt16();
                int offset = 0;
                if (len == 31)
                {
                    R = new byte[len + 1];
                    R[0] = 0;
                    offset = 1;
                }
                else
                {
                    R = new byte[len];
                }
                br.Read(R, offset, len);                
                len = br.ReadUInt16();
                offset = 0;
                if (len == 31)
                {
                    S = new byte[len + 1];
                    S[0] = 0;
                    offset = 1;
                }
                else
                {
                    S = new byte[len];
                }
                br.Read(S, offset, len);
            }
        }

        public ECSignature(Hash sign)
        {
            SigCompat = new byte[0];
            byte[] data = sign;
            R = data.Take(32).ToArray();
            S = data.Skip(32).Take(32).ToArray();
        }

        public byte[] Signature
        {
            get
            {
                var ret = R.ToList();
                ret.AddRange(S);
                return ret.ToArray();
            }
        }

        internal void SaveToStream(Stream stream)
        {
            using (var bw = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                bw.Write((ushort) R.Length);
                bw.Write(R);
                bw.Write((ushort) S.Length);
                bw.Write(S);
            }
        }
    }
}