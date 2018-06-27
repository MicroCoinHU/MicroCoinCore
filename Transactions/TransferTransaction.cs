//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// TransferTransaction.cs - Copyright (c) 2018 Németh Péter
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
using System.IO;
using System.Text;
using MicroCoin.Chain;

namespace MicroCoin.Transactions
{
    public sealed class TransferTransaction : Transaction
    {
        public enum TransferType : byte { Transaction, TransactionAndBuyAccount, BuyAccount }
        public MCC AccountPrice { get; set; }
        public AccountNumber SellerAccount { get; set; }
        public ECKeyPair NewAccountKey { get; set; }
        public TransferType TransactionStyle { get; set; }        

        public TransferTransaction(Stream stream)
        {
            LoadFromStream(stream);
        }
        public TransferTransaction()
        {

        }
        public override byte[] GetHash()
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(SignerAccount);
                    bw.Write(NumberOfOperations);
                    bw.Write(TargetAccount);
                    bw.Write(Amount);
                    bw.Write(Fee);
                    if (Payload.Length > 0) bw.Write((byte[]) Payload);
                    bw.Write((ushort)AccountKey.CurveType);
                    if (AccountKey?.PublicKey.X != null && AccountKey.PublicKey.X.Length > 0 && AccountKey.PublicKey.Y.Length > 0)
                    {
                        bw.Write(AccountKey.PublicKey.X);
                        bw.Write(AccountKey.PublicKey.Y);
                    }
                    ms.Position = 0;
                    byte[] b = ms.ToArray();
                    ms = null;
                    return b;
                }
            }
            finally
            {
                ms?.Dispose();
            }        
        }
        public override void SaveToStream(Stream s)
        {
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write((uint)SignerAccount);
                bw.Write(NumberOfOperations);
                bw.Write((uint)TargetAccount);
                bw.Write(Amount);
                bw.Write(Fee);
                Payload.SaveToStream(bw);
                AccountKey.SaveToStream(s, false);
                if(TransactionStyle == TransferType.BuyAccount || TransactionStyle == TransferType.TransactionAndBuyAccount)
                {
                    bw.Write((byte)TransactionStyle);
                    bw.Write(AccountPrice);
                    bw.Write((uint)SellerAccount);
                    NewAccountKey.SaveToStream(s, false);
                }
                Signature.SaveToStream(s);
            }
        }
        public override void LoadFromStream(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                SignerAccount = br.ReadUInt32();
                NumberOfOperations = br.ReadUInt32();
                TargetAccount = br.ReadUInt32();
                Amount = br.ReadUInt64();
                Fee = br.ReadUInt64();
                Payload = ByteString.ReadFromStream(br);
                AccountKey = new ECKeyPair();
                AccountKey.LoadFromStream(stream, false);
                //stream.Position -= 1;
                byte b = br.ReadByte();
                TransactionStyle = (TransferType)b;
                if (b > 2) { stream.Position -= 1; TransactionStyle = TransferType.Transaction; TransactionType = TransactionType.Transaction; }
                if (b > 0 && b < 3)
                {
                    AccountPrice = br.ReadUInt64();
                    SellerAccount = br.ReadUInt32();
                    NewAccountKey = new ECKeyPair();
                    NewAccountKey.LoadFromStream(stream, false);
                    switch (TransactionStyle)
                    {
                        case TransferType.BuyAccount: TransactionType = TransactionType.BuyAccount; break;
                        case TransferType.TransactionAndBuyAccount: TransactionType = TransactionType.BuyAccount; break;
                        default: TransactionType = TransactionType.Transaction; break;
                    }
                }
                Signature = new ECSignature(stream);
                
            }

        }
        public override bool IsValid()
        {
            if (!base.IsValid()) return false;
            if (TransactionStyle != TransferType.BuyAccount) return true;
            if (AccountPrice < 0) return false;
            if (!SellerAccount.IsValid()) return false;
            return true;
        }
    }
}