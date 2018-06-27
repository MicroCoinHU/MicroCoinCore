//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// ListAccountTransaction.cs - Copyright (c) 2018 Németh Péter
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


using MicroCoin.Chain;
using MicroCoin.Cryptography;
using MicroCoin.Util;
using System.IO;
using System.Text;

namespace MicroCoin.Transactions
{
    public sealed class ListAccountTransaction : Transaction
    {
        public enum AccountTransactionType : ushort
        {
            ListAccount = 4,
            DeListAccount = 5
        }


        public MCC AccountPrice { get; set; }

        public AccountNumber AccountToPay { get; set; }

        public ECKeyPair NewPublicKey { get; set; } = new ECKeyPair();

        public uint LockedUntilBlock { get; set; }

        public ListAccountTransaction(TransactionType type = TransactionType.ListAccountForSale)
        {
            TransactionType = type;
        }

        public ListAccountTransaction(Stream stream)
        {
            LoadFromStream(stream);
        }

        public override void SaveToStream(Stream s)
        {
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write(SignerAccount);
                bw.Write(TargetAccount);
                bw.Write((ushort) TransactionType);
                bw.Write(NumberOfOperations);
                if (TransactionType == TransactionType.ListAccountForSale)
                {
                    bw.Write(AccountPrice);
                    bw.Write(AccountToPay);
                    AccountKey.SaveToStream(s, false);
                    if (NewPublicKey.CurveType != CurveType.Empty)
                    {
                        NewPublicKey.SaveToStream(s);
                    }
                    else
                    {
                        bw.Write((ushort) 0);
                    }

                    bw.Write(LockedUntilBlock);
                }

                bw.Write(Fee);
                Payload.SaveToStream(bw);
                Signature.SaveToStream(s);
            }
        }

        public override void LoadFromStream(Stream s)
        {
            using (BinaryReader br = new BinaryReader(s, Encoding.Default, true))
            {
                SignerAccount = br.ReadUInt32();
                TargetAccount = br.ReadUInt32();
                TransactionType = (TransactionType) br.ReadUInt16();
                NumberOfOperations = br.ReadUInt32();
                if (TransactionType == TransactionType.ListAccountForSale)
                {
                    AccountPrice = br.ReadUInt64();
                    AccountToPay = br.ReadUInt32();
                    AccountKey = new ECKeyPair();
                    AccountKey.LoadFromStream(s, false);
                    NewPublicKey = new ECKeyPair();
                    NewPublicKey.LoadFromStream(s);
                    LockedUntilBlock = br.ReadUInt32();
                }

                Fee = br.ReadUInt64();
                Payload = ByteString.ReadFromStream(br);
                Signature = new ECSignature(s);
            }
        }

        public override byte[] GetHash()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(SignerAccount);
                    bw.Write(TargetAccount);
                    bw.Write(NumberOfOperations);
                    bw.Write(AccountPrice);
                    bw.Write(AccountToPay);
                    bw.Write(Fee);
                    Payload.SaveToStream(bw, false);
                    if (AccountKey?.PublicKey.X != null && AccountKey.PublicKey.X.Length > 0 && AccountKey.PublicKey.Y.Length > 0)
                    {
                        bw.Write((ushort) AccountKey.CurveType);
                        bw.Write((byte[]) AccountKey.PublicKey.X);
                        bw.Write((byte[]) AccountKey.PublicKey.Y);
                    }
                    else
                    {
                        bw.Write((ushort) 0);
                    }

                    NewPublicKey.SaveToStream(ms, false);
                    bw.Write(LockedUntilBlock);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }
        }

        public override bool IsValid()
        {
            if (!base.IsValid()) return false;
            if (AccountPrice < 0) return false;
            if (!AccountToPay.IsValid()) return false;
            return true;
        }
    }
}
