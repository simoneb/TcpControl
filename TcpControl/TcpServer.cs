using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace TcpControl
{
    public class TcpServer
    {
        private TcpListener listener;

        private string outboundHost;
        private int outboundPort;
        private readonly SynchronizedCollection<TcpProxy> proxies = new SynchronizedCollection<TcpProxy>();
        private int status = -1;
        private int inboundToOutboundDelay;
        private int outboundToInboundDelay;

        public event Action<TcpProxy> ProxyAdded = delegate { };
        public event Action<TcpProxy> ProxyRemoved = delegate { };
        public event Action Started = delegate { };
        public event Action Stopped = delegate { };

        public void Start(int inboundPort, string outboundHost, int outboundPort, int inboundToOutboundDelay, int outboundToInboundDelay)
        {
            if (status == 1)
                return;

            this.outboundHost = outboundHost;
            this.outboundPort = outboundPort;
            this.inboundToOutboundDelay = inboundToOutboundDelay;
            this.outboundToInboundDelay = outboundToInboundDelay;

            listener = new TcpListener(new IPEndPoint(IPAddress.Any, inboundPort));
            listener.Start();
            listener.BeginAcceptTcpClient(AcceptInboundCallback, null);

            status = 1;
            Started();
        }

        private void AcceptInboundCallback(IAsyncResult ar)
        {
            TcpClient inbound;

            try
            {
                inbound = listener.EndAcceptTcpClient(ar);
            }
            catch (Exception e)
            {
                if(e is NullReferenceException || e is ObjectDisposedException)
                    return;

                Stop();
                throw;
            }

            var proxy = new TcpProxy(inbound, outboundHost, outboundPort, inboundToOutboundDelay, outboundToInboundDelay);
            proxy.Connect();

            proxy.Closed += ProxyClosed;
            AddProxy(proxy);

            listener.BeginAcceptTcpClient(AcceptInboundCallback, listener);
        }

        private void AddProxy(TcpProxy proxy)
        {
            proxies.Add(proxy);
            ProxyAdded(proxy);
        }

        private void ProxyClosed(TcpProxy proxy)
        {
            if (proxies.Remove(proxy))
                ProxyRemoved(proxy);
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref status, 0, 1) == 0)
                return;

            try
            {
                listener.Stop();
            }
            catch (SocketException)
            {
            }

            foreach (var proxy in proxies.ToList())
                proxy.Close();

            proxies.Clear();

            Stopped();
        }
    }
}