using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;
using Graylog.Target.Tests.Resources;
using Moq;
using Newtonsoft.Json.Linq;
using NLog;
using NUnit.Framework;

namespace Graylog.Target.Tests
{
	/// <summary>
	/// Tests for <see cref="ITransportClient"/>.
	/// </summary>
	public class UdpTransportTest
	{
		/// <summary>
		/// Tests for <see cref="UdpTransport"/>.
		/// </summary>
		[TestFixture]
		public class SendMethod
		{
			/// <summary>
			/// Short message send test.
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

				transportClient.Verify(t => t.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once());
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
				transportClient.Setup(t => t.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>())).Verifiable();

				var transport = new UdpTransport(transportClient.Object);

				var target = new NLogTarget(transport, converter.Object) { HostIp = "127.0.0.1" };
				target.WriteLogEventInfo(new LogEventInfo());

				transportClient.Verify(t => t.Send(It.IsAny<IEnumerable<byte[]>>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
				converter.Verify(c => c.GetGelfJson(It.IsAny<LogEventInfo>(), It.IsAny<string>()), Times.Once());
			}
		}

		/// <summary>
		/// Tests for <see cref="UdpTransport.CreateChunks" />.
		/// </summary>
		[TestFixture]
		public class CreateChunks
		{
			/// <summary>
			/// Test for creating proper amount of chunks.
			/// </summary>
			/// <param name="bufferLength">Source buffer length.</param>
			/// <param name="expectedChunksCount">Expected chunks count.</param>
			[TestCase(UdpTransport.MaxMessageSizeInChunk, 1)]
			[TestCase(UdpTransport.MaxMessageSizeInChunk - 1, 1)]
			[TestCase(UdpTransport.MaxMessageSizeInChunk + 1, 2)]
			[TestCase(UdpTransport.MaxMessageSizeInChunk * 2, 2)]
			public void AmountOfChunksTest(int bufferLength, int expectedChunksCount)
			{
				// arrange
				var buffer = new byte[bufferLength];

				// act
				var chunksCount = UdpTransport.CreateChunks(buffer).Count();

				// assert
				chunksCount.Should().Be(expectedChunksCount);
			}
		}
	}
}