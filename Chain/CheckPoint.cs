//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// CheckPoint.cs - Copyright (c) 2018 Németh Péter
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


using log4net;
using MicroCoin.Transactions;
using MicroCoin.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MicroCoin.Chain
{
    public class CheckPoint
    {
#if !NETCOREAPP
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly object LoadLock = new object();
        private Stream _stream;
        public const string CheckPointFileName = "checkpoint";
        public ulong WorkSum { get; set; }
        public CheckPointHeader Header { get; set; }

        public uint BlockCount
        {
            get
            {
                if (Header == null) return 0;
                return Header.BlockCount;
            }
        }
        public List<Account> Accounts { get; set; } = new List<Account>();                

        public CheckPointBlock this[uint i]
        {
            get
            {
                long p = _stream.Position;
//		log.Info(Header.BlockOffset(i));
                _stream.Position = Header.BlockOffset(i);
                var block = new CheckPointBlock(_stream);
                _stream.Position = p;
                return block;
            }
        }

        public CheckPoint()
        {

        }

        public CheckPoint(Stream s)
        {
            LoadFromStream(s);
        } 
        
        public static void SaveList(List<CheckPointBlock> list, Stream stream, Stream indexStream)
        {
            var offsets = new List<uint>();
            foreach(var item in list)
            {
                long pos = stream.Position;
                offsets.Add((uint)pos);
                item.SaveToStream(stream);
            }
            using (var bw = new BinaryWriter(indexStream, Encoding.Default, true))
            {
                foreach (var u in offsets)
                {
                    bw.Write(u);
                }
            }
        }

        public static Hash CheckPointHash(List<CheckPointBlock> checkpoint)
        {
            using (var ms = new MemoryStream())
            {
                foreach (var block in checkpoint)
                {
                    ms.Write(block.BlockHash, 0, block.BlockHash.Length);
                }
                var sha = new System.Security.Cryptography.SHA256Managed();
                ms.Position = 0;
                return sha.ComputeHash(ms);
            }           
        }

        public static List<CheckPointBlock> BuildFromBlockChain(BlockChain blockChain)
        {
            var checkPoint = new List<CheckPointBlock>(blockChain.BlockHeight()+1);
            uint accNumber = 0;
            ulong accWork = 0;
            for (int block=0; block < 100*((blockChain.GetLastBlock().BlockNumber+1)/100); block++)                
            {
#if !NETCOREAPP
                if (block % 1000==0)
                {
                    Log.Info($"Building checkpont: {block} block");
                }
#endif
                Block b = blockChain.Get(block);
                var checkPointBlock = new CheckPointBlock {AccountKey = b.AccountKey};
                for (var i = 0; i < 5; i++) {
                    checkPointBlock.Accounts.Add(new Account
                    {
                        AccountNumber=accNumber,
                        Balance = (i==0)?1000000ul+(ulong)b.Fee:0ul,
                        BlockNumber = b.BlockNumber,
                        UpdatedBlock = b.BlockNumber,
                        NumberOfOperations=0,
                        AccountType = 0,
                        Name="",
                        UpdatedByBlock=b.BlockNumber,
                        AccountInfo = new AccountInfo
                        {
                            AccountKey = b.AccountKey,
                            State = AccountState.Normal                            
                        }
                    });
                    accNumber++;
                }
                accWork+=b.CompactTarget;                
                checkPointBlock.AccumulatedWork = accWork;
                checkPointBlock.AvailableProtocol = b.AvailableProtocol;
                checkPointBlock.BlockNumber = b.BlockNumber;
                checkPointBlock.BlockSignature = 2;//b.BlockSignature;
                checkPointBlock.CheckPointHash = b.CheckPointHash;
                checkPointBlock.CompactTarget = b.CompactTarget;
                checkPointBlock.Fee = b.Fee;
                checkPointBlock.Nonce = b.Nonce;
                checkPointBlock.Payload = b.Payload;
                checkPointBlock.ProofOfWork = b.ProofOfWork;
                checkPointBlock.ProtocolVersion = b.ProtocolVersion;
                checkPointBlock.Reward = b.Reward;
                checkPointBlock.Timestamp = b.Timestamp;
                checkPointBlock.TransactionHash = b.TransactionHash;
                foreach(var t in b.Transactions)
                {
                    Account account;
                    var signer = checkPoint.FirstOrDefault(p => p.Accounts.Count(a => a.AccountNumber == t.SignerAccount) > 0);
                    var target = checkPoint.FirstOrDefault(p => p.Accounts.Count(a => a.AccountNumber == t.TargetAccount) > 0);
                    if(signer==null) throw new NullReferenceException("Signer account is null");
                    if(t.Fee!=0) signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                    switch (t.TransactionType)
                    {
                        case TransactionType.Transaction:
                            var transfer = (TransferTransaction)t;
                            if(target != null)
                            {
                                if (t.Fee == 0) signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                                signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -= transfer.Fee + transfer.Amount;
                                target.Accounts.First(p => p.AccountNumber == t.TargetAccount).Balance += transfer.Amount;
                                signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).UpdatedBlock = b.BlockNumber;
                                target.Accounts.First(p => p.AccountNumber == t.TargetAccount).UpdatedBlock = b.BlockNumber;
                            }
                            break;
                        case TransactionType.BuyAccount:
                            TransferTransaction transferTransaction = (TransferTransaction)t; // TODO: be kell fejezni
                            if (t.Fee == 0) signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -= transferTransaction.Fee + transferTransaction.Amount;
                            CheckPointBlock seller = checkPoint.FirstOrDefault(p => p.Accounts.Count(a => a.AccountNumber == transferTransaction.SellerAccount) > 0);
                            seller.Accounts.First(p => p.AccountNumber == transferTransaction.SellerAccount).Balance += transferTransaction.Amount;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).UpdatedBlock = b.BlockNumber;
                            target.Accounts.First(p => p.AccountNumber == t.TargetAccount).UpdatedBlock = b.BlockNumber;
                            seller.Accounts.First(p => p.AccountNumber == transferTransaction.SellerAccount).UpdatedBlock = b.BlockNumber;
                            account = target.Accounts.First(p => p.AccountNumber == t.TargetAccount);
                            account.AccountInfo.AccountKey = transferTransaction.NewAccountKey;
                            account.AccountInfo.Price = 0;
                            account.AccountInfo.LockedUntilBlock = 0;
                            account.AccountInfo.State = AccountState.Normal;
                            account.AccountInfo.AccountToPayPrice = 0;
                            account.AccountInfo.NewPublicKey = null;
                            break;
                        case TransactionType.DeListAccountForSale:
                        case TransactionType.ListAccountForSale:
                            ListAccountTransaction listAccountTransaction = (ListAccountTransaction)t;
                            account = target.Accounts.First(p => p.AccountNumber == t.TargetAccount);
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -= listAccountTransaction.Fee;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).UpdatedBlock = b.BlockNumber;
                            target.Accounts.First(p => p.AccountNumber == t.TargetAccount).UpdatedBlock = b.BlockNumber;
                            if (t.Fee == 0) signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                            if (listAccountTransaction.TransactionType == TransactionType.ListAccountForSale) {
                                account.AccountInfo.Price = listAccountTransaction.AccountPrice;
                                account.AccountInfo.LockedUntilBlock = listAccountTransaction.LockedUntilBlock;
                                account.AccountInfo.State = AccountState.Sale;                                    
                                account.AccountInfo.Price = listAccountTransaction.AccountPrice;
                                account.AccountInfo.NewPublicKey = listAccountTransaction.NewPublicKey;
                                account.AccountInfo.AccountToPayPrice = listAccountTransaction.AccountToPay;
                            }
                            else
                            {
                                account.AccountInfo.State = AccountState.Normal;
                                account.AccountInfo.Price = 0;
                                account.AccountInfo.NewPublicKey = null;
                                account.AccountInfo.LockedUntilBlock = 0;
                                account.AccountInfo.AccountToPayPrice = 0;
                            }
                            break;
                        case TransactionType.ChangeAccountInfo:
                            ChangeAccountInfoTransaction changeAccountInfoTransaction = (ChangeAccountInfoTransaction)t;
                            account = target.Accounts.First(p => p.AccountNumber == t.TargetAccount);
                            if ((changeAccountInfoTransaction.ChangeType & 1) == 1)
                            {
                                account.AccountInfo.AccountKey = changeAccountInfoTransaction.NewAccountKey;
                            }
                            if ((changeAccountInfoTransaction.ChangeType & 4) == 4)
                            {
                                account.AccountType = changeAccountInfoTransaction.NewType;
                            }
                            if ((changeAccountInfoTransaction.ChangeType & 2) == 2)
                            {
                                account.Name = changeAccountInfoTransaction.NewName;
                            }
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -= changeAccountInfoTransaction.Fee;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).UpdatedBlock = b.BlockNumber;
                            account.UpdatedBlock = b.BlockNumber;                            
                            if (t.Fee == 0) signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                            break;
                        case TransactionType.ChangeKey:
                        case TransactionType.ChangeKeySigned:
                            ChangeKeyTransaction changeKeyTransaction = (ChangeKeyTransaction)t;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -= changeKeyTransaction.Fee;
                            account = target.Accounts.First(p => p.AccountNumber == t.TargetAccount);
                            account.AccountInfo.AccountKey = changeKeyTransaction.NewAccountKey;
                            account.UpdatedBlock = b.BlockNumber;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).UpdatedBlock = b.BlockNumber;
                            if (t.Fee == 0) signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                            break;
                    }
                }
                checkPoint.Add(checkPointBlock);
            }
            foreach(var p in checkPoint) p.BlockHash = p.CalculateBlockHash();
            return checkPoint;
        }

        public void LoadFromFile(string filename)
        {
            FileStream fs = File.Open(filename, FileMode.Open, FileAccess.ReadWrite);
            _stream?.Dispose();
            _stream = null;
            _stream = fs;
            Accounts.Clear();
            Accounts = null;
            GC.Collect();
            Header = new CheckPointHeader(_stream);
            Accounts = new List<Account>();
            WorkSum = 0;
            for (uint i = 0; i < Header.EndBlock - Header.StartBlock; i++)
            {
                WorkSum += this[i].CompactTarget;
                foreach (Account a in this[i].Accounts)
                {
                    
                    Accounts.Add(a);
                    // Accounts.AddRange(b.Accounts);
                }
            }
        }
        public void LoadFromStream(Stream s)
        {
            _stream?.Dispose();
            _stream = null;
            _stream = new MemoryStream();
            s.CopyTo(_stream);
            // stream = s;
            _stream.Position = 0;
            Header = new CheckPointHeader(_stream);
            Accounts = new List<Account>();
            WorkSum = 0;
            for (uint i = 0; i < Header.EndBlock - Header.StartBlock; i++)
            {
                try
                {
                    var b = this[i];
                    WorkSum += b.CompactTarget;
                    Accounts.AddRange(b.Accounts);
                }
                catch (Exception e)
                {
#if !NETCOREAPP
                    Log.Error(e.Message, e);
#endif
                }
            }
#if !NETCOREAPP
            Log.Info(WorkSum);
#endif
        }

        public void Append(CheckPoint checkPoint)
        {
            lock (LoadLock)
            {
                var list = new List<CheckPointBlock>();
                if (Header != null)
                {
                    for (uint i = Header.StartBlock; i < Header.EndBlock+1; i++)
                    {
                        list.Add(this[i]);
                    }
                    if (Header.BlockCount > checkPoint.Header.BlockCount)
                    {
                        return;
                    }
                    Header.BlockCount = checkPoint.Header.BlockCount;
                    Header.Hash = checkPoint.Header.Hash;
                    Header.EndBlock = checkPoint.Header.EndBlock;
                }
                else
                {
                    Header = checkPoint.Header;
                }
                for (uint i = 0; i != checkPoint.Header.EndBlock- checkPoint.Header.StartBlock + 1; i++){
//		    log.Info($"Added block {i}");
                    list.Add(checkPoint[i]);
		}
                var ms = new MemoryStream();
                Header.Offsets = new uint[list.Count + 1];
                Header.SaveToStream(ms);
                long headerSize = ms.Position;
                ms.Position = 0;
//		log.Info("checkPoint 1");
                using (var bw = new BinaryWriter(ms, Encoding.Default, true))
                {
                    uint i = 0;
                    foreach (var b in list)
                    {
                        Header.Offsets[i] = (uint)(ms.Position + headerSize);
                        b.SaveToStream(ms);
                        i++;
                    }
                }
//		log.Info("checkPoint 2");
                var memoryStream = new MemoryStream();
                Header.SaveToStream(memoryStream);
                ms.Position = 0;
                ms.CopyTo(memoryStream);
                memoryStream.Write(Header.Hash, 0, Header.Hash.Length);
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
                memoryStream.Position = 0;
//		log.Info("checkPoint 3");
                LoadFromStream(memoryStream);
//		log.Info("checkPoint 4");
                ms.Dispose();
                memoryStream.Dispose();
                GC.Collect();
//		log.Info("checkPoint appended");
            }
        }

        public void SaveToStream(Stream s)
        {
            long pos = _stream.Position;
            _stream.Position = 0;
            _stream.CopyTo(s);
            _stream.Position = pos;
        }

        public void Dispose()
        {
            if (_stream == null) return;
            _stream.Dispose();                
            Accounts.Clear();
            Accounts = null;
            _stream = null;
        }

        public CheckPoint SaveChunk(uint startBlock, uint endBlock)
        {
            lock (LoadLock)
            {
                var list = new List<CheckPointBlock>();
                var header = new CheckPointHeader
                {
                    StartBlock = startBlock,
                    EndBlock = endBlock,
                    BlockCount = endBlock - startBlock + 1
                };
                for (var i = startBlock; i <= endBlock; i++)
                    list.Add(this[i]);
                var ms = new MemoryStream();
                header.Offsets = new uint[list.Count + 1];
                header.SaveToStream(ms);
                long headerSize = ms.Position;
                ms.Position = 0;
                using (var bw = new BinaryWriter(ms, Encoding.Default, true))
                {
                    uint i = 0;
                    foreach (var b in list)
                    {
                        header.Offsets[i] = (uint)(ms.Position + headerSize);
                        b.SaveToStream(ms);
                        i++;
                    }
                }
                var memoryStream = new MemoryStream();
                header.SaveToStream(memoryStream);
                ms.Position = 0;
                ms.CopyTo(memoryStream);
                memoryStream.Write(header.Hash, 0, header.Hash.Length);
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
                memoryStream.Position = 0;
                var checkPoint = new CheckPoint(memoryStream);
                ms.Dispose();
                memoryStream.Dispose();
                GC.Collect();
                return checkPoint;
            }
        }

    }
}