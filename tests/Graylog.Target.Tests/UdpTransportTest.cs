#region Usings

using System;
using System.Collections.Generic;
using System.Net;

using Graylog.Target.Tests.Resources;

using Moq;

using Newtonsoft.Json.Linq;

using NLog;

using NUnit.Framework;

#endregion

namespace Graylog.Target.Tests
{
	/// <summary>
	/// Тесты <see cref="ITransportClient"/>
	/// </summary>
	public class UdpTransportTest
	{
		/// <summary>
		/// Тесты <see cref="ITransportClient"/>
		/// </summary>
		[TestFixture]
		public class SendMethod
		{
			/// <summary>
			/// Отправка короткого сообщения.
			/// </summary>
			[Test]
			public void ShouldSendShortUdpMessage()
			{
				var transportClient = new Mock<ITransportClient>();
				var transport = new UdpTransport(transportClient.Object);
				var converter = new Mock<IConverter>();
				converter.Setup(c => c.GetGelfJson(It.IsAny<LogEventInfo>(), It.IsAny<string>())).Returns(new JObject());

				var target = new NLogTarget(transport, converter.Object) { HostIp = "127.0.0.1" };
				var logEventInfo = new LogEventInfo { Message = "Test Message" };

				target.WriteLogEventInfo(logEventInfo);

				transportClient.Verify(t => t.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once());
				converter.Verify(c => c.GetGelfJson(It.IsAny<LogEventInfo>(), It.IsAny<string>()), Times.Once());
			}

			/// <summary>
			/// Отправка длинного сообщения.
			/// </summary>
			[Test]
			public void ShouldSendLongUdpMessage()
			{
				var jsonObject = new JObject();
				var message = ResourceHelper.GetResource("LongMessage.txt").ReadToEnd();

				jsonObject.Add("full_message", JToken.FromObject(message));

				var converter = new Mock<IConverter>();
				converter.Setup(c => c.GetGelfJson(It.IsAny<LogEventInfo>(), It.IsAny<string>())).Returns(jsonObject).Verifiable();

				var transportClient = new Mock<ITransportClient>();
				transportClient.Setup(t => t.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>())).Verifiable();

				var transport = new UdpTransport(transportClient.Object);

				var target = new NLogTarget(transport, converter.Object) { HostIp = "127.0.0.1" };
				target.WriteLogEventInfo(new LogEventInfo());

				transportClient.Verify(t => t.Send(It.IsAny<IEnumerable<byte[]>>(), It.IsAny<IPEndPoint>()), Times.Once);
				converter.Verify(c => c.GetGelfJson(It.IsAny<LogEventInfo>(), It.IsAny<string>()), Times.Once());
			}
		}
	}
}