//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// Hash.cs - Copyright (c) 2018 Németh Péter
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
using System.IO;
using System.Linq;

namespace MicroCoin.Util
{
    public struct Hash
    {

        private readonly byte[] _value;
        
        public Hash(byte[] b)
        {
            _value = b;
        }

        public int Length => _value.Length;

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public Hash Reverse()
        {
            return _value.Reverse().ToArray();
        }

        public static implicit operator Hash(string s)
        {
            return new Hash(StringToByteArray(s));
        }

        public static implicit operator string(Hash s)
        {
            return s._value == null ? null : BitConverter.ToString(s).Replace("-", "");
        }

        public static implicit operator ByteString(Hash s) => new ByteString(s);


        public static implicit operator byte[] (Hash s) => s._value;

        public static implicit operator Hash(byte[] s) => new Hash(s);

        public static implicit operator Hash(ByteString s) => new Hash(s);

        public static Hash ReadFromStream(BinaryReader br)
        {
            ushort len = br.ReadUInt16();
            Hash bs = br.ReadBytes(len);
            return bs;
        }

        public void SaveToStream(BinaryWriter bw)
        {
            _value.SaveToStream(bw);
        }

        public override string ToString()
        {
            return this;
        }

        internal void SaveToStream(BinaryWriter bw, bool writeLengths)
        {
            if (writeLengths) _value.SaveToStream(bw);
            else {
                if (_value == null) return;
                bw.Write(_value);
                
            }
        }

        internal bool SequenceEqual(Hash x)
        {
            return _value.SequenceEqual(x._value);
        }
    }


}