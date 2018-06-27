//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// CheckPointBlock.cs - Copyright (c) 2018 Németh Péter
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
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MicroCoin.Chain
{
    /// <summary>
    /// One entry in the checkpoint
    /// </summary>
    public class CheckPointBlock : BlockBase, IEquatable<CheckPointBlock>
    {
        /// <summary>
        /// List of all accounts
        /// </summary>
        /// <value>The accounts.</value>
        public List<Account> Accounts { get; set; } = new List<Account>(5);
        /// <summary>
        /// The block hash
        /// </summary>
        /// <value>The block hash.</value>
        public Hash BlockHash { get; set; }
        /// <summary>
        /// Gets or sets the accumulated work.
        /// </summary>
        /// <value>The accumulated work.</value>
        public ulong AccumulatedWork { get; set; }

        internal override void SaveToStream(Stream stream)
        {
            using(BinaryWriter bw = new BinaryWriter(stream, Encoding.Default, true))
            {
                SaveToStream(bw);
            }
        }

        internal void SaveToStream(BinaryWriter bw, bool saveHash = true, bool proto1 = false)
        {
            bw.Write(BlockNumber);
            if (!proto1)
            {
                AccountKey.SaveToStream(bw.BaseStream, false);
                bw.Write(Reward);
                bw.Write(Fee);
                bw.Write(ProtocolVersion);
                bw.Write(AvailableProtocol);
                bw.Write(Timestamp);
                bw.Write(CompactTarget);
                bw.Write(Nonce);
                Payload.SaveToStream(bw);
                CheckPointHash.SaveToStream(bw);
                TransactionHash.SaveToStream(bw);
                ProofOfWork.SaveToStream(bw);
            }
            for (int i = 0; i < 5; i++)
            {
                Accounts[i].SaveToStream(bw, saveHash, !proto1);
            }
            if (proto1) bw.Write(Timestamp);
            if (saveHash)
            {
                BlockHash.SaveToStream(bw);
            }
            if (!proto1)
            {
                bw.Write(AccumulatedWork);
            }
        }

        public CheckPointBlock() 
        {

        }

        internal Hash CalculateBlockHash(bool checkproto = false)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    //SaveToStream(bw, false, checkproto ? ProtocolVersion<2 : false);
                    SaveToStream(bw, false, false);
                    ms.Position = 0;
                    using (SHA256Managed sha = new SHA256Managed())
                    {
                        return sha.ComputeHash(ms);
                    }
                }
            }
            finally
            {                
                ms.Dispose();
                ms = null;
            }
        }

        public bool Equals(CheckPointBlock other)
        {
            if (other == null) return false;
            if (other.AccumulatedWork != AccumulatedWork) return false;
            if (other.AvailableProtocol != AvailableProtocol) return false;
            if (other.BlockNumber != BlockNumber) return false;
        //    if (other.BlockSignature != BlockSignature) return false;            
            if (other.CheckPointHash != CheckPointHash) return false;
            if (other.CompactTarget != CompactTarget) return false;
            if (other.Fee != Fee) return false;
            if (other.Nonce != Nonce) return false;
            if (other.Payload != Payload) return false;
            if (other.ProofOfWork != ProofOfWork) return false;
            if (other.ProtocolVersion != ProtocolVersion) return false;
            if (other.Reward != Reward) return false;
            if (other.Timestamp != Timestamp) return false;
            if (other.TransactionHash != TransactionHash) return false;
            if (other.Accounts.Count != Accounts.Count) return false;
            if (!other.AccountKey.Equals(AccountKey)) return false;
            for (int i = 0; i < Accounts.Count; i++)
            {
                if (!Accounts[i].Equals(other.Accounts[i])) return false;
            }
            return true;
        }

        internal CheckPointBlock(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.Default, true))
            {
                BlockNumber = br.ReadUInt32();
                AccountKey = new ECKeyPair();
                AccountKey.LoadFromStream(stream, false);
                Reward = br.ReadUInt64();
                Fee = br.ReadUInt64();
                ProtocolVersion = br.ReadUInt16();
                AvailableProtocol = br.ReadUInt16();
                Timestamp = br.ReadUInt32();
                CompactTarget = br.ReadUInt32();
                Nonce = br.ReadInt32();
                ushort len = br.ReadUInt16();
                Payload = br.ReadBytes(len);
                len = br.ReadUInt16();
                CheckPointHash = br.ReadBytes(len);
                len = br.ReadUInt16();
                TransactionHash = br.ReadBytes(len);
                len = br.ReadUInt16();
                ProofOfWork = br.ReadBytes(len);
                for (int i = 0; i < 5; i++)
                {
                    Account acc = new Account(stream);
                    Accounts.Add(acc);
                }
                BlockHash = Hash.ReadFromStream(br);
                AccumulatedWork = br.ReadUInt64();
            }
        }
    }
}
