//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// Transaction.cs - Copyright (c) 2018 Németh Péter
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
using MicroCoin.Chain;
using System.Linq;
using System.Text;

namespace MicroCoin.Transactions
{
    public enum TransactionType : uint
    {
        None = 0,
        Transaction = 1, ChangeKey, RecoverFounds, ListAccountForSale,
        DeListAccountForSale, BuyAccount, ChangeKeySigned, ChangeAccountInfo
    }

    public abstract class Transaction : ITransaction
    {
        private ByteString _payload;

        public AccountNumber SignerAccount { get; set; }
        public uint NumberOfOperations { get; set; }
        public AccountNumber TargetAccount { get; set; }
        public ByteString Payload
        {
            get
            {
                if (_payload.IsReadable) return _payload;
                ByteString bs = (string)(new Hash(_payload));
                return bs;
            }
            set => _payload = value;
        }
        public ECSignature Signature { get; set; }
        public ECKeyPair AccountKey { get; set; }
        public MCC Fee { get; set; }
        public MCC Amount { get; set; }
        public abstract byte[] GetHash();
        public abstract void SaveToStream(Stream s);
        public abstract void LoadFromStream(Stream s);
        public TransactionType TransactionType { get; set; }
        public ECSignature GetSignature()
        {
            return Utils.GenerateSignature(GetHash(), AccountKey);
        }
        public bool SignatureValid()
        {
            var accountKey = AccountKey;
            if (AccountKey == null || AccountKey.CurveType == CurveType.Empty)
                accountKey = CheckPoints.Accounts[SignerAccount].AccountInfo.AccountKey;
            return Utils.ValidateSignature(GetHash(), Signature, accountKey);
        }

        public virtual bool IsValid()
        {
            if (SignerAccount < 0) return false;
            if (CheckPoints.Accounts.Count(p => p.AccountNumber == SignerAccount) == 0) return false;
            if (CheckPoints.Accounts.Count(p => p.AccountNumber == TargetAccount) == 0) return false;
            Account Signer = CheckPoints.Accounts.FirstOrDefault(p => p.AccountNumber == SignerAccount);
            if (Signer.BlockNumber >=
                Node.Instance.BlockChain.BlockHeight() - Node.NetParams.MinimumBlocksToUseAccount) return false;
            if (CheckPoints.Accounts.FirstOrDefault(p => p.AccountNumber == TargetAccount).BlockNumber >=
                Node.Instance.BlockChain.BlockHeight() - Node.NetParams.MinimumBlocksToUseAccount) return false;
            if (Amount < 0) return false;
            if (Fee < 0) return false;
            if (Amount + Fee > Signer.Balance) return false;
            if (
                (NumberOfOperations != SignerAccount.Account().NumberOfOperations) &&
                (NumberOfOperations != SignerAccount.Account().NumberOfOperations + 1) // Bug fix hack
                ) return false;
            return true;
        }

        public Hash GetOpHash(uint block)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(block);
                    bw.Write(SignerAccount);
                    bw.Write(NumberOfOperations);
                    Hash data;
                    using (MemoryStream m = new MemoryStream())
                    {
                        SaveToStream(m);
                        data = m.ToArray();                                                
                    }
                    Hash hh = Utils.RipeMD160(data);
                    string s = hh;
                    s = s.Substring(0, 20);
                    bw.Write(Encoding.ASCII.GetBytes(s), 0, 20);
                    return ms.ToArray();
                }
            }
            finally
            {
                ms?.Dispose();
            }
        }

        public Hash Serialize()
        {
            using(var ms = new MemoryStream())
            {
                SaveToStream(ms);
                return ms.ToArray();
            }
        }
    }
}
