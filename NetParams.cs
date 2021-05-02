using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoin
{
#if PRODUCTION
    public class MainParams
    {
#if MICROCOIN
        public  ushort Port { get; set; } = 4004;
        public  int MinerPort { get; set; } = 4009;
        
#else
        public  ushort Port { get; set; } = 4010;
        public  int MinerPort { get; set; } = 4011;
        public  string GenesisBlockPayload { get; set; } = "GPUMINE---";
        public  uint GenesisBlockTimeStamp { get; set; } = 1526765918;
        public  int GenesisBlockNonce { get; set; } = -1049394533;
#endif
        public  uint MaxBlockInPacket { get; set; } = 10000;
        public  uint MinimumDifficulty = 0x19000000;
        public  uint NetworkPacketMagic { get; set; } = 0x0A043580;
        public  ushort NetworkProtocolVersion { get; set; } = 2;
        public  ushort NetworkProtocolAvailable { get; set; } = 3;
#if MICROCOIN
        public const string CoinName  = "MicroCoin";
        public  string CoinTicker { get; set; } = "MCC";
#else
        public const string CoinName  = "PussyCoin";
        public  string CoinTicker { get; set; } = "PYC";
#endif
        public  bool EnableCheckPointing { get; set; } = true;
        public  int CheckPointFrequency { get; set; } = 100; // blocks
        public  int BlockTime { get; set; } = 300;  // seconds
        public  int DifficultyAdjustFrequency { get; set; } = 100; // blocks
        public  int DifficultyCalcFrequency { get; set; } = 10; // blocks
        public  uint MinimumBlocksToUseAccount { get; set; } = 100; // blocks
        public  uint FreeTransactionsPerBlock { get; set; } = 1; // blocks
        public  string GenesisPayload { get; set; } = "(c) Peter Nemeth - Okes rendben okes";
        public  string DataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\"+ CoinName+"V2";
        public  string CheckPointIndexName { get; set; } = System.IO.Path.Combine(DataDirectory, "checkpoints.idx");
        public  string CheckPointFileName { get;set; } = System.IO.Path.Combine(DataDirectory, "checkpoints.mcc");
        public  string BlockChainFileName { get; set; } = System.IO.Path.Combine(DataDirectory, "block.chain");
        public  string KeysFileName { get; set; } = System.IO.Path.Combine(DataDirectory, "WalletKeys.dat");

        public  readonly List<string> FixedSeedServers = new List<string>
        {
            "185.28.101.93"
#if MICROCOIN
            ,
            "185.28.101.93",
            "80.211.200.121",
            "80.211.211.48",
            "94.177.237.196",
            "185.33.146.44",
#endif
        };
    }
}
#endif
    public class NetParams
    {
        public ushort Port { get; set; } = 4104;
        public int MinerPort { get; set; } = 4109;
        public uint MaxBlockInPacket { get; set; } = 10000;
        public uint MinimumDifficulty = 0x19000000;
        public uint NetworkPacketMagic { get; set; } = 0x0A04FFFF;
        public ushort NetworkProtocolVersion { get; set; } = 2;
        public ushort NetworkProtocolAvailable { get; set; } = 3;
        public const string CoinName = "MicroCoinTN";
        public string CoinTicker { get; set; } = "TMCC";
        public bool EnableCheckPointing { get; set; } = true;
        public int CheckPointFrequency { get; set; } = 100; // blocks
        public int BlockTime { get; set; } = 30;  // seconds
        public int DifficultyAdjustFrequency { get; set; } = 100; // blocks
        public int DifficultyCalcFrequency { get; set; } = 10; // blocks
        public uint MinimumBlocksToUseAccount { get; set; } = 10; // blocks
        public uint FreeTransactionsPerBlock { get; set; } = 1; // blocks
        public string GenesisPayload { get; set; } = "(c) Peter Nemeth - Okes rendben okes";
        public string DataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + CoinName + "V2";
        public string CheckPointIndexName
        {
            get
            {
                return System.IO.Path.Combine(DataDirectory, "checkpoints.idx");
            }
        }
        public string CheckPointFileName
        {
            get
            {
                return System.IO.Path.Combine(DataDirectory, "checkpoints.mcc");
            }
        }
        public string BlockChainFileName
        {
            get { return System.IO.Path.Combine(DataDirectory, "block.chain"); }
        }
        public string KeysFileName
        {
            get
            {
                return System.IO.Path.Combine(DataDirectory, "WalletKeys.dat");
            }
        }

        public readonly List<string> FixedSeedServers = new List<string>
        {
            "185.28.101.93",
            "80.211.200.121",
            "80.211.211.48",
            "94.177.237.196",
            "185.33.146.44",
        };
    }
}
