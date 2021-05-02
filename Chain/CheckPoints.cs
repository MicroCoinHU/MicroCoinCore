//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// CheckPoints.cs - Copyright (c) 2018 Németh Péter
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
using System.Security.Cryptography;
using System.Text;

namespace MicroCoin.Chain
{

    public class CheckPoints
    {
        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static event EventHandler<CheckPointBuildingEventArgs> CheckPointBuilding;
        private static List<uint> _offsets = new List<uint>();
        internal static ulong WorkSum { get; set; }
        internal static Hash OldCheckPointHash { get; set; }
        internal static List<Account> Accounts { get; set; } = new List<Account>();
        internal static List<CheckPointBlock> Current { get; set; } = new List<CheckPointBlock>();
        public static List<ITransaction> AppliedTransactions { get; private set; } = new List<ITransaction>();

        internal static void Init()
        {
            WorkSum = 0;
            Current = new List<CheckPointBlock>();
            if (!File.Exists((Node.NetParams.CheckPointIndexName))) return;
            try
            {
                FileStream fs = File.Open(Node.NetParams.CheckPointIndexName, FileMode.Open);
                _offsets = new List<uint>((int) (fs.Length / 4));
                Accounts = new List<Account>();
                using (BinaryReader br = new BinaryReader(fs))
                {
                    while (fs.Position < fs.Length)
                    {
                        _offsets.Add(br.ReadUInt32());
                    }
                }

                using (FileStream cf = File.OpenRead(Node.NetParams.CheckPointFileName))
                {
                    while (cf.Position < cf.Length)
                    {
                        var block = new CheckPointBlock(cf);
                        WorkSum += block.CompactTarget;
                        Accounts.AddRange(block.Accounts);
                        Current.Add(block);
                    }
                }
            }
            catch
            {
            }
            if (Accounts.Count > 0)
            {
                Log.Info($"Accounts: {Accounts.Last().AccountNumber}");
            }
        }
        internal static void Put(CheckPointBlock cb)
        {
            using (FileStream fs = File.OpenWrite(Node.NetParams.CheckPointFileName))
            {
                uint position;
                if (_offsets.Count <= cb.BlockNumber)
                {
                    position = (uint) fs.Length;
                    _offsets.Add(position);
                }
                else
                {
                    position = _offsets[(int) cb.BlockNumber];
                }

                fs.Position = position;
                cb.SaveToStream(fs);
            }
        }
        internal static CheckPointBlock Get(uint i)
        {
            return Current[(int) i];
        }
        internal static Account Account(int i)
        {
            return Accounts[i];
        }
        internal static void SaveList(List<CheckPointBlock> list, Stream stream, Stream indexStream)
        {
            List<uint> offsets = new List<uint>();
            foreach (var item in list)
            {
                long pos = stream.Position;
                offsets.Add((uint) pos);
                item.SaveToStream(stream);
                if (item.BlockNumber % 100 == 0)
                {
                    CheckPointBuilding?.Invoke(new object(), new CheckPointBuildingEventArgs
                    {
                        BlocksDone = (int)(item.BlockNumber),
                        BlocksNeeded = (int)(list.Count)
                    });
                }
            }

            using (BinaryWriter bw = new BinaryWriter(indexStream, Encoding.Default, true))
            {
                foreach (uint u in offsets)
                {
                    bw.Write(u);
                }
            }
        }
        internal static List<CheckPointBlock> ReadAll()
        {
            List<CheckPointBlock> list = new List<CheckPointBlock>(_offsets.Count);
            using (FileStream fs = File.OpenRead(Node.NetParams.CheckPointFileName))
            {
                while (fs.Position < fs.Length)
                {
                    list.Add(new CheckPointBlock(fs));
                }
            }

            return list;
        }
        internal static Hash CheckPointHash(List<CheckPointBlock> checkpoint)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // 957B9DF462FA2A4B60FEE64A10BCCCB0099E8D0B91B36CD1FD36AEE8CF413E6BD235B37162C2F0F09839836C614F94F47D75A7F62D18F21D3D6D7793B5A664888420BD9AFD49A8D2D1B01031B07B169C2474F63A8F96C8A863206E3BA6EB681F23BF3DAB37303ECF18846DB6D644A003C28121A58D089B12600FD593C022642A318A85A2A5AE0E53BF527CD0064F9510D53F553862E6E86D4B55A20C7D18A07F093A1B5488CE01FE5E100A6EA14FDD288D0608A14ABFC1CC424833690D8522EE8AC3B79D842FF368138F9BA25AD9A7C9D638519D1DABA84FABEBAF65520E2EFEE220D7FE02C7667D6EF254082F6548BE9FCE14A638591F887F671C4D193BD6E73E2A03796834695891812EC86FDCC6AB24C7A38EB440E77D399809AAFB16306892CC6ADC1A3AD43316ECFAAD886929E125CA0C963AEBB8E56F8F397BC27296D8CF8B9EBD3E55E3875AC24F099F71B527CB392C49B74BF63B48A85A977957D97751E43C2705474805AD543EAADE88366DBC625A25354ABA2A836CF89413DFB9671B19C4DA57A96E9FE73842383BB4FC0D1F46374350EF26C1F7742F96CE642942E5BDCB3BE29A7DAA2FF59615F123A6784FC97DF231C6FC82F80CCE2DEE89761E539BB7CBEAEB09A8A091A6B858D41502CE28384059DEFDCE3F3CCB8AF2C4D77529933051F771F4448D2EB72F1C8251D8B05ECD969369E9906EF1A23610BE7590C210C24B57B040BD59276E3F2EC8EB99627E1720A1832A4D1C0418FA56040702614D2B4C634FE86A474F9070A6FBDC9A85BC69C940FEE07F12BBBCEF7D26A898702D2071535911F320F539B6D05C7AB85BEC3E88730C32EECF0AB7D2FE8EFE9EF7415FDA776D49ACD61535B30E2F9417E2D2109BC51E229177C98D5C7E7ADEB1244ECD0FCE5D0A119CC4AF1287DED4A1EB3104D1D282146BB2A74BDD319666EE27A6419146E16CB0202EA2271B3622EAF364B5C4F6E86FA935FC8B709E7F051C8EAB3CEE054C626760E7AB5287F48A7CE1EC0A0D3E8E5F69A60E59B0DAC018C3292AD1A0B21665D111684BF292429C539182C9A25039B5403FD2AE144604F7C92DE1DF4E21471FBD2A38AEE229AFB00C937466676153486AB3369C20FF147CCE802098F5D77482E1A059B2908733AD2967AED432083D53B4AFC7FC3A380EB5C00420BF4237E15B7ABCF7942F7BC39ED134538C69957E789BB69AC8D12EF904681F9EB3CCBAE667A6D382540D572A7FC7A311CD0215A8B153B3306F207EE712EEF4DAC0750467E965A482F4BD19A6A8F00692244A92AEED9F56634EAF06D9AD31A00A6073E493F8CB703BA8009CE08FBD24E63796AD8FE96DBF2FDC902CA510EB
                foreach (var block in checkpoint)                    
                {                    
                    block.BlockHash = block.CalculateBlockHash(true);
                    ms.Write(block.BlockHash, 0, block.BlockHash.Length);
                    var sha1 = new SHA256Managed();
                    var p = ms.Position;
                    ms.Position = 0;
                    Hash h=sha1.ComputeHash(ms);
                    Hash h2 = h.Reverse();
                    ms.Position = p;
                }
                if (ms.Length == 0)
                {
                    byte[] b = Encoding.ASCII.GetBytes(Node.NetParams.GenesisPayload);
                    ms.Write(b, 0, b.Length);
                }
                var sha = new SHA256Managed();
                ms.Position = 0;
                return sha.ComputeHash(ms);
            }
        }
        internal static List<CheckPointBlock> BuildFromBlockChain(BlockChain blockChain)
        {
            List<CheckPointBlock> checkPoint = new List<CheckPointBlock>(blockChain.BlockHeight() + 1);
            uint accNumber = 0;
            ulong accWork = 0;
            for (int block = 0; block < 100 * ((blockChain.GetLastBlock().BlockNumber + 1) / 100); block++)
            {
                Block currentBlock = blockChain.Get(block);
                if (currentBlock == null)
                {
                    var h = blockChain.GetLastBlock().BlockNumber;
                    Block currentBlock1 = blockChain.Get((int)block);
                    continue;
                }
                CheckPointBlock checkPointBlock = new CheckPointBlock {AccountKey = currentBlock.AccountKey};
                for (int i = 0; i < 5; i++)
                {
                    checkPointBlock.Accounts.Add(new Account
                    {
                        AccountNumber = accNumber,
                        Balance = (i == 0 ? 1000000ul + (ulong) currentBlock.Fee : 0ul),
                        BlockNumber = currentBlock.BlockNumber,
                        UpdatedBlock = currentBlock.BlockNumber,
                        NumberOfOperations = 0,
                        AccountType = 0,
                        Name = "",
                        UpdatedByBlock = currentBlock.BlockNumber,
                        AccountInfo = new AccountInfo
                        {
                            AccountKey = currentBlock.AccountKey,
                            State = AccountState.Normal
                        }
                    });
                    accNumber++;
                }

                accWork += currentBlock.CompactTarget;
                checkPointBlock.AccumulatedWork = accWork;
                checkPointBlock.AvailableProtocol = currentBlock.AvailableProtocol;
                checkPointBlock.BlockNumber = currentBlock.BlockNumber;
                checkPointBlock.BlockSignature = 2; //b.BlockSignature;
                checkPointBlock.CheckPointHash = currentBlock.CheckPointHash;
                checkPointBlock.CompactTarget = currentBlock.CompactTarget;
                checkPointBlock.Fee = currentBlock.Fee;
                checkPointBlock.Nonce = currentBlock.Nonce;
                checkPointBlock.Payload = currentBlock.Payload;
                checkPointBlock.ProofOfWork = currentBlock.ProofOfWork;
                checkPointBlock.ProtocolVersion = currentBlock.ProtocolVersion;
                checkPointBlock.Reward = currentBlock.Reward;
                checkPointBlock.Timestamp = currentBlock.Timestamp;
                checkPointBlock.TransactionHash = currentBlock.TransactionHash;
                WorkSum += currentBlock.CompactTarget;
                foreach (var t in currentBlock.Transactions)
                {
                    Account account;
                    var signer = checkPoint.FirstOrDefault(p =>
                        p.Accounts.Count(a => a.AccountNumber == t.SignerAccount) > 0);
                    var target = checkPoint.FirstOrDefault(p =>
                        p.Accounts.Count(a => a.AccountNumber == t.TargetAccount) > 0);
                    if (t.Fee != 0) signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                    switch (t.TransactionType)
                    {
                        case TransactionType.Transaction:
                            TransferTransaction transfer = (TransferTransaction) t;
                            if (signer != null && target != null)
                            {
                                if (t.Fee == 0)
                                    signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                                signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -=
                                    (transfer.Fee + transfer.Amount);
                                target.Accounts.First(p => p.AccountNumber == t.TargetAccount).Balance +=
                                    transfer.Amount;
                                signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).UpdatedBlock =
                                    currentBlock.BlockNumber;
                                target.Accounts.First(p => p.AccountNumber == t.TargetAccount).UpdatedBlock =
                                    currentBlock.BlockNumber;
                            }

                            break;
                        case TransactionType.BuyAccount:
                            TransferTransaction transferTransaction = (TransferTransaction) t; // TODO: be kell fejezni
                            if (t.Fee == 0)
                                signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -=
                                (transferTransaction.Fee + transferTransaction.Amount);
                            CheckPointBlock seller = checkPoint.FirstOrDefault(p =>
                                p.Accounts.Count(a => a.AccountNumber == transferTransaction.SellerAccount) > 0);
                            seller.Accounts.First(p => p.AccountNumber == transferTransaction.SellerAccount).Balance +=
                                transferTransaction.Amount;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).UpdatedBlock = currentBlock.BlockNumber;
                            target.Accounts.First(p => p.AccountNumber == t.TargetAccount).UpdatedBlock = currentBlock.BlockNumber;
                            seller.Accounts.First(p => p.AccountNumber == transferTransaction.SellerAccount)
                                .UpdatedBlock = currentBlock.BlockNumber;
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
                            ListAccountTransaction listAccountTransaction = (ListAccountTransaction) t;
                            account = target.Accounts.First(p => p.AccountNumber == t.TargetAccount);
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -=
                                listAccountTransaction.Fee;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).UpdatedBlock = currentBlock.BlockNumber;
                            target.Accounts.First(p => p.AccountNumber == t.TargetAccount).UpdatedBlock = currentBlock.BlockNumber;
                            if (t.Fee == 0)
                                signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                            if (listAccountTransaction.TransactionType == TransactionType.ListAccountForSale)
                            {
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
                            ChangeAccountInfoTransaction changeAccountInfoTransaction =
                                (ChangeAccountInfoTransaction) t;
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

                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -=
                                changeAccountInfoTransaction.Fee;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).UpdatedBlock = currentBlock.BlockNumber;
                            account.UpdatedBlock = currentBlock.BlockNumber;
                            if (t.Fee == 0)
                                signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                            break;
                        case TransactionType.ChangeKey:
                        case TransactionType.ChangeKeySigned:
                            ChangeKeyTransaction changeKeyTransaction = (ChangeKeyTransaction) t;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -=
                                changeKeyTransaction.Fee;
                            account = target.Accounts.First(p => p.AccountNumber == t.TargetAccount);
                            account.AccountInfo.AccountKey = changeKeyTransaction.NewAccountKey;
                            account.UpdatedBlock = currentBlock.BlockNumber;
                            signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).UpdatedBlock = currentBlock.BlockNumber;
                            if (t.Fee == 0)
                                signer.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                            break;
                    }
                }

                checkPoint.Add(checkPointBlock);
                if (block % 100 == 0)
                {
                    CheckPointBuilding?.Invoke(new object(), new CheckPointBuildingEventArgs
                    {
                        BlocksDone = block,
                        BlocksNeeded = (int)(100 * ((blockChain.GetLastBlock().BlockNumber + 1) / 100))*2
                    });
                }
            }

            foreach (var p in checkPoint) { p.BlockHash = p.CalculateBlockHash();

                if (p.BlockNumber % 100 == 0)
                {
                    CheckPointBuilding?.Invoke(new object(), new CheckPointBuildingEventArgs
                    {
                        BlocksDone = (int)(p.BlockNumber+checkPoint.Count),
                        BlocksNeeded = (int)(checkPoint.Count * 2)
                    });
                }

            }
            return checkPoint;
        }
        internal static CheckPointBlock GetLastBlock()
        {
            return Current.Count > 0 ? Current.Last() : null;
        }
        protected static void SaveNext()
        {
            var offsets2 = new List<uint>();
            uint chunk = (Current.Last().BlockNumber / 100) % 10;
            Log.Info($"Saving next checkpont => {chunk}");
            using (FileStream fs = File.Create(Node.NetParams.CheckPointFileName + $".{chunk}"))
            {
                foreach (var block in Current)
                {
                    offsets2.Add((uint) fs.Position);
                    block.SaveToStream(fs);
                }
            }

            using (FileStream fs = File.Create(Node.NetParams.CheckPointIndexName + $".{chunk}"))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    foreach (var o in offsets2) bw.Write(o);
                }
            }

            File.Copy(Node.NetParams.CheckPointIndexName + $".{chunk}", Node.NetParams.CheckPointIndexName, true);
            File.Copy(Node.NetParams.CheckPointFileName + $".{chunk}", Node.NetParams.CheckPointFileName, true);
        }

        public static void ApplyTransaction(ITransaction t, Block b = null)
        {
            Account targetAccount = null;
            Account signerAccount = null;
            try
            {
                signerAccount = Accounts[t.SignerAccount];
                targetAccount = Accounts[t.TargetAccount];
            }
            catch
            {
                SaveNext();
                signerAccount = Accounts[t.SignerAccount];
                targetAccount = Accounts[t.TargetAccount];
            }
            targetAccount.Saved = false;
            signerAccount.Saved = false;
            var signerBlock = Get(signerAccount.BlockNumber);
            var targetBlock = Get(targetAccount.BlockNumber);
            if (t.Fee != 0) signerAccount.NumberOfOperations++;
            if (b != null)
            {
                signerAccount.UpdatedBlock = b.BlockNumber;
                targetAccount.UpdatedBlock = b.BlockNumber;
                targetAccount.Saved = true;
                signerAccount.Saved = true;
            }
            if (TransactionType.BuyAccount == t.TransactionType)
            {
                TransferTransaction transferTransaction = (TransferTransaction)t;
                Account sellerAccount = Accounts[transferTransaction.SellerAccount];
                sellerAccount.Saved = false;
                if (b != null)
                {
                    sellerAccount.UpdatedBlock = b.BlockNumber;
                    sellerAccount.Saved = true;
                }
            }
            if (AppliedTransactions.Count(p => p.GetHash().SequenceEqual(t.GetHash())) > 0)
                return; // FDon't apply twice
            switch (t.TransactionType)
            {
                case TransactionType.Transaction:
                    TransferTransaction transfer = (TransferTransaction)t;
                    if (signerBlock != null && targetBlock != null)
                    {
                        if (t.Fee == 0) signerAccount.NumberOfOperations++;
                        signerAccount.Balance -= (transfer.Fee + transfer.Amount);
                        targetAccount.Balance += transfer.Amount;
                    }

                    break;
                case TransactionType.BuyAccount:
                    TransferTransaction transferTransaction = (TransferTransaction)t;
                    if (t.Fee == 0)
                        signerBlock.Accounts.First(p => p.AccountNumber == t.SignerAccount).NumberOfOperations++;
                    signerBlock.Accounts.First(p => p.AccountNumber == t.SignerAccount).Balance -=
                        (transferTransaction.Fee + transferTransaction.Amount);
                    Account sellerAccount = Accounts[transferTransaction.SellerAccount];
                    Get(sellerAccount.BlockNumber);
                    sellerAccount.Balance += transferTransaction.Amount;
                    targetAccount.AccountInfo.AccountKey = transferTransaction.NewAccountKey;
                    targetAccount.AccountInfo.Price = 0;
                    targetAccount.AccountInfo.LockedUntilBlock = 0;
                    targetAccount.AccountInfo.State = AccountState.Normal;
                    targetAccount.AccountInfo.AccountToPayPrice = 0;
                    targetAccount.AccountInfo.NewPublicKey = null;
                    break;
                case TransactionType.DeListAccountForSale:
                case TransactionType.ListAccountForSale:
                    ListAccountTransaction listAccountTransaction = (ListAccountTransaction)t;
                    signerAccount.Balance -= listAccountTransaction.Fee;
                    if (t.Fee == 0) signerAccount.NumberOfOperations++;
                    if (signerBlock != null && targetBlock != null)
                    {
                        if (listAccountTransaction.TransactionType == TransactionType.ListAccountForSale)
                        {
                            targetAccount.AccountInfo.Price = listAccountTransaction.AccountPrice;
                            targetAccount.AccountInfo.LockedUntilBlock = listAccountTransaction.LockedUntilBlock;
                            targetAccount.AccountInfo.State = AccountState.Sale;
                            targetAccount.AccountInfo.Price = listAccountTransaction.AccountPrice;
                            targetAccount.AccountInfo.NewPublicKey = listAccountTransaction.NewPublicKey;
                            targetAccount.AccountInfo.AccountToPayPrice = listAccountTransaction.AccountToPay;
                        }
                        else
                        {
                            targetAccount.AccountInfo.State = AccountState.Normal;
                            targetAccount.AccountInfo.Price = 0;
                            targetAccount.AccountInfo.NewPublicKey = null;
                            targetAccount.AccountInfo.LockedUntilBlock = 0;
                            targetAccount.AccountInfo.AccountToPayPrice = 0;
                        }
                    }
                    break;
                case TransactionType.ChangeAccountInfo:
                    ChangeAccountInfoTransaction changeAccountInfoTransaction = (ChangeAccountInfoTransaction)t;
                    if ((changeAccountInfoTransaction.ChangeType & 1) == 1)
                    {
                        targetAccount.AccountInfo.AccountKey = changeAccountInfoTransaction.NewAccountKey;
                    }

                    if ((changeAccountInfoTransaction.ChangeType & 4) == 4)
                    {
                        targetAccount.AccountType = changeAccountInfoTransaction.NewType;
                    }

                    if ((changeAccountInfoTransaction.ChangeType & 2) == 2)
                    {
                        targetAccount.Name = changeAccountInfoTransaction.NewName;
                    }

                    signerAccount.Balance -= changeAccountInfoTransaction.Fee;
                    if (t.Fee == 0) signerAccount.NumberOfOperations++;
                    break;
                case TransactionType.ChangeKey:
                case TransactionType.ChangeKeySigned:
                    ChangeKeyTransaction changeKeyTransaction = (ChangeKeyTransaction)t;
                    signerAccount.Balance -= changeKeyTransaction.Fee;
                    targetAccount.AccountInfo.AccountKey = changeKeyTransaction.NewAccountKey;
                    if (t.Fee == 0) signerAccount.NumberOfOperations++;
                    break;
            }
            AppliedTransactions.Add(t);
        }

        internal static void AppendBlock(Block b)
        {
            int lastBlock = -1;
            if (GetLastBlock() != null)
            {
                if (b.BlockNumber <= GetLastBlock().BlockNumber) return;
                lastBlock = (int) GetLastBlock().BlockNumber;
            }

            CheckPointBlock checkPointBlock = new CheckPointBlock {AccountKey = b.AccountKey};
            uint accNumber = (uint) (lastBlock + 1) * 5;
            if (accNumber == 0)
            {
                Log.Info("NULL");
            }

            ulong accWork = WorkSum;
            for (int i = 0; i < 5; i++)
            {                
                checkPointBlock.Accounts.Add(new Account
                {
                    AccountNumber = accNumber,
                    Balance = (i == 0 ? 1000000ul + (ulong) b.Fee : 0ul),
                    BlockNumber = b.BlockNumber,
                    UpdatedBlock = b.BlockNumber,
                    NumberOfOperations = 0,
                    AccountType = 0,
                    Name = "",
                    UpdatedByBlock = b.BlockNumber,
                    AccountInfo = new AccountInfo
                    {
                        AccountKey = b.AccountKey,
                        State = AccountState.Normal
                    }
                });
                accNumber++;
            }

            accWork += b.CompactTarget;
            WorkSum += b.CompactTarget;
            checkPointBlock.AccumulatedWork = accWork;
            checkPointBlock.AvailableProtocol = b.AvailableProtocol;
            checkPointBlock.BlockNumber = b.BlockNumber;
            checkPointBlock.BlockSignature = 2;
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
            foreach (var t in b.Transactions)
            {
                ApplyTransaction(t, b);
            }
            Current.Add(checkPointBlock);
            Accounts.AddRange(checkPointBlock.Accounts);
            if ((checkPointBlock.BlockNumber + 1) % 100 == 0)
            {
                SaveNext();
            }
            OldCheckPointHash = CheckPointHash(Current);
        }
    }
}
