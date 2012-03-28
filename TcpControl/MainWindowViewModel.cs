using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using TcpControl.Properties;
using System.Linq;

namespace TcpControl
{
    public class MainWindowViewModel : NotifyPropertyChangedBase
    {
        private readonly TcpServer tcpServer;
        private ICommand startStop;
        private string buttonText;
        private readonly Dispatcher dispatcher;

        public ObservableCollection<TcpProxyViewModel> Proxies { get; private set; }

        public MainWindowViewModel()
        {
            ButtonText = "Start";
            IsStopped = true;
            Proxies = new ObservableCollection<TcpProxyViewModel>();
            dispatcher = Dispatcher.CurrentDispatcher;

            tcpServer = new TcpServer();
            tcpServer.Started += TcpServerOnStarted;
            tcpServer.Stopped += TcpServerOnStopped;
            tcpServer.ProxyAdded += TcpServerOnProxyAdded;
            tcpServer.ProxyRemoved += TcpServerOnProxyRemoved;
        }

        private void TcpServerOnProxyAdded(TcpProxy tcpProxy)
        {
            dispatcher.Invoke(new Action<TcpProxy>(AddProxy), tcpProxy);
        }

        private void AddProxy(TcpProxy proxy)
        {
            Proxies.Add(new TcpProxyViewModel(proxy));
        }

        private void TcpServerOnProxyRemoved(TcpProxy tcpProxy)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(5000);
                dispatcher.Invoke(new Action<TcpProxy>(RemoveProxy), tcpProxy);
            });
        }

        private void RemoveProxy(TcpProxy proxy)
        {
            var toRemove = Proxies.SingleOrDefault(p => p.UnderlyingProxy == proxy);

            if (toRemove != null)
                Proxies.Remove(toRemove);
        }

        private void TcpServerOnStopped()
        {
            dispatcher.Invoke(new Action(() => ButtonText = "Start"));
        }

        private void TcpServerOnStarted()
        {
            dispatcher.Invoke(new Action(() => ButtonText = "Stop"));
        }

        public ICommand StartStop
        {
            get { return startStop ?? (startStop = new RelayCommand(_ => DoStartStop())); }
        }

        private void DoStartStop()
        {
            if (IsStopped)
                tcpServer.Start(InboundPort, OutboundHost, OutboundPort, InboundToOutboundDelay, OutboundToInboundDelay, BufferSize);
            else
                tcpServer.Stop();

            IsStopped = !IsStopped;
        }

        private bool isStopped;
        public bool IsStopped
        {
            get { return isStopped; }
            set
            {
                isStopped = value;
                NotifyPropertyChanged(() => IsStopped);
            }
        }

        public string ButtonText
        {
            get { return buttonText; }
            set
            {
                buttonText = value;
                NotifyPropertyChanged(() => ButtonText);
            }
        }

        public int OutboundPort
        {
            get { return Settings.Default.OutboundPort; }
            set
            {
                Settings.Default.OutboundPort = value;
                NotifyPropertyChanged(() => OutboundPort);
            }
        }

        public string OutboundHost
        {
            get { return Settings.Default.OutboundHost; }
            set
            {
                Settings.Default.OutboundHost = value;
                NotifyPropertyChanged(() => OutboundHost);
            }
        }

        public int InboundPort
        {
            get { return Settings.Default.InboundPort; }
            set
            {
                Settings.Default.InboundPort = value;
                NotifyPropertyChanged(() => InboundPort);
            }
        }

        public int InboundToOutboundDelay
        {
            get { return Settings.Default.InboundToOutboundDelay; }
            set
            {
                Settings.Default.InboundToOutboundDelay = value;
                NotifyPropertyChanged(() => InboundToOutboundDelay);
            }
        }

        public int OutboundToInboundDelay
        {
            get { return Settings.Default.OutboundToInboundDelay; }
            set
            {
                Settings.Default.OutboundToInboundDelay = value;
                NotifyPropertyChanged(() => OutboundToInboundDelay);
            }
        }

        public int BufferSize
        {
            get { return Settings.Default.BufferSize; }
            set
            {
                Settings.Default.BufferSize = value;
                NotifyPropertyChanged(() => BufferSize);
            }
        }
    }
}