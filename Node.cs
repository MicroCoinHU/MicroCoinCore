//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// Node.cs - Copyright (c) 2018 Németh Péter
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using MicroCoin.Chain;
using MicroCoin.Cryptography;
using MicroCoin.Mining;
using MicroCoin.Net;
using MicroCoin.Protocol;
using MicroCoin.Transactions;
using MicroCoin.Util;

namespace MicroCoin
{
    public class Node : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static Node _sInstance;
        private static object nodeLock = new object();
        public event EventHandler<BlocksDownloadProgressEventArgs> BlockDownloadProgress;
        public List<ITransaction> PendingTransactions { get; set; } = new List<ITransaction>();
        protected Node()
        {
        }

        public ECKeyPair NodeKey { get; set; } = ECKeyPair.CreateNew(false);
        public static IList<ECKeyPair> Keys { get; set; }        
        public NodeServerList NodeServers { get; set; } = new NodeServerList();
        public BlockChain BlockChain { get; set; } = BlockChain.Instance;
        public List<Account> Accounts => CheckPoints.Accounts;
        public static Node Instance
        {
            get => _sInstance ?? (_sInstance = new Node());
            set => _sInstance = value;
        }
        protected Thread ListenerThread { get; set; }
        private static readonly List<string> Transmitted = new List<string>();
        public MinerServer MinerServer { get; set; }

        protected List<MicroCoinClient> Clients { get; set; } = new List<MicroCoinClient>();

        public static NetParams NetParams { get; set; }

        public static void Initialize(NetParams netParams)
        {
            NetParams = netParams;
        }

        public async Task<Node> StartNode( object parameters)
        {
            return await StartNode(NetParams.Port);
        }
        public async Task<Node> StartNode(IList<ECKeyPair> keys = null)
        {
#if NETCOREAPP
            SetupConsoleLogger();
#else
            XmlConfigurator.Configure(File.OpenRead("log4net.config"));
#endif
            Keys = keys;
            await CheckRemoteBlockChain();
            Log.Info("BlockChain OK");
            LoadCheckPoints();
            GC.Collect();
            NodeServers.NewNode += (sender, ev) =>
            {                
                SetupNewClient(ev.Node.MicroCoinClient);
            };
            SetupFixSeedServers();
            P2PClient.ServerPort = NetParams.Port;
            Listen();
            SetupMinerServer();
            return Instance;
        }

        private void LoadCheckPoints()
        {
            CheckPoints.Init();
            if (!File.Exists(NetParams.CheckPointFileName))
            {
                var cpList = CheckPoints.BuildFromBlockChain(BlockChain.Instance);
                var cpFile = File.Create(NetParams.CheckPointFileName);
                var indexFile = File.Create(NetParams.CheckPointIndexName);
                CheckPoints.SaveList(cpList, cpFile, indexFile);
                cpFile.Dispose();
                indexFile.Dispose();
            }
            if (File.Exists(NetParams.CheckPointIndexName))
            {
                CheckPoints.Init();
                if (CheckPoints.GetLastBlock() != null)
                {
                    var blocks = CheckPoints.GetLastBlock().BlockNumber;
                    var need = BlockChain.Instance.GetLastBlock().BlockNumber;
                    for (var i = blocks; i <= need; i++)
                        CheckPoints.AppendBlock(BlockChain.Instance.Get((int)i));
                }
                var ind = (BlockChain.BlockHeight() + 1) % NetParams.CheckPointFrequency;
                for (int bb = ind; bb > 0; bb--)
                {
                    int c = (BlockChain.BlockHeight() + 1) - bb;
                    CheckPoints.AppendBlock(BlockChain.Instance.Get(c));
                }
            }
            else
            {
                throw new FileNotFoundException("Checkpoint file not found", NetParams.CheckPointIndexName);
            }
        }

