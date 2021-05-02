//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// BlockHeaderForHash.cs - Copyright (c) 2018 Németh Péter
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
using MicroCoin.Util;

namespace MicroCoin.Chain
{
    public struct BlockHeaderForHash
    {
        public Hash Part1 { get; set; }
        public ByteString MinerPayload { get; set; }
        public Hash Part3 { get; set; }
        public Hash Join()
        {
            return Part1 + MinerPayload + Part3;
        }

        public Hash GetBlockHeaderHash(uint nonce, uint timestamp)
        {
            Hash s1 = $"{timestamp:X04}";
            Hash s2 = $"{nonce:X08}";
            Hash h = (byte[]) MinerPayload;
            return Part1 + h + Part3 + s1.Reverse() + s2.Reverse();
        }
    }
}
