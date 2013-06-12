﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TrinityCore_Manager.TCP
{
    class TCClient : IDisposable
    {

        public event EventHandler TCConnected;
        public event EventHandler TCDisconnected;

        public event MessageReceivedEventHandler TCMessageReceived;

        private TcpClient _client;

        private byte[] _buffer;

        private string _host;
        private int _port;

        public bool IsConnected { get; private set; }

        public TCClient(string host, int port)
        {

            _host = host;
            _port = port;

            _buffer = new byte[1024];

        }

        /// <summary>
        /// Connect To Server
        /// </summary>
        public void Connect()
        {

            if (_client != null)
            {
                throw new Exception("_client NOT null");
            }

            _client = new TcpClient();

            _client.BeginConnect(_host, _port, Connected, _client);
        
        }

        /// <summary>
        /// Disconnect from server
        /// </summary>
        public void Disconnect()
        {

            if (_client == null)
                return;

            if (!_client.Connected || !IsConnected)
                return;

            IsConnected = false;

            if (TCDisconnected != null)
                TCDisconnected(this, EventArgs.Empty);

            _client.Close();
            
            _client = null;
            _buffer = null;


        }

        private void Connected(IAsyncResult iar)
        {

            try
            {

                if (_client == null)
                    return;

                _client.EndConnect(iar);

                if (_client.Connected)
                {

                    IsConnected = true;

                    if (TCConnected != null)
                        TCConnected(this, EventArgs.Empty);

                    var stream = _client.GetStream();

                    stream.BeginRead(_buffer, 0, _buffer.Length, Receive, stream);

                }

            }
            catch (Exception)
            {
                Disconnect();
            }

        }

        private void Receive(IAsyncResult iar)
        {

            try
            {

                var stream = iar.AsyncState as NetworkStream;

                if (stream == null)
                    return;

                int read = stream.EndRead(iar);

                ProcessReceive(read);

            }
            catch (Exception)
            {
                Disconnect();
            }

        }

        private void ProcessReceive(int read)
        {

            try
            {

                string str = Encoding.ASCII.GetString(_buffer, 0, read);

                string[] ex = str.Split('\n');

                var stream = _client.GetStream();

                if (string.IsNullOrEmpty(str) || read == 0)
                {
                    stream.BeginRead(_buffer, 0, _buffer.Length, Receive, stream);
                }
                else if (ex.Length > 0 && str.EndsWith("\n"))
                {

                    for (int i = 0; i < ex.Length; i++)
                    {

                        if (string.IsNullOrEmpty(ex[i]) || ex[i] == "\n")
                            continue;

                        if (TCMessageReceived != null)
                            TCMessageReceived(this, new MessageReceivedEventArgs(ex[i]));

                    }

                    _buffer = new byte[1024];

                    stream.BeginRead(_buffer, 0, _buffer.Length, Receive, stream);

                }
                else //need more data
                {

                    byte[] tmp = Encoding.ASCII.GetBytes(str);

                    _buffer = new byte[read + 1024];
                    Array.Copy(tmp, 0, _buffer, 0, tmp.Length);

                    stream.BeginRead(_buffer, read, _buffer.Length - read, Receive, stream);

                }

            }
            catch (Exception)
            {
                Disconnect();
            }

        }

        /// <summary>
        /// Send a message to the server
        /// </summary>
        /// <param name="message">The message to send</param>
        public void Send(string message)
        {

            try
            {

                if (_client == null)
                    return;

                if (!_client.Connected && IsConnected)
                    return;

                byte[] msg = Encoding.ASCII.GetBytes(message);

                var stream = _client.GetStream();

                stream.BeginWrite(msg, 0, msg.Length, Send, stream);

            }
            catch (Exception)
            {
                Disconnect();
            }

        }

        private void Send(IAsyncResult iar)
        {

            try
            {

                var stream = iar.AsyncState as NetworkStream;

                if (stream == null)
                    return;

                stream.EndWrite(iar);

            }
            catch (Exception)
            {
                Disconnect();
            }

        }

        public void Dispose()
        {

            Dispose(true);

            GC.SuppressFinalize(this);

        }

        public virtual void Dispose(bool disposing)
        {

            if (disposing)
            {
                _client.Close();
            }

            _client = null;
            _buffer = null;

        }

        ~TCClient()
        {
            Dispose(false);
        }

    }

    public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs e);

    public class MessageReceivedEventArgs : EventArgs
    {

        public string Message { get; set; }

        public MessageReceivedEventArgs(string message)
        {
            Message = message;
        }

    }

}
