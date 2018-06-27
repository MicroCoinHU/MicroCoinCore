//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// AccountInfo.cs - Copyright (c) 2018 Németh Péter
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


using MicroCoin.Cryptography;
using MicroCoin.Util;
using System;
using System.IO;


namespace MicroCoin.Chain
{
    public enum AccountState
    {
        Unknown,
        Normal,
        Sale
    }

    public class AccountInfo : IEquatable<AccountInfo>
    {
        public AccountState State { get; set; }
        public string StateString
        {
            get
            {
                switch (State)
                {
                    case AccountState.Normal: return "Normál";
                    case AccountState.Sale: return "Eladó";
                    case AccountState.Unknown: return "Ismeretlen";
                    default:
                        return "?";
                }
            }
        }
        public ECKeyPair AccountKey { get; set; }
        public uint LockedUntilBlock { get; set; }
        public MCC Price { get; set; }
        public decimal VisiblePrice => Price;
        public AccountNumber AccountToPayPrice { get; set; }
        public ECKeyPair NewPublicKey { get; set; }

        public static AccountInfo CreateFromStream(BinaryReader br)
        {
            var ai = new AccountInfo();
            ai.LoadFromStream(br);
            return ai;
        }

        internal void SaveToStream(BinaryWriter bw, bool writeLengths = true)
        {
            ushort len = 0;
            long pos = bw.BaseStream.Position;
            if (writeLengths)
            {
                bw.Write(len);
            }
            switch (State)
            {
                case AccountState.Normal:
                    AccountKey.SaveToStream(bw.BaseStream, false);
                    break;
                case AccountState.Sale:
                    bw.Write((ushort)1000);
                    AccountKey.SaveToStream(bw.BaseStream, false);
                    bw.Write(LockedUntilBlock);
                    bw.Write(Price);
                    bw.Write(AccountToPayPrice);
                    NewPublicKey.SaveToStream(bw.BaseStream, false);
                    break;
                case AccountState.Unknown:
                    break;
                default: throw new Exception("Invalid account state");
            }
            long size = bw.BaseStream.Position - pos - 2;
            long reverse = bw.BaseStream.Position;
            bw.BaseStream.Position = pos;
            if(writeLengths) bw.Write((ushort)size);
            bw.BaseStream.Position = reverse;
        }

        internal void LoadFromStream(BinaryReader br)
        {
            ushort unused = br.ReadUInt16();
            ushort stateOrKeyType = br.ReadUInt16();
            switch (stateOrKeyType)
            {
                case (ushort)CurveType.Secp256K1:
                case (ushort)CurveType.Secp384R1:
                case (ushort)CurveType.Secp521R1:
                case (ushort)CurveType.Sect283K1:
                    br.BaseStream.Position -= 2;
                    State = AccountState.Normal;
                    AccountKey = new ECKeyPair();
                    AccountKey.LoadFromStream(br.BaseStream, false);
                    LockedUntilBlock = 0;
                    Price = 0;
                    AccountToPayPrice = 0;
                    //NewPublicKey = new ECKeyPair();
                    break;
                case 1000:
                    State = AccountState.Sale;
                    AccountKey = new ECKeyPair();
                    AccountKey.LoadFromStream(br.BaseStream, false);
                    LockedUntilBlock = br.ReadUInt32();
                    Price = br.ReadUInt64();
                    AccountToPayPrice = br.ReadUInt32();
                    NewPublicKey = new ECKeyPair();
                    NewPublicKey.LoadFromStream(br.BaseStream, false);
                    break;
                default:
                    throw new Exception("Invalid account info");
            }
        }

        public bool Equals(AccountInfo other)
        {
            if (other == null) return false;
            if (other.AccountToPayPrice != AccountToPayPrice) return false;
            if (other.LockedUntilBlock != LockedUntilBlock) return false;
            if (other.Price != Price) return false;
            if (other.State != State) return false;
            if (!other.AccountKey.Equals(AccountKey)) return false;
            if (other.NewPublicKey == null)
            {
                if (NewPublicKey != null)
                    return false;
            }
            else
            {
                if (!other.NewPublicKey.Equals(NewPublicKey)) return false;
            }
            return true;
        }
    }
}
