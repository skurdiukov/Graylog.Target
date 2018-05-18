#region Usings

using System;
using System.Collections.Generic;
using System.Net;

#endregion

namespace Graylog.Target
{
	/// <summary>
	/// Клиент для отправки двоичных данных.
	/// </summary>
	public interface ITransportClient
	{
		/// <summary>
		/// Отправляет набор двоичных данных.
		/// </summary>
		/// <param name="datagram">The datagram.</param>
		/// <param name="length">Size of send data.</param>
		/// <param name="hostname">Target endpoint.</param>
		/// <param name="port">Target port.</param>
		void Send(byte[] datagram, int length, string hostname, int port);

		/// <summary>
		/// Send datagrams with UDP protocol.
		/// </summary>
		/// <param name="datagrams">The datagram.</param>
		/// <param name="hostname">Target endpoint.</param>
		/// <param name="port">Target port.</param>
		void Send(IEnumerable<byte[]> datagrams, string hostname, int port);
	}
}