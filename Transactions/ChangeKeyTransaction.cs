//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// ChangeKeyTransaction.cs - Copyright (c) 2018 Németh Péter
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

namespace MicroCoin.Transactions
{
    public sealed class ChangeKeyTransaction : Transaction
    {
        public ECKeyPair NewAccountKey { get; set; }

        public ChangeKeyTransaction()
        {
        }

        public ChangeKeyTransaction(Stream s, TransactionType transactionType)
        {
            TransactionType = transactionType;
            LoadFromStream(s);
        }

        public override void SaveToStream(Stream s)
        {
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write(SignerAccount);
                if (TransactionType == TransactionType.ChangeKeySigned)
                {
                    bw.Write(TargetAccount);
                }
                bw.Write(NumberOfOperations);
                bw.Write(Fee);
                Payload.SaveToStream(bw);
                AccountKey.SaveToStream(s, false);
                NewAccountKey.SaveToStream(s);
                Signature.SaveToStream(s);
            }
        }

        public override void LoadFromStream(Stream s)
        {
            using (BinaryReader br = new BinaryReader(s, Encoding.Default, true))
            {
                SignerAccount = br.ReadUInt32();
                switch (TransactionType)
                {
                    case TransactionType.ChangeKey:
                        TargetAccount = SignerAccount;
                        break;
                    case TransactionType.ChangeKeySigned:
                        TargetAccount = br.ReadUInt32();
                        break;
                }

                NumberOfOperations = br.ReadUInt32();
                Fee = br.ReadUInt64();
                Payload = ByteString.ReadFromStream(br);
                AccountKey = new ECKeyPair();
                AccountKey.LoadFromStream(s, false);
                NewAccountKey = new ECKeyPair();
                NewAccountKey.LoadFromStream(s);
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
                    if (TargetAccount != SignerAccount)
                    {
                        bw.Write(TargetAccount);
                    }

                    bw.Write(NumberOfOperations);
                    bw.Write(Fee);
                    if (Payload != "")
                    {
                        Payload.SaveToStream(bw, false);
                    }

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

                    NewAccountKey.SaveToStream(ms, false);
                    return ms.ToArray();
                }
            }
        }

        public override bool IsValid()
        {
            if (!base.IsValid()) return false;
            if (NewAccountKey.CurveType == CurveType.Empty) return false;
            if (NewAccountKey.PublicKey.X.Length == 0) return false;
            if (NewAccountKey.PublicKey.Y.Length == 0) return false;
            return true;
        }
    }
}