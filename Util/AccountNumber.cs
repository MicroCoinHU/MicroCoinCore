//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// AccountNumber.cs - Copyright (c) 2018 Németh Péter
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
    public struct AccountNumber : IEquatable<object>, IEquatable<uint>, IEquatable<string>
    {

        private readonly uint _value;

        public AccountNumber(string value) {
            try
            {
                if (value.Contains("-"))
                {
                    _value = Convert.ToUInt32(value.Split('-')[0]);
                    if (!ToString().Equals(value))
                    {
                        throw new ArgumentException(String.Format("Invalid account number, bad checksum {0}!={1}", value, ToString()));
                    }
                }
                else
                {
                    _value = Convert.ToUInt32(value);
                }
            }
            catch (Exception)
            {
                throw new InvalidCastException("Hibás számlaszám!");
            }
        }

        public AccountNumber(uint value) {
            _value = value;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
        public bool Equals(uint obj)
        {
            return _value.Equals(obj);
        }

        public static bool operator ==(AccountNumber an, AccountNumber an2)
        {
            return an._value == an2._value;
        }

        public static bool operator !=(AccountNumber an, AccountNumber an2)
        {
            return an._value != an2._value;
        }

        public static bool operator ==(AccountNumber an, string an2)
        {
            return an.Equals(an2);
        }

        public static bool operator !=(AccountNumber an, string an2)
        {
            return !an.Equals(an2);
        }


        public override bool Equals(object obj)
        {
            return _value.Equals(obj);

        }

        public override string ToString()
        {
            var checksum = ((_value * 101) % 89) + 10;
            return _value.ToString() + '-' + checksum;
        }

        public bool Equals(string other)
        {
            AccountNumber an = new AccountNumber(other);
            return an._value == _value;
        }

        public static implicit operator AccountNumber(string s)
        {
            return new AccountNumber(s);
        }

        public static implicit operator AccountNumber(uint s)
        {
            return new AccountNumber(s);
        }

        public static implicit operator UInt32(AccountNumber number)
        {
            return number._value;
        }
        public static implicit operator Int32(AccountNumber number)
        {
            return (int) number._value;
        }

    }
}
