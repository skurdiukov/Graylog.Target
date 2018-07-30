#region Usings

using System;
using System.Net;

using FluentAssertions;

using NLog;

using NUnit.Framework;

#endregion

namespace Graylog.Target.Tests
{
	/// <summary>
	/// Тесты для <see cref="GelfConverter"/>.
	/// </summary>
	[TestFixture(Category = "GelfConverter")]
	public class GelfConverterTest
	{
		/// <summary>
		/// Проверка на формирование корректного Json.
		/// </summary>
		[Test]
		public void ShouldCreateGelfJsonCorrectly()
		{
			// arrange
			var timestamp = DateTime.Now;
			var logEvent = new LogEventInfo
			{
				Message = "Test Log Message",
				Level = LogLevel.Info,
				TimeStamp = timestamp,
				LoggerName = "GelfConverterTestLogger",
			};
			logEvent.Properties.Add("customproperty1", "customvalue1");
			logEvent.Properties.Add("customproperty2", "customvalue2");

			// act
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

			// assert
			jsonObject.Should().NotBeNull();
			jsonObject.Value<string>("version").Should().Be("1.1");
			jsonObject.Value<string>("host").Should().Be(Dns.GetHostName());
			jsonObject.Value<string>("short_message").Should().Be("Test Log Message");
			jsonObject.Value<string>("full_message").Should().Be("Test Log Message");
			jsonObject.Value<double>("timestamp").Should().Be(timestamp.ToUnixTimestamp());
			jsonObject.Value<int>("level").Should().Be(5);

			jsonObject.Value<string>("_facility").Should().Be("TestFacility");
			jsonObject.Value<string>("_customproperty1").Should().Be("customvalue1");
			jsonObject.Value<string>("_customproperty2").Should().Be("customvalue2");
			jsonObject.Value<string>("_LoggerName").Should().Be("GelfConverterTestLogger");

			// make sure that there are no other junk in there
			jsonObject.Should().HaveCount(10);
		}

		/// <summary>
		/// Exception logging test.
		/// </summary>
		[Test]
		public void ShouldHandleExceptionsCorrectly()
		{
			// arrange
			var logEvent = new LogEventInfo
			{
				Message = "Test Message",
				Exception = new DivideByZeroException("div by 0"),
			};

			// act
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

			// assert
			jsonObject.Should().NotBeNull();
			jsonObject.Value<string>("short_message").Should().Be("Test Message");
			jsonObject.Value<string>("full_message").Should().Be("Test Message");
			jsonObject.Value<int>("level").Should().Be(3);
			jsonObject.Value<string>("_facility").Should().Be("TestFacility");
			jsonObject.Value<string>("_ExceptionSource").Should().Be(null);
			jsonObject.Value<string>("_Exception.0.Type").Should().Be(typeof(DivideByZeroException).FullName);
			jsonObject.Value<string>("_Exception.0.Message").Should().Be("div by 0");
			jsonObject.Value<string>("_Exception.0.StackTrace").Should().Be(null);
			jsonObject.Value<string>("_LoggerName").Should().Be(null);
		}

		/// <summary>
		/// Nested exception logging test.
		/// </summary>
		[Test]
		public void ShouldHandleNestedExceptionCorrectly()
		{
			// arrange
			var logEvent = new LogEventInfo
			{
				Message = "Test Message",
				Exception = new AggregateException("div by 0", new Exception("Nested exception")),
			};

			// act
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

			// assert
			jsonObject.Should().NotBeNull();
			jsonObject.Value<string>("_Exception.1.Type").Should().Be(typeof(Exception).FullName);
			jsonObject.Value<string>("_Exception.1.Message").Should().Be("Nested exception");
		}

		/// <summary>
		/// Long message log test.
		/// </summary>
		[Test]
		public void ShouldHandleLongMessageCorrectly()
		{
			// arrange
			var logEvent = new LogEventInfo
			{
				// The first 300 chars of lorem ipsum...
				Message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus interdum est in est cursus vitae pellentesque felis lobortis. Donec a orci quis ante viverra eleifend ac et quam. Donec imperdiet libero ut justo tincidunt non tristique mauris gravida. Fusce sapien eros, tincidunt a placerat nullam.",
			};

			// act
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

			// assert
			jsonObject.Should().NotBeNull();
			jsonObject.Value<string>("short_message").Length.Should().Be(250);
			jsonObject.Value<string>("full_message").Length.Should().Be(300);
		}
	}
}
