using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using System.Windows;
using WpfApp.Model;

namespace WpfApp.ViewModels
{
    internal class ViewModel : BaseViewModel
    {
        private ChatClient _chatClient;

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

        //private readonly SynchronizationContext _synchronizationContext;

        public DelegateCommand CommandJoin { get; private set; } //Команда присоединения к чату
        public DelegateCommand CommandSendMessage { get; private set; } //Команда отправки сообщения
        public DelegateCommand CommandDisconnection { get; private set; } //Отключение от чата

        public ViewModel() 
        {
            try 
            {
                NameValueCollection configuration = ConfigurationManager.AppSettings;
                IPAddress _groupAddress = IPAddress.Parse(configuration["GroupAddress"]);
                int _localPort = int.Parse(configuration["LocalPort"]);
                int  _remotePort = int.Parse(configuration["RemotePort"]);
                int _ttl = int.Parse(configuration["TTL"]);
                _chatClient = new ChatClient(_groupAddress, _localPort, _remotePort, _ttl);
                _chatClient.MessageReceived += DisplayReceiveMessage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось считать файл конфигурации - {ex}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            CommandJoin = new DelegateCommand((obj) => startSession());
            CommandSendMessage = new DelegateCommand((obj) => sendMessage());
            CommandDisconnection = new DelegateCommand((obj) => Disconnect());
        }

        private void startSession() 
        {
            try
            {
                _chatClient.Start(Name);
                UnlockItems();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться к чату! - {ex}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayReceiveMessage(string receivedMessage) 
        {
            string time = DateTime.Now.ToString("t");
            TextBlock = time + " " + receivedMessage + "\r\n" + TextBlock;
        }

        private void sendMessage()
        {
            try
            {
                _chatClient.SendMessage($"{Name}: {Message}");
                Message = string.Empty;
            }
            catch(Exception ex) 
            {
                MessageBox.Show($"Не удалось отправить сообщение - {ex}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Disconnect() 
        {
            _chatClient.Stop();
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