using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WpfApp.Model
{
    internal class ChatClient
    {
        private UdpClient _client;
        private IPAddress _groupAddress;
        private int _localPort;
        private int _remotePort;
        private int _ttl;
        private string _name;
        private UnicodeEncoding _encoding = new UnicodeEncoding();
        private volatile bool _listening = false;

        public event Action<string> MessageReceived;

        public ChatClient(IPAddress groupAddress, int localPort, int remotePort, int ttl)
        {
            _groupAddress = groupAddress;
            _localPort = localPort;
            _remotePort = remotePort;
            _ttl = ttl;
        }

        public void Start(string Name)
        {
            try
            {
                _client = new UdpClient(_localPort);
                _client.JoinMulticastGroup(_groupAddress, _ttl);
                _listening = true;
                _name = Name;

                Thread receiver = new Thread(new ThreadStart(Listener));
                receiver.IsBackground = true;
                receiver.Start();

                SendMessage(_name + " присоединился к чату!");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void SendMessage(string message)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(_groupAddress, _remotePort);
                byte[] data = _encoding.GetBytes(message);
                _client.Send(data, data.Length, remoteEP);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void Listener()
        {
            try
            {
                while (_listening)
                {
                    IPEndPoint endPoint = null;
                    byte[] buffer = _client.Receive(ref endPoint);
                    string receivedMessage = _encoding.GetString(buffer);
                    MessageReceived?.Invoke(receivedMessage);
                }
            }
            catch (Exception ex)
            {
                if (!_listening) return;
                throw new Exception(ex.Message);   
            }
        }

        public void Stop()
        {
            SendMessage(_name + " покинул чат!");
            _listening = false;
            _client?.DropMulticastGroup(_groupAddress);
            _client?.Close();
        }
    }
}