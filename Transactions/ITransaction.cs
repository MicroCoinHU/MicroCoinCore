//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// ITransaction.cs - Copyright (c) 2018 Németh Péter
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

using System.IO;
using MicroCoin.Cryptography;
using MicroCoin.Util;

namespace MicroCoin.Transactions
{
    public interface ITransaction
    {
        MCC Fee { get; set; }
        ECSignature Signature { get; set; }
        AccountNumber SignerAccount { get; set; }
        AccountNumber TargetAccount { get; set; }
        ECKeyPair AccountKey { get; set; }
        TransactionType TransactionType { get; set; }
        ByteString Payload { get; set; }
        byte[] GetHash();
        Hash Serialize();
        void LoadFromStream(Stream s);
        void SaveToStream(Stream s);
        bool IsValid();
        bool SignatureValid();
        ECSignature GetSignature();
        Hash GetOpHash(uint block);
    }
}
