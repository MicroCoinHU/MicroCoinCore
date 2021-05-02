//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// MCC.cs - Copyright (c) 2018 Németh Péter
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

namespace MicroCoin.Util
{
    public struct MCC
    {
        public decimal value { get; }

        public MCC(decimal value)
        {
            this.value = value;
        }

        public static implicit operator decimal(MCC m)
        {
            return m.value;
        }

        public static implicit operator ulong(MCC m)
        {
            return (ulong)(m.value*10000M);
        }

        public static implicit operator MCC(ulong m)
        {
            return new MCC( m / 10000M );
        }

        public static MCC operator +(MCC a, MCC b)
        {
            return new MCC(a.value + b.value);
        }

        public static MCC operator -(MCC a, MCC b)
        {
            return new MCC(a.value - b.value);
        }

        public static MCC operator -(MCC a, ulong b)
        {
            return new MCC(a.value - b);
        }

        public static MCC operator +(MCC a, ulong b)
        {
            return new MCC(a.value + b);
        }

        public override bool Equals(object obj)
        {
            return ((ulong)this).Equals(obj);
        }

        public override string ToString()
        {
            return value.ToString("G");
        }

        public string ToString(string format)
        {
            return value.ToString(format);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
    }
}
