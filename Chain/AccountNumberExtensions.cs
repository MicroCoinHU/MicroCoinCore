//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// CheckPoints.cs - Copyright (c) 2018 Németh Péter
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
using System;
using System.Linq;

namespace MicroCoin.Chain
{
    public static class AccountNumberExtensions
    {
        public static bool IsValid(this AccountNumber number)
        {
            if (CheckPoints.Accounts.Count(p => p.AccountNumber == number) != 1) return false;
            return true;
        }
        public static Account Account(this AccountNumber an)
        {
            if(!an.IsValid()) throw new InvalidCastException();
            return CheckPoints.Accounts[an];
        }
    }
}
