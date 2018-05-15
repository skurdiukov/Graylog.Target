using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Graylog.Target
{
	/// <inheritdoc />
	public class UdpTransportClient : ITransportClient
	{
		/// <inheritdoc />
		public void Send(byte[] datagram, int length, IPEndPoint ipEndPoint)
		{
			using (var udpClient = new UdpClient())
			{
				udpClient.Send(datagram, length, ipEndPoint);
			}
		}

		/// <inheritdoc />
		public void Send(IEnumerable<byte[]> datagrams, IPEndPoint ipEndPoint)
		{
			using (var udpClient = new UdpClient())
			{
				foreach (var datagram in datagrams)
				{
					udpClient.Send(datagram, datagram.Length, ipEndPoint);
				}
			}
		}
	}
}
