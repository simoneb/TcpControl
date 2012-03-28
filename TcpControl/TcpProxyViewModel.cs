using System;
using System.Windows.Threading;

namespace TcpControl
{
    public class TcpProxyViewModel : NotifyPropertyChangedBase
    {
        private readonly TcpProxy tcpProxy;
        private readonly Dispatcher dispatcher;
        private TcpProxyStatus status;

        public TcpProxyViewModel(TcpProxy tcpProxy)
        {
            dispatcher = Dispatcher.CurrentDispatcher;

            this.tcpProxy = tcpProxy;

            Status = TcpProxyStatus.Running;

            RemoteEndpoint = tcpProxy.RemoteEndpoint.ToString();
            tcpProxy.InboundReadChanged += i => InboundRead = i;
            tcpProxy.OutboundReadChanged += i => OutboundRead = i;
            tcpProxy.OutboundWrittenChanged += i => OutboundWritten = i;
            tcpProxy.InboundWrittenChanged += i => InboundWritten = i;
            tcpProxy.Closed += TcpProxyOnClosed;
        }

        private void TcpProxyOnClosed(TcpProxy _)
        {
            dispatcher.Invoke(new Action(() => { Status = TcpProxyStatus.Stopped; }));
        }

        public TcpProxyStatus Status
        {
            get { return status; }
            set
            {
                status = value;
                NotifyPropertyChanged(() => Status);
            }
        }

        public string RemoteEndpoint
        {
            get { return remoteEndpoint; }
            set
            {
                remoteEndpoint = value;
                NotifyPropertyChanged(() => RemoteEndpoint);
            }
        }

        private int inboundRead;
        public int InboundRead
        {
            get { return inboundRead; }
            set
            {
                inboundRead = value;
                NotifyPropertyChanged(() => InboundRead);
            }
        }

        private int outboundWritten;
        public int OutboundWritten
        {
            get { return outboundWritten; }
            set
            {
                outboundWritten = value;
                NotifyPropertyChanged(() => OutboundWritten);
            }
        }

        private int outboundRead;
        private string remoteEndpoint;

        public int OutboundRead
        {
            get { return outboundRead; }
            set
            {
                outboundRead = value;
                NotifyPropertyChanged(() => OutboundRead);
            }
        }

        private int inboundWritten;
        
        public int InboundWritten
        {
            get { return inboundWritten; }
            set
            {
                inboundWritten = value;
                NotifyPropertyChanged(() => InboundWritten);
            }
        }

        internal TcpProxy UnderlyingProxy
        {
            get { return tcpProxy; }
        }
    }

    public enum TcpProxyStatus
    {
        Running,
        Stopped
    }
}