        private async Task CheckRemoteBlockChain()
        {
            var MicroCoinClient = new MicroCoinClient();
            var bl = BlockChain.Instance.BlockHeight();
            do
            {
                for (int i = 0; i < NetParams.FixedSeedServers.Count; i++)
                {
                    MicroCoinClient.Connect(NetParams.FixedSeedServers[i], NetParams.Port);
                    if (MicroCoinClient.Connected) break;
                }

                if (MicroCoinClient.Connected)
                {
                    var response = await MicroCoinClient.SendHelloAsync();
                    if (BlockChain.Instance.GetLastBlock().CompactTarget < response.Block.CompactTarget)
                    {
                        if (bl == response.Block.BlockNumber)
                        {
                            throw new Exception("Ajjaj!");
                        }
                    }
                    uint blockChunk = NetParams.MaxBlockInPacket;
                    while (bl <= response.Block.BlockNumber)
                    {
                        try
                        {
                            var blocks = MicroCoinClient.RequestBlocksAsync((uint)bl, blockChunk); //response.TransactionBlock.BlockNumber);                    
                            Log.InfoFormat("Received {0} blocks", blocks.Blocks.Count);
                            if (blocks.Blocks.Count > 0 && blocks.Blocks.First().BlockNumber != bl) continue;
                            if (blocks.Blocks.Count < blockChunk && blocks.Blocks.Last().BlockNumber < response.Block.BlockNumber)
                            {
                                blockChunk = ((uint)blocks.Blocks.Count / 100) * 100;
                                Log.WarnFormat("Changed download count to {0}", blockChunk);
                                continue;
                            }
                            try
                            {
                                BlockChain.Instance.AppendAll(blocks.Blocks, true);
                            }
                            catch
                            {
                                continue;
                            }
                            bl += (int)blockChunk;
                            BlockDownloadProgress?.Invoke(this, new BlocksDownloadProgressEventArgs
                            {
                                BlocksToDownload = (int)response.Block.BlockNumber,
                                DownloadedBlocks = bl
                            });
                            GC.Collect();
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            } while (!MicroCoinClient.Connected);
            MicroCoinClient.Dispose();
            MicroCoinClient = null;
        }

        private void SetupMinerServer()
        {
            MinerServer = new MinerServer();
            MinerServer.Start();
        }

        protected void SetupFixSeedServers()
        {
            for (int i = 0; i < NetParams.FixedSeedServers.Count; i++)
            {
                NodeServers.TryAddNew(NetParams.FixedSeedServers[i] + ":" + NetParams.Port.ToString(), new NodeServer
                {
                    IP = NetParams.FixedSeedServers[i],
                    LastConnection = DateTime.Now,
                    Port = NetParams.Port
                });
            }
        }

        protected void SetupNewClient(MicroCoinClient microCoinClient)
        {
            microCoinClient.HelloResponse += (o, e) =>
            {
                if (BlockChain.Instance.BlockHeight() < e.HelloResponse.Block.BlockNumber)
                {
                    var bl = BlockChain.Instance.BlockHeight();
                    if (bl <= e.HelloResponse.Block.BlockNumber)
                    {
                        microCoinClient.RequestBlockChain((uint)(BlockChain.Instance.BlockHeight()), 100);
                    }
                }
            };
            microCoinClient.BlockResponse += (ob, eb) =>
            {
                BlockChain.Instance.AppendAll(eb.BlockResponse.Blocks);
                foreach (var b in eb.BlockResponse.Blocks)
                {
                    foreach (var t in b.Transactions)
                    {
                        if (PendingTransactions.Count(p => p.GetHash().SequenceEqual(t.GetHash())) > 0)
                        {
                            var trans = PendingTransactions.FirstOrDefault(p => p.GetHash().SequenceEqual(t.GetHash()));
                            PendingTransactions.Remove(trans);
                        }
                    }
                }
                microCoinClient.SendHello();
            };
            microCoinClient.NewTransaction += (o, e) =>
            {
                string hash = e.Transaction.GetHash();
                if (Transmitted.Contains(hash))
                {
                    return;
                }
                var client = (MicroCoinClient)o;
                var ip = ((IPEndPoint)client.TcpClient.Client.RemoteEndPoint).Address.ToString();
                Transmitted.Add(hash);
                if (e.Transaction.Transactions[0] is TransferTransaction t)
                {
                    if (!t.IsValid() || !t.SignatureValid())
                    {
                        return;
                    }
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    foreach (var c in Instance.NodeServers)
                    {
                        if (c.Value.IP == ip) continue;
                        ms.Position = 0;
                        try
                        {
                            c.Value.MicroCoinClient.SendRaw(ms);
                        }
                        catch
                        {
                        }
                    }
                }
                PendingTransactions.AddRange(e.Transaction.Transactions);
                foreach (var tr in e.Transaction.Transactions)
                {
                    CheckPoints.ApplyTransaction(tr);
                }
            };
            microCoinClient.NewBlock += (o, e) =>
            {
                BlockChain.Instance.Append(e.Block);
                foreach (var t in e.Block.Transactions)
                {
                    if (PendingTransactions.Count(p => p.GetHash().SequenceEqual(t.GetHash())) > 0)
                    {
                        var trans = PendingTransactions.FirstOrDefault(p => p.GetHash().SequenceEqual(t.GetHash()));
                        PendingTransactions.Remove(trans);
                    }
                }
            };
            microCoinClient.SendHello();
        }

        internal void SendNewBlock(Block block)
        {
            NewBlockRequest request = new NewBlockRequest();
            request.Block = block;
            using (MemoryStream ms = new MemoryStream())
            {
                request.SaveToStream(ms);                
                foreach (var c in Instance.NodeServers)
                {                    
                    ms.Position = 0;
                    try
                    {
                        c.Value.MicroCoinClient.SendRaw(ms);
                    }
                    catch
                    {
                    }
                }
            }
        }

        protected void Listen()
        {
            ListenerThread = new Thread(() =>
            {
                try
                {
                    try
                    {
                        var tcpListener = new TcpListener(IPAddress.Any, NetParams.Port); //
                        try
                        {
                            P2PClient.ServerPort = NetParams.Port;
                            tcpListener.Start();
                            var connected = new ManualResetEvent(false);
                            while (true)
                            {
                                connected.Reset();
                                var asyncResult = tcpListener.BeginAcceptTcpClient(state =>
                                {
                                    try
                                    {
                                        var client = tcpListener.EndAcceptTcpClient(state);
                                        var mClient = new MicroCoinClient();
                                        mClient.Disconnected += (o, e) => { Clients.Remove((MicroCoinClient) o); };
                                        Clients.Add(mClient);
                                        mClient.Handle(client);
                                        connected.Set();
                                    }
                                    catch (ObjectDisposedException)
                                    {
                                    }
                                }, null);
                                while (!connected.WaitOne(1));
                            }
                        }
                        catch (ThreadAbortException)
                        {
                            tcpListener.Stop();
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                finally
                {
                    Log.Warn("Listener exited");
                }
            });
            ListenerThread.Name = "Node Server";
            ListenerThread.Start();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            foreach (var c in Clients)
            {
                if (c.IsDisposed) continue;
                Clients.Remove(c);
                c.Dispose();
            }

            if (MinerServer != null)
            {
                MinerServer.Stop = true;
                MinerServer = null;                
            }
            ListenerThread?.Abort();            
            NodeServers?.Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void SendTransaction(Transaction transaction)
        {
            if (!transaction.IsValid()) throw new InvalidOperationException("Hibás tranzakció");
            NewTransactionMessage message = new NewTransactionMessage
            {
                Operation = NetOperationType.AddOperations,
                RequestType = RequestType.AutoSend,
                TransactionCount = 1,
                Transactions = new ITransaction[] { transaction }
            };
            using (Stream s = new MemoryStream())
            {
                message.SaveToStream(s);
                s.Position = 0;
                NodeServers.BroadCastMessage(s);
            }
        }

        public bool SendCoin(Account sender, Account target, decimal amount, decimal fee, ECKeyPair key, string payload)
        {
            var transaction = new TransferTransaction
            {
                Amount = (ulong)(amount * 10000M),
                Fee = (ulong)(fee * 10000M),
                Payload = payload,
                SignerAccount = sender.AccountNumber,
                TargetAccount = target.AccountNumber,
                TransactionStyle = TransferTransaction.TransferType.Transaction,
                TransactionType = TransactionType.Transaction,
                AccountKey = key
            };
            CheckPoints.Account(transaction.SignerAccount).NumberOfOperations++;
            transaction.NumberOfOperations = CheckPoints.Account(transaction.SignerAccount).NumberOfOperations;
            if (!transaction.IsValid()) throw new Exception("Érvénytelen tranzakció");
            transaction.Signature = transaction.GetSignature();
            if (!transaction.SignatureValid()) throw new InvalidDataException("Érvénytelen aláírás!");
            sender.Balance -= transaction.Amount + transaction.Fee;
            target.Balance += transaction.Amount;
            sender.Saved = false;
            target.Saved = false;
            Instance.SendTransaction(transaction);
            return true;
        }
        public bool ChangeAccountInfo(Account account, decimal fee, string payload, ECKeyPair key)
        {
            var transaction = new ChangeAccountInfoTransaction
            {
                NewName = account.Name,
                Payload = payload,
                Fee = (ulong)(fee * 10000M),
                TargetAccount = account.AccountNumber,
                ChangeType = (byte)ChangeAccountInfoTransaction.AccountInfoChangeType.AccountName,
                SignerAccount = account.AccountNumber,
                NumberOfOperations = ++account.NumberOfOperations,
                AccountKey = account.AccountInfo.AccountKey,
                NewAccountKey = account.AccountInfo.NewPublicKey
            };
            transaction.AccountKey = key;
            transaction.Signature = transaction.GetSignature();
            account.Saved = false;
            if (transaction.NewAccountKey.CurveType != CurveType.Empty)
                transaction.ChangeType |= (byte) ChangeAccountInfoTransaction.AccountInfoChangeType.PublicKey;
            Instance.SendTransaction(transaction);
            return true;
        }
        public bool SellAccount(Account account, decimal price, decimal fee, AccountNumber seller, ECKeyPair key)
        {
            var transaction = new ListAccountTransaction
            {
                AccountPrice = (ulong) (price * 10000M),
                AccountToPay = seller,
                Fee = (ulong) (fee * 10000M),
                NumberOfOperations = ++account.NumberOfOperations,
                SignerAccount = account.AccountNumber,
                TargetAccount = account.AccountNumber,
                Payload = "",
                TransactionType = TransactionType.ListAccountForSale,
                AccountKey = key,
                LockedUntilBlock = 0
            };
            transaction.Signature = transaction.GetSignature();
            Instance.SendTransaction(transaction);
            return true;
        }
        public bool ChangeAccountKey(Account account, decimal fee, string payload, Account signer, ECKeyPair key,
            string newKey)
        {
            var transaction = new ChangeKeyTransaction
            {
                AccountKey = key,
                Fee = (ulong)(fee * 10000M),
                NewAccountKey = ECKeyPair.FromEncodedString(newKey),
                NumberOfOperations = ++signer.NumberOfOperations,
                Payload = payload,
                SignerAccount = signer.AccountNumber,
                TargetAccount = account.AccountNumber,
                TransactionType = TransactionType.ChangeKeySigned
            };
            transaction.Signature = transaction.GetSignature();
            Instance.SendTransaction(transaction);
            signer.Saved = account.Saved = false;
            return true;
        }
        public bool BuyAccount(Account account, decimal fee, string payload, Account buyer, ECKeyPair key)
        {
            var transaction = new TransferTransaction
            {
                Amount = account.AccountInfo.Price,
                Fee = (ulong)(fee * 10000M),
                Payload = payload,
                SignerAccount = buyer.AccountNumber,
                TargetAccount = account.AccountNumber,
                TransactionStyle = TransferTransaction.TransferType.BuyAccount,
                TransactionType = TransactionType.BuyAccount,
                AccountKey = key,
                AccountPrice = account.AccountInfo.Price,
                NewAccountKey = key
            };
            CheckPoints.Account(transaction.SignerAccount).NumberOfOperations++;
            transaction.NumberOfOperations = CheckPoints.Account(transaction.SignerAccount).NumberOfOperations;
            transaction.Signature = transaction.GetSignature();
            var seller =
                CheckPoints.Accounts.FirstOrDefault(p => p.AccountNumber == account.AccountInfo.AccountToPayPrice);
            if(seller==null) throw new NullReferenceException("No seller");
            seller.Balance += transaction.Amount;
            buyer.Balance -= transaction.Amount + transaction.Fee;
            transaction.SellerAccount = seller.AccountNumber;
            buyer.Saved = false;
            seller.Saved = false;
            account.Saved = false;
            Instance.SendTransaction(transaction);
            return true;
        }

        protected void SetupConsoleLogger()
        {
#if !NETCOREAPP
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly());
            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date{yyyy-MM-dd HH:mm:ss} %-5level %logger - %message%newline";
            patternLayout.ActivateOptions();
            ManagedColoredConsoleAppender consoleAppender = new ManagedColoredConsoleAppender();
            consoleAppender.Layout = patternLayout;
            consoleAppender.AddMapping(new ManagedColoredConsoleAppender.LevelColors()
            {
                ForeColor = ConsoleColor.Yellow,
                Level = Level.Warn
            });
            consoleAppender.AddMapping(new ManagedColoredConsoleAppender.LevelColors()
            {
                ForeColor = ConsoleColor.Cyan,
                Level = Level.Info
            });
            consoleAppender.AddMapping(new ManagedColoredConsoleAppender.LevelColors()
            {
                ForeColor = ConsoleColor.DarkGray,
                Level = Level.Debug
            });

            consoleAppender.AddMapping(new ManagedColoredConsoleAppender.LevelColors()
            {
                ForeColor = ConsoleColor.Red,
                Level = Level.Error
            });
            consoleAppender.ActivateOptions();
            hierarchy.Root.AddAppender(consoleAppender);
            //MemoryAppender memory = new MemoryAppender();
            //memory.ActivateOptions();
            //hierarchy.Root.AddAppender(memory);
            hierarchy.Root.Level = Level.Debug;
            hierarchy.Configured = true;
#endif
        }
    }
}
