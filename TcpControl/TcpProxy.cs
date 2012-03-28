using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TcpControl
{
    public class TcpProxy
    {
        private readonly TcpClient inbound;
        private readonly TcpClient outbound;
        private readonly int outboundPort;
        private readonly int inboundToOutboundDelay;
        private readonly int outboundToInboundDelay;
        private readonly string outboundHost;
        private Thread inboundToOutbound;
        private Thread outboundToInbound;
        private int closed;
        private int inboundRead;
        private int outboundWritten;
        private int outboundRead;
        private int inboundWritten;

        public event Action<TcpProxy> Closed = delegate { };
        public event Action<int> InboundReadChanged = delegate { };
        public event Action<int> OutboundReadChanged = delegate { };
        public event Action<int> OutboundWrittenChanged = delegate { };
        public event Action<int> InboundWrittenChanged = delegate { };

        public TcpProxy(TcpClient inbound, string outboundHost, int outboundPort, int inboundToOutboundDelay, int outboundToInboundDelay)
        {
            this.inbound = inbound;
            outbound = new TcpClient();

            this.outboundHost = outboundHost;
            this.outboundPort = outboundPort;
            this.inboundToOutboundDelay = inboundToOutboundDelay;
            this.outboundToInboundDelay = outboundToInboundDelay;
        }

        public IPEndPoint RemoteEndpoint
        {
            get { return (IPEndPoint) inbound.Client.RemoteEndPoint; }
        }

        public void Connect()
        {
            outbound.BeginConnect(outboundHost, outboundPort, OutboundConnectCallback, null);
        }
        
        private void OutboundConnectCallback(IAsyncResult ar)
        {
            // do not retry to connect if already closed
            if (closed == 1)
                return;

            try
            {
                outbound.EndConnect(ar);
            }
            catch (SocketException)
            {
                Debug.WriteLine("Could not connect to remote host, retrying in 1 seconds");
                Thread.Sleep(1000);
                Connect();
                return;
            }

            inboundToOutbound = new Thread(Proxy) { IsBackground = true };
            inboundToOutbound.Start(Tuple.Create<TcpClient, TcpClient, int, Action<int>, Action<int>>(inbound, outbound, inboundToOutboundDelay, IncreaseInboundRead, IncreaseOutboundWritten));

            outboundToInbound = new Thread(Proxy) { IsBackground = true };
            outboundToInbound.Start(Tuple.Create<TcpClient, TcpClient, int, Action<int>, Action<int>>(outbound, inbound, outboundToInboundDelay, IncreaseOutboundRead, IncreaseInboundWritten));
        }

        private void IncreaseInboundRead(int bytes)
        {
            InboundReadChanged(inboundRead += bytes);
        }

        private void IncreaseOutboundWritten(int bytes)
        {
            OutboundWrittenChanged(outboundWritten += bytes);
        }

        private void IncreaseOutboundRead(int bytes)
        {
            OutboundReadChanged(outboundRead += bytes);
        }

        private void IncreaseInboundWritten(int bytes)
        {
            InboundWrittenChanged(inboundWritten += bytes);
        }

        private void Proxy(object state)
        {
            var channels = ((Tuple<TcpClient, TcpClient, int, Action<int>, Action<int>>)state);

            var source = channels.Item1;
            var destination = channels.Item2;
            var delay = channels.Item3;
            var readBytes = channels.Item4;
            var writtenBytes = channels.Item5;

            try
            {
                var sourceStream = source.GetStream();
                var destinationStream = destination.GetStream();

                var buffer = new byte[4096];

                while (true)
                {
                    try
                    {
                        var read = sourceStream.Read(buffer, 0, buffer.Length);

                        if (read == 0)
                        {
                            Close();
                            break;
                        }

                        readBytes(read);

                        if(delay > 0)
                            Thread.Sleep(delay);

                        destinationStream.Write(buffer, 0, read);

                        writtenBytes(read);
                    }
                    catch
                    {
                        Close();
                        break;
                    }
                }
            }
            catch(ObjectDisposedException)
            {
                Close();
            }
            catch (InvalidOperationException)
            {
                Close();
            }
        }

        public void Close()
        {
            if (Interlocked.CompareExchange(ref closed, 1, 0) == 1)
                return;

            try { inbound.Close(); } catch {}
            try { outbound.Close(); } catch {}

            if (inboundToOutbound != null) 
                inboundToOutbound.Join(100);

            if (outboundToInbound != null) 
                outboundToInbound.Join(100);

            Closed(this);
        }
    }
}