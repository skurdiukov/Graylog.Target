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
		public void Send(byte[] datagram, int length, string hostname, int port)
		{
			using (var udpClient = new UdpClient())
			{
				udpClient.Send(datagram, length, hostname, port);
			}
		}

		/// <inheritdoc />
		public void Send(IEnumerable<byte[]> datagrams, string hostname, int port)
		{
			if (datagrams == null)
				throw new ArgumentNullException(nameof(datagrams));

			using (var udpClient = new UdpClient())
			{
				foreach (var datagram in datagrams)
				{
					udpClient.Send(datagram, datagram.Length, hostname, port);
				}
			}
		}
	}
}
