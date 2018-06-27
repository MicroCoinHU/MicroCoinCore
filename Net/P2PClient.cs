//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// P2PClient.cs - Copyright (c) 2018 Németh Péter
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
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace MicroCoin.Net
{
    public abstract class P2PClient : IDisposable
    {
        protected static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected Thread ListenerThread;
        protected bool Stop;
        internal virtual event EventHandler Disconnected;
        internal static ushort ServerPort { get; set; }
        internal TcpClient TcpClient { get; set; }
        internal bool Connected { get; set; }
        internal bool IsDisposed { get; set; }

        protected virtual void OnDisconnected()
        {
            Disconnected?.Invoke(this, new EventArgs());
        }

        protected int WaitForData(int timeoutMs)
        {
            while (TcpClient.Available == 0);
            return TcpClient.Available;
        }

        internal bool Connect(string hostname, int port)
        {
            try
            {
                TcpClient = new TcpClient();                
                var result = TcpClient.BeginConnect(hostname, port, null, null);
                Connected = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(50));
                TcpClient.EndConnect(result);                
                if (!Connected)
                {
                    throw new Exception("");
                }
                Log.Info($"Connected to {hostname}:{port}");
            }
            catch (Exception e)
            {
                if (TcpClient != null)
                {
                    TcpClient.Dispose();
                    TcpClient = null;
                }
                Connected = false;
                Log.Debug($"Can't connect to {hostname}:{port}. {e.Message}");
                TcpClient = null;
                return false;
            }
            return Connected;
        }

        internal void Handle(TcpClient client)
        {
            Log.Info($"Connected client {client.Client.RemoteEndPoint}");
            TcpClient = client;
            Connected = true;
            Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

        }

        protected virtual void Dispose(bool disposing)
        {
            // Disconnected?.Invoke(this, new EventArgs());
            if (disposing && !IsDisposed)
            {
                Stop = true;
                while(ListenerThread!=null && ListenerThread.IsAlive)
                {                    
                    Thread.Sleep(1);
                }

                TcpClient?.Dispose();
            }
            IsDisposed = true;
        }

        protected bool ReadData(int requiredSize, MemoryStream ms)
        {
            do
            {
            } while (TcpClient.Available == 0);
            var ns = TcpClient.GetStream();
            int read = 0;
            byte[] buffer = new byte[requiredSize - ms.Length];
            while (read < buffer.Length)
            {
                read += ns.Read(buffer, read, buffer.Length - read);
            }
            ms.Write(buffer, 0, buffer.Length);
            return true;
        }

        protected void ReadAvailable(MemoryStream ms)
        {
            try
            {
                NetworkStream ns = TcpClient.GetStream();
                while (TcpClient.Available > 0)
                {
                    byte[] buffer = new byte[TcpClient.Available];                    
                    ns.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, buffer.Length);
                }
            }
            catch
            {
                OnDisconnected();
            }
        }

        protected bool WaitForPacket()
        {
            while (TcpClient.Available == 0)
            {
                if (Stop)
                {
                    return true;
                }

                Thread.Sleep(1);
            }

            return false;
        }

        internal bool SendRaw(Stream stream)
        {
            if (!TcpClient.Connected)
            {
                OnDisconnected();
                return false;
            }
            try
            {
                NetworkStream ns = TcpClient.GetStream();
                stream.Position = 0;
                stream.CopyTo(ns);
                ns.Flush();
            }
            catch
            {
                OnDisconnected();
                return false;
            }

            return true;
        }

        protected abstract bool HandleConnection();

        internal virtual void Start()
        {
            ListenerThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        if (WaitForPacket()) return;
                        if (HandleConnection()) return;
                    }
                }
                finally
                {
                    TcpClient.Close();
                    TcpClient.Dispose();
                }
            }) {Name = TcpClient.Client.RemoteEndPoint.ToString()};
            ListenerThread.Start();
        }
    }
}
