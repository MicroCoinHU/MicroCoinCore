//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// Timestamp.cs - Copyright (c) 2018 Németh Péter
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

namespace MicroCoin.Util
{
    public struct Timestamp
    {
        private readonly uint _unixTimestamp;
        public Timestamp(uint unixTimestamp)
        {
            _unixTimestamp = unixTimestamp;
        }
        public static implicit operator Timestamp(DateTime dt)
        {
            return new Timestamp((uint)dt.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        }

        public static implicit operator DateTime(Timestamp t)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);            
            dtDateTime = dtDateTime.AddSeconds(t._unixTimestamp).ToLocalTime();
            return dtDateTime;
        }

        public static implicit operator Timestamp(uint dt)
        {
            return new Timestamp(dt);
        }

        public static implicit operator UInt32(Timestamp t)
        {
            return t._unixTimestamp;
        }

        public override string ToString()
        {
            return ((DateTime)this).ToString();
        }
    }
}
