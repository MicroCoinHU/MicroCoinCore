//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// MicroCoinClient.cs - Copyright (c) 2018 Németh Péter
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


using MicroCoin.Chain;
using MicroCoin.Cryptography;
using MicroCoin.Protocol;
using MicroCoin.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MicroCoin.Net
{

    public enum RequestType : ushort { None = 0, Request, Response, AutoSend, Unknown }
    public enum NetOperationType : ushort
    {
        Hello = 1,
        Error = 2,
        Message = 3,
        Transactions = 0x05,
        Blocks = 0x10,
        NewBlock = 0x11,
        AddOperations = 0x20,
        CheckPoint = 0x21
    }
    public class HelloRequestEventArgs : EventArgs
    {
        public HelloRequest HelloRequest { get; set; }
        public HelloRequestEventArgs(HelloRequest helloRequest)
        {
            HelloRequest = helloRequest;
        }
    }
    public class HelloResponseEventArgs : EventArgs
    {
        public HelloResponse HelloResponse { get; }
        public HelloResponseEventArgs(HelloResponse helloResponse)
        {
            HelloResponse = helloResponse;
        }
    }
    public class BlockResponseEventArgs : EventArgs
    {
        public BlockResponse BlockResponse { get; }
        public BlockResponseEventArgs(BlockResponse blockResponse)
        {
            BlockResponse = blockResponse;
        }
    }
    public class NewBlockEventArgs : EventArgs
    {
        public Block Block { get; set; }

        public NewBlockEventArgs(Block block)
        {
            Block = block;
        }
    }
    public class NewTransactionEventArgs : EventArgs
    {
        public NewTransactionMessage Transaction { get; }

        public NewTransactionEventArgs(NewTransactionMessage transaction)
        {
            Transaction = transaction;
        }
    }

    public class MicroCoinClient : P2PClient
    {        

        public event EventHandler<HelloRequestEventArgs> HelloRequest;
        public event EventHandler<HelloResponseEventArgs> HelloResponse;
        public event EventHandler<BlockResponseEventArgs> BlockResponse;
        public event EventHandler<NewTransactionEventArgs> NewTransaction;
        public event EventHandler<NewBlockEventArgs> NewBlock;

        public Timer Timer { get; set; }

        public MicroCoinClient()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Timer != null)
            {
                Timer.Dispose();
                Timer = null;
            }

            base.Dispose(disposing);
        }

        protected virtual void OnHelloResponse(HelloResponse helloResponse)
        {
            HelloResponse?.Invoke(this, new HelloResponseEventArgs(helloResponse));
        }

        static object transactionLock = new object();

        protected virtual void OnNewTransaction(NewTransactionMessage newTransaction)
        {
                NewTransaction?.Invoke(this, new NewTransactionEventArgs(newTransaction));
        }

        protected virtual void OnGetBlockResponse(BlockResponse blockResponse)
        {
            BlockResponse?.Invoke(this, new BlockResponseEventArgs(blockResponse));
        }

        protected virtual void OnHelloRequest(HelloRequest helloRequest)
        {
            HelloRequest?.Invoke(this, new HelloRequestEventArgs(helloRequest));
        }

        protected virtual void OnNewBlock(NewBlockRequest newBlockRequest)
        {
            NewBlock?.Invoke(this, new NewBlockEventArgs(newBlockRequest.Block));
        }

        internal override void Start()
        {
            base.Start();
            Timer = new Timer(state =>SendHello(), null, 0, 120000);
        }

        internal void SendHello()
        {
            HelloRequest request = new HelloRequest
            {
                AccountKey = Node.Instance.NodeKey,
                AvailableProtocol = 6,
                Error = 0,
                NodeServers = Node.Instance.NodeServers,
                Operation = NetOperationType.Hello
            };
            Hash h = Utils.Sha256(Encoding.ASCII.GetBytes(Node.NetParams.GenesisPayload));
            request.Block = new Block
            {
                AccountKey = null,
                AvailableProtocol = 0,
                BlockNumber = 0,
                CompactTarget = 0,
                Fee = 0,
                Nonce = 0,
                TransactionHash = new byte[0],
                Payload = new byte[0],
                ProofOfWork = new byte[0],
                ProtocolVersion = 0,
                Reward = 0,
                CheckPointHash = h,
                BlockSignature = 3,
                Timestamp = 0
            };
            request.ProtocolVersion = 6;
            request.RequestType = RequestType.Request;
            request.ServerPort = ServerPort;
            request.Timestamp = DateTime.UtcNow;
            request.Version = "2.0.0wN";
            request.WorkSum = 0;
            using (MemoryStream ms = new MemoryStream())
            {
                request.SaveToStream(ms);
                ms.Flush();
                ms.Position = 0;
                SendRaw(ms);
            }            
        }

        private async Task<MessageHeader> ReadResponse(MemoryStream responseStream)
        {
            responseStream.Position = 0;
            return await Task.Run(() =>
            {
                var rp = ReadHeader(responseStream);
                var pos = responseStream.Position;
                if (ReadBody(rp.DataLength, responseStream))
                {
                    FileStream file = File.Create("hellobello");
                    responseStream.Position = 0;
                    responseStream.CopyTo(file);
                    file.Dispose();
                    //throw new Exception("DEV ERROR");
                }
                responseStream.Position = pos;                
                return rp;
            });
        }

        private MessageHeader ReadResponseSync(MemoryStream responseStream)
        {
            responseStream.Position = 0;
            responseStream.Capacity = 0;

            var rp = ReadHeader(responseStream);
            var pos = responseStream.Position;
            ReadBody(rp.DataLength, responseStream);
            if(responseStream.Length!=rp.DataLength+RequestHeader.Size)
            {
                throw new InvalidDataException("Received invalid chunk");
            }
            responseStream.Position = pos;
            return rp;
        }


        internal async Task<HelloResponse> SendHelloAsync()
        {
            HelloRequest request = new HelloRequest
            {
                AccountKey = Node.Instance.NodeKey,
                AvailableProtocol = 6,
                Error = 0,
                NodeServers = Node.Instance.NodeServers,
                Operation = NetOperationType.Hello
            };
            
            byte[] h = Utils.Sha256(Encoding.ASCII.GetBytes(Node.NetParams.GenesisPayload));
            request.Block = new Block
            {
                AccountKey = null,
                AvailableProtocol = 0,
                BlockNumber = 0,
                CompactTarget = 0,
                Fee = 0,
                Nonce = 0,
                TransactionHash = new byte[0],
                Payload = new byte[0],
                ProofOfWork = new byte[0],
                ProtocolVersion = 0,
                Reward = 0,
#if PRODUCTION
                CheckPointHash = h,
#endif
#if TESTNET
                CheckPointHash = new byte[0],
#endif
                BlockSignature = 3,
                Timestamp = 0
            };
            request.ProtocolVersion = 6;
            request.RequestType = RequestType.Request;
            request.ServerPort = ServerPort;
            request.Timestamp = DateTime.UtcNow;
            request.Version = "2.0.0wN";
            request.WorkSum = 0;
            using (MemoryStream ms = new MemoryStream())
            {
                request.SaveToStream(ms);
                ms.Flush();
                ms.Position = 0;
                SendRaw(ms);
            }            
            WaitForData(10000);
            using (var responseStream = new MemoryStream())
            {
                var rp = await ReadResponse(responseStream);
                if (rp.Operation == NetOperationType.Hello)
                {
                    HelloResponse response = new HelloResponse(responseStream, rp);
                    return response;
                }
                throw new InvalidDataException($"Not hello {rp.Operation}");
            }
        }

