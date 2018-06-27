//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// BlockBase.cs - Copyright (c) 2018 Németh Péter
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


using System.CodeDom;
using System.Diagnostics;
using MicroCoin.Cryptography;
using MicroCoin.Util;
using System.IO;
using System.Text;
using log4net.Util;

namespace MicroCoin.Chain
{

    // TPCOperationsComp
    /// <summary>
    /// One block in the blockchain
    /// </summary>
    public class BlockBase
    {

        internal byte BlockSignature { get; set; } = 3;
        internal ushort ProtocolVersion { get; set; }
        internal ushort AvailableProtocol { get; set; }
        public uint BlockNumber { get; set; }
        public ECKeyPair AccountKey { get; set; }
        public MCC Reward { get; set; }
        public MCC Fee { get; set; }
        public Timestamp Timestamp { get; set; }        
        public uint CompactTarget { get; set; }
        public int Nonce { get; set; }
        public ByteString Payload { get; set; }
        public Hash CheckPointHash { get; set; }
        public Hash TransactionHash { get; set; }
        public Hash ProofOfWork { get; set; }
        public static BlockBase GenesisBlock => new BlockBase
        {
            BlockNumber = 0
        };

        internal BlockBase(Stream s)
        {            
            using (var br = new BinaryReader(s, Encoding.ASCII, true))
            {
                BlockSignature = br.ReadByte();
                if (BlockSignature > 0)
                {
                    ProtocolVersion = br.ReadUInt16();
                    AvailableProtocol = br.ReadUInt16();
                }
                BlockNumber = br.ReadUInt32();
                AccountKey = new ECKeyPair();
                AccountKey.LoadFromStream(s);
                Reward = br.ReadUInt64();
                Fee = br.ReadUInt64();
                Timestamp = br.ReadUInt32();
                CompactTarget = br.ReadUInt32();
                Nonce = br.ReadInt32();
                ushort pl = br.ReadUInt16();
                if (pl > 0)
                {
                    Payload = br.ReadBytes(pl);
                }
                pl = br.ReadUInt16();
                if (pl > 0)
                {
                    CheckPointHash = br.ReadBytes(pl);
                }
                pl = br.ReadUInt16();
                if (pl > 0)
                {
                    TransactionHash = br.ReadBytes(pl);
                }
                pl = br.ReadUInt16();
                if (pl > 0)
                {
                    ProofOfWork = br.ReadBytes(pl);
                }
            }
        }

        internal BlockBase()
        {
            AccountKey = new ECKeyPair();
        }

        internal virtual void SaveToStream(Stream s)
        {
            using (var bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write(BlockSignature);
                bw.Write(ProtocolVersion);
                bw.Write(AvailableProtocol);
                bw.Write(BlockNumber);
                if (AccountKey == null)
                {
                    bw.Write((ushort)6);
                    bw.Write((ushort)0);
                    bw.Write((ushort)0);
                    bw.Write((ushort)0);
                }
                else
                {
                    AccountKey.SaveToStream(s);
                }
                bw.Write(Reward);
                bw.Write(Fee);
                bw.Write(Timestamp);
                bw.Write(CompactTarget);
                bw.Write(Nonce);
                Payload.SaveToStream(bw);
                CheckPointHash.SaveToStream(bw);
                TransactionHash.SaveToStream(bw);
                ProofOfWork.SaveToStream(bw);
            }
        }

    }
}