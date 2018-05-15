#region Usings

using System;
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
		/// <param name="ipEndPoint">Target endpoint.</param>
		void Send(byte[] datagram, IPEndPoint ipEndPoint);
	}
}