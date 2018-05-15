using System;
using System.Net;
using System.Net.Sockets;

namespace Graylog.Target
{
	/// <inheritdoc />
    public class UdpTransportClient : ITransportClient
    {
		/// <inheritdoc />
        public void Send(byte[] datagram, IPEndPoint ipEndPoint)
        {
            using (var udpClient = new UdpClient())
            {
                udpClient.Send(datagram, datagram.Length, ipEndPoint);
            }
        }
    }
}
