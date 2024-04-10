using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WpfApp.Model;

namespace WpfApp.ViewModels
{
    internal class ViewModel : BaseViewModel
    {
        private bool _done = true; //флаг остановки следующего потока
        private UdpClient _client;
        private IPAddress _groupAddress; //групповой адрес рассылки
        private int _localPort; //локальный порт для приема сообщений
        private int _remotePort; //удаленный порт для отправки сообщений
        private int _ttl;

        private string _receivedMessage;

        private IPEndPoint _remoteEP;
        private UnicodeEncoding _encoding = new UnicodeEncoding();

        private string _name; //Имя пользователя
        public string Name 
        {
            get => _name;
            set 
            {
                _name = value;
                OnPropertyChanded(nameof(Name));
            }
        }

        private bool _nameEnabled = true;
        public bool NameEnabled
        {
            get => _nameEnabled;
            private set 
            {
                _nameEnabled = value;
                OnPropertyChanded(nameof(NameEnabled));
            }
        }

        private bool _buttonJoinEnabled = true;
        public bool ButtonJoinEnabled 
        {
            get => _buttonJoinEnabled;
            private set 
            {
                _buttonJoinEnabled = value;
                OnPropertyChanded(nameof(ButtonJoinEnabled));
            }
        }

        private bool _buttonStopEnabled = false;
        public bool ButtonStopEnabled
        {
            get => _buttonStopEnabled;
            private set
            {
                _buttonStopEnabled = value;
                OnPropertyChanded(nameof(ButtonStopEnabled));
            }
        }

        private bool _buttonSendEnabled = false;
        public bool ButtonSendEnabled
        {
            get => _buttonSendEnabled;
            private set
            {
                _buttonSendEnabled = value;
                OnPropertyChanded(nameof(ButtonSendEnabled));
            }
        }

        private string _textBlock;
        public string TextBlock
        {
            get => _textBlock;
            private set 
            {
                _textBlock = value;
                OnPropertyChanded(nameof(TextBlock));
            }
        }

        private string _message; //сообщение для отправки
        public string Message 
        {
            get => _message;
            set 
            {
                _message = value;
                OnPropertyChanded(nameof(Message));
            }
        }

        private readonly SynchronizationContext _synchronizationContext;

        public DelegateCommand CommandJoin { get; private set; } //Команда присоединения к чату
        public DelegateCommand CommandSendMessage { get; private set; } //Команда отправки сообщения
        public DelegateCommand CommandDisconnection { get; private set; } //Отключение от чата

        public ViewModel() 
        {
            try 
            {
                NameValueCollection configuration = ConfigurationManager.AppSettings;
                _groupAddress = IPAddress.Parse(configuration["GroupAddress"]);
                _localPort = int.Parse(configuration["LocalPort"]);
                _remotePort = int.Parse(configuration["RemotePort"]);
                _ttl = int.Parse(configuration["TTL"]);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось считать файл конфигурации - {ex}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            _synchronizationContext = SynchronizationContext.Current;

            CommandJoin = new DelegateCommand((obj) => startSession());
            CommandSendMessage = new DelegateCommand((obj) => sendMessage());
            CommandDisconnection = new DelegateCommand((obj) => Disconnect());
        }

        private void startSession() 
        {
            try
            {
                _client = new UdpClient(_localPort);
                _client.JoinMulticastGroup(_groupAddress, _ttl);
                _remoteEP = new IPEndPoint(_groupAddress, _remotePort);

                Thread receiver = new Thread(new ThreadStart(Listener));
                receiver.IsBackground = true;
                receiver.Start();

                byte[] data = _encoding.GetBytes(Name + " присоединился к чату!");
                _client.Send(data, data.Length, _remoteEP);

                UnlockItems();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться к чату! - {ex}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Listener() 
        {
            _done = false;

            try
            {
                while (!_done)
                {
                    IPEndPoint endPoint = null;
                    byte[] buffer = _client.Receive(ref endPoint);
                    _receivedMessage = _encoding.GetString(buffer);
                    _synchronizationContext.Post(x => DisplayReceiveMessage(), null);
                }
            }
            catch(Exception ex)
            {
                if (_done)
                    return;
                else
                    MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayReceiveMessage() 
        {
            string time = DateTime.Now.ToString("t");
            TextBlock = time + " " + _receivedMessage + "\r\n" + TextBlock;
            _receivedMessage = string.Empty;
        }

        private void sendMessage()
        {
            try
            {
                byte[] data = _encoding.GetBytes(Name + ": " + Message);
                _client.Send(data, data.Length, _remoteEP);
                Message = string.Empty;
            }
            catch(Exception ex) 
            {
                MessageBox.Show($"Не удалось отправить сообщение - {ex}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Disconnect() 
        {
            StopListener();
        }

        private void StopListener() 
        {
            byte[] data = _encoding.GetBytes(Name + " покинул чат");
            _client.Send(data, data.Length, _remoteEP);

            _client.DropMulticastGroup(_groupAddress);
            _client.Close();

            _done = true;

            UnlockItems();
        }

        private void UnlockItems() 
        {
            ButtonJoinEnabled = !ButtonJoinEnabled;
            ButtonStopEnabled = !ButtonStopEnabled;
            ButtonSendEnabled = !ButtonSendEnabled;
            NameEnabled = !NameEnabled;
        }
    }
}