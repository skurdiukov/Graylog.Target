using System;

namespace Graylog.Target
{
	/// <summary>
	/// Интерфейс транспорта.
	/// </summary>
    public interface ITransport
    {
		/// <summary>
		/// Отправляет сообщение на сервер.
		/// </summary>
		/// <param name="serverIpAddress">Адрсе сервера.</param>
		/// <param name="serverPort">Порт сервера.</param>
		/// <param name="message">Текст сообщения.</param>
        void Send(string serverIpAddress, int serverPort, string message);
    }
}
