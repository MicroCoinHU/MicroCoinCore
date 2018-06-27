//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// ChangeAccountInfoTransaction.cs - Copyright (c) 2018 Németh Péter
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
    public sealed class ChangeAccountInfoTransaction : Transaction
    {

        public enum AccountInfoChangeType : byte { PublicKey = 1, AccountName = 2, AccountType = 3 }

        public byte ChangeType { get; set; }

        public ECKeyPair NewAccountKey { get; set; } = new ECKeyPair();

        public ByteString NewName { get; set; }

        public ushort NewType { get; set; }

        public ChangeAccountInfoTransaction() {
            TransactionType = TransactionType.ChangeAccountInfo;
        }

        public ChangeAccountInfoTransaction(Stream stream)
        {
            LoadFromStream(stream);
        }

        public override void SaveToStream(Stream s)
        {
            using(BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write(SignerAccount);
                bw.Write(TargetAccount);
                bw.Write(NumberOfOperations);
                bw.Write(Fee);
                Payload.SaveToStream(bw);
                AccountKey.SaveToStream(s, false);
                bw.Write(ChangeType);
                NewAccountKey.SaveToStream(s,false);
                NewName.SaveToStream(bw);
                bw.Write(NewType);
                Signature.SaveToStream(s);
            }
        }

        public override void LoadFromStream(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.Default, true))
            {
                SignerAccount = br.ReadUInt32();
                TargetAccount = br.ReadUInt32();
                NumberOfOperations = br.ReadUInt32();
                Fee = br.ReadUInt64();
                Payload = ByteString.ReadFromStream(br);
                AccountKey = new ECKeyPair();
                AccountKey.LoadFromStream(stream, false);
                ChangeType = br.ReadByte();
                NewAccountKey = new ECKeyPair();
                NewAccountKey.LoadFromStream(stream, false);                
                NewName = ByteString.ReadFromStream(br);
                NewType = br.ReadUInt16();
                Signature = new ECSignature(stream);
            }
        }

        public override byte[] GetHash()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using(BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(SignerAccount);
                    bw.Write(TargetAccount);
                    bw.Write(NumberOfOperations);
                    bw.Write(Fee);
                    Payload.SaveToStream(bw);
                    AccountKey.SaveToStream(ms, false);
                    bw.Write(ChangeType);
                    NewAccountKey.SaveToStream(ms, false);
                    NewName.SaveToStream(bw);
                    bw.Write(NewType);
                    return ms.ToArray();
                }
            }
        }

        public override bool IsValid()
        {
            return base.IsValid();
        }
    }
}