#if false
        internal async Task<bool> DownloadCheckPointAsync(uint blockCount)
        {
            {
                uint endBlock = (blockCount / 100) * 100;
                var blockResponse = await RequestTransactionBlocksAsync(endBlock, 1);
                var ns = TcpClient.GetStream();
                using (var ms = new MemoryStream())
                {
                    for (uint i = 0; i <= blockResponse.Blocks.Last().BlockNumber / 10000; i++)
                    {
                        CheckPointRequest checkPointRequest =
                            new CheckPointRequest
                            {
                                Operation = NetOperationType.CheckPoint,
                                RequestType = RequestType.Request,
                                StartBlock = i * 10000,
                                EndBlock = ((i + 1) * 10000) - 1
                            };
                        if (checkPointRequest.EndBlock > blockResponse.Blocks.Last().BlockNumber - 1)
                        {
                            checkPointRequest.EndBlock = blockResponse.Blocks.Last().BlockNumber - 1;
                        }
                        checkPointRequest.CheckPointBlockCount = endBlock;
                        checkPointRequest.RequestId = 12345;
                        checkPointRequest.CheckPointHash = blockResponse.Blocks.Last().CheckPointHash;
                        using (MemoryStream requestStream = new MemoryStream())
                        {
                            checkPointRequest.SaveToStream(requestStream);
                            requestStream.Position = 0;
                            requestStream.CopyTo(ns);
                            ns.Flush();
                        }
                        WaitForData(10000);
			            Log.Info("Data received");
                        using (var responseStream = new MemoryStream())
                        {
                            var rp = await ReadResponse(responseStream);
			                Log.Info("All Data received");
                            responseStream.Position = 0;
                            CheckPointResponse response = new CheckPointResponse(responseStream);
                        }
                    }
                    return true;
                }
            }
        }

        internal void DownloadCheckPoint(uint blockCount)
        {
            const uint REQUEST_ID = 123456;
            void Handler(object o, BlockResponseEventArgs e)
            {
                if (e.BlockResponse.RequestId == REQUEST_ID)
                {
                    Log.Debug(e.BlockResponse.Blocks.First().BlockNumber);
                    for (uint i = 0; i <= e.BlockResponse.Blocks.Last().BlockNumber / 10000; i++)
                    {
                        CheckPointRequest checkPointRequest =
                            new CheckPointRequest
                            {
                                Operation = NetOperationType.CheckPoint,
                                RequestType = RequestType.Request,
                                StartBlock = i * 10000,
                                EndBlock = ((i + 1) * 10000) - 1
                            };
                        if (checkPointRequest.EndBlock > e.BlockResponse.Blocks.Last().BlockNumber - 1)
                        {
                            checkPointRequest.EndBlock = e.BlockResponse.Blocks.Last().BlockNumber - 1;
                        }
                        checkPointRequest.CheckPointBlockCount = 14700;
                        checkPointRequest.RequestId = 12345;
                        checkPointRequest.CheckPointHash = e.BlockResponse.Blocks.Last().CheckPointHash;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            checkPointRequest.SaveToStream(ms);
                            NetworkStream ns = TcpClient.GetStream();
                            ms.Position = 0;
                            ms.CopyTo(ns);
                            ns.Flush();
                        }
                    }
                }
            }
            BlockResponse += Handler;
            RequestBlockChain(blockCount, 1, REQUEST_ID, NetOperationType.Transactions);
        }
#endif
        internal BlockResponse RequestBlocksAsync(uint startBlock, uint quantity, uint? requestId = null)
        {
            BlockRequest br = new BlockRequest
            {
                StartBlock = startBlock,
                BlockNumber = quantity,
                Operation = NetOperationType.Blocks
            };
            if (requestId != null)
            {
                br.RequestId = requestId.Value;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                br.SaveToStream(ms);
                ms.Position = 0;
                SendRaw(ms);
            }

            WaitForData(10000);
            using (var rs = new MemoryStream())
            {                
                var rp = ReadResponseSync(rs);
                switch (rp.Operation)
                {
                    case NetOperationType.Blocks:
                        return new BlockResponse(rs, rp);
                    default:
                        throw new InvalidDataException();
                }
            }
        }

        internal async Task<BlockResponse> RequestTransactionBlocksAsync(uint startBlock, uint quantity,
            uint? requestId = null)
        {
            BlockRequest br = new BlockRequest
            {
                StartBlock = startBlock,
                BlockNumber = quantity,
                Operation = NetOperationType.Transactions
            };
            if (requestId != null)
            {
                br.RequestId = requestId.Value;
            }


            using (MemoryStream ms = new MemoryStream())
            {
                br.SaveToStream(ms);
                ms.Position = 0;
                SendRaw(ms);
            }

            WaitForData(10000);
            using (var rs = new MemoryStream())
            {
                var rp = await ReadResponse(rs);
                switch (rp.Operation)
                {
                    case NetOperationType.Transactions:
                        return new BlockResponse(rs, rp);
                    default:
                        throw new InvalidDataException();
                }
            }
        }

        internal void RequestBlockChain(uint startBlock, uint quantity, uint? requestId = null, NetOperationType netOperationType = NetOperationType.Blocks)
        {
            BlockRequest br = new BlockRequest
            {
                StartBlock = startBlock,
                BlockNumber = quantity,
                Operation = netOperationType
            };
            if (requestId != null)
            {
                br.RequestId = requestId.Value;
            }            
            using (MemoryStream ms = new MemoryStream())
            {
                br.SaveToStream(ms);
                ms.Position = 0;
                SendRaw(ms);
            }            
        }

        protected override bool HandleConnection()
        {
            using (var ms = new MemoryStream())
            {
                ms.SetLength(0);
                MessageHeader rp = ReadHeader(ms);
                long pos = ms.Position; // Header end
                ms.Position = ms.Length;
                ReadBody(rp.DataLength, ms);
                if (ms.Length != rp.DataLength + RequestHeader.Size)
                    throw new InvalidDataException("More than expected");
                ms.Position = pos;
                HandleNetworkPacket(rp, ms);
            }            
            return false;
        }

        protected bool ReadBody(int dataLength, MemoryStream ms)
        {
            var requiredSize = dataLength + RequestHeader.Size;
            return ReadData(requiredSize, ms);
        }

        protected MessageHeader ReadHeader(MemoryStream ms)
        {
            ReadData(RequestHeader.Size, ms);
            ms.Position = 0;
            MessageHeader rp = new MessageHeader(ms);            
            if(rp.Magic != Node.NetParams.NetworkPacketMagic) {
                throw new InvalidDataException("Invalid magic / no magic found");
            }
            if (rp.Error != 0)
            {
                throw new Exception("Error in response");
            }
            Log.InfoFormat("Received {0}", rp.Operation);
            return rp;
        }

        protected void HandleNetworkPacket(MessageHeader rp, MemoryStream ms)
        {
            switch (rp.RequestType)
            {
                case RequestType.Response:
                    HandleResponses(rp, ms);
                    break;
                case RequestType.Request:
                    HandleRequests(rp, ms);
                    break;
                case RequestType.AutoSend:
                    HandleAutoSend(rp, ms);
                    break;
            }
        }

        protected virtual void HandleAutoSend(MessageHeader rp, MemoryStream ms)
        {
            switch (rp.Operation)
            {
                case NetOperationType.Error:
                    ByteString buffer = new byte[rp.DataLength];
                    ms.Read(buffer, 0, rp.DataLength);
                    break;
                case NetOperationType.NewBlock:
                    NewBlockRequest response = new NewBlockRequest(ms, rp);
                    OnNewBlock(response);
                    break;
                case NetOperationType.AddOperations:
                    lock (transactionLock)
                    {
                        var newTransaction = new NewTransactionMessage(ms, rp);
                        OnNewTransaction(newTransaction);
                        break;
                    }
            }
        }

        protected virtual void HandleRequests(MessageHeader rp, MemoryStream ms)
        {
            switch (rp.Operation)
            {
                case NetOperationType.Blocks:
                    BlockRequest blockRequest = new BlockRequest(ms, rp);
                    var blockResponse = new BlockResponse
                    {
                        RequestId = blockRequest.RequestId,
                        Blocks = BlockChain.Instance.GetBlocks(blockRequest.StartBlock, blockRequest.EndBlock)
                    };
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        blockResponse.SaveToStream(memoryStream);
                        memoryStream.Position = 0;
                        SendRaw(memoryStream);
                    }

                    break;
                case NetOperationType.Transactions:
                {
                    BlockRequest transactionBlockRequest = new BlockRequest(ms, rp);
                    BlockResponse transactionBlockResponse = new BlockResponse
                    {
                        RequestId = transactionBlockRequest.RequestId,
                        Blocks = BlockChain.Instance.GetBlocks(transactionBlockRequest.StartBlock,
                            transactionBlockRequest.EndBlock).ToList()
                    };
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        transactionBlockResponse.SaveToStream(memoryStream);
                        memoryStream.Position = 0;
                        SendRaw(memoryStream);
                    }
                    break;
                }
                case NetOperationType.CheckPoint:
                    break;
                case NetOperationType.Hello:
                    HelloRequest request = new HelloRequest(ms, rp);
                    Node.Instance.NodeServers.UpdateNodeServers(request.NodeServers);
                    HelloResponse response = new HelloResponse(request)
                    {
                        Timestamp = DateTime.UtcNow,
                        Error = 0,
                        ServerPort = Node.NetParams.Port,
                        Block = BlockChain.Instance.GetLastBlock(),
                        WorkSum = CheckPoints.WorkSum,
                        AccountKey = Node.Instance.NodeKey,
                        RequestType = RequestType.Response,
                        Operation = NetOperationType.Hello
                    };
                    using (MemoryStream vm = new MemoryStream())
                    {
                        response.SaveToStream(vm);
                        vm.Position = 0;
                        SendRaw(vm);
                    }
                    break;
            }
        }

        protected virtual void HandleResponses(MessageHeader rp, MemoryStream ms)
        {
            switch (rp.Operation)
            {
                case NetOperationType.Hello:
                    try
                    {
                        HelloResponse response = new HelloResponse(ms, rp);
                        Node.Instance.NodeServers.UpdateNodeServers(response.NodeServers);
                        OnHelloResponse(response);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.Message, e);
                    }

                    break;
                case NetOperationType.Transactions:
                    BlockResponse transactionBlockResponse = new BlockResponse(ms, rp);
                    OnGetBlockResponse(transactionBlockResponse);
                    break;
                case NetOperationType.Blocks:
                    BlockResponse blockResponse = new BlockResponse(ms, rp);
                    OnGetBlockResponse(blockResponse);
                    break;
                case NetOperationType.CheckPoint:
                    break;
            }
        }
    }

}