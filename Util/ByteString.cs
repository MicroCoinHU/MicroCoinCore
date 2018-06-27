//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// ByteString.cs - Copyright (c) 2018 Németh Péter
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

namespace MicroCoin.Util
{
    public struct ByteString
    {

        private byte[] _value;

        public ByteString(byte[] b)
        {
            _value = b;
        }

        public int Length => _value == null ? 0 : _value.Length;

        public static implicit operator ByteString(string s)
        {
            return new ByteString(s==null?new byte[0]:Encoding.Default.GetBytes(s));
        }

        public static implicit operator string(ByteString s)
        {
            return s._value == null ? null : Encoding.UTF8.GetString(s._value);
        }

        public static implicit operator byte[](ByteString s)
        {
            return s._value;
        }

        public static implicit operator ByteString(byte[] s)
        {
            return new ByteString(s);
        }

        public bool IsReadable => _value == null ? true : _value.Count(p => char.IsControl((char)p)) == 0;

        public static ByteString ReadFromStream(BinaryReader br)
        {
            ushort len = br.ReadUInt16();
            ByteString bs = br.ReadBytes(len);
            if (bs._value == null) bs._value = new byte[0];
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
            else
            {
                if (_value == null) return;
                bw.Write(_value);
            }
//            bw.Write(value);
        }
    }


}