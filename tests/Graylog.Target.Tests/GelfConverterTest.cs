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

			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

			jsonObject.Should().NotBeNull();
			Assert.AreEqual("1.1", jsonObject.Value<string>("version"));
			Assert.AreEqual(Dns.GetHostName(), jsonObject.Value<string>("host"));
			Assert.AreEqual("Test Log Message", jsonObject.Value<string>("short_message"));
			Assert.AreEqual("Test Log Message", jsonObject.Value<string>("full_message"));
			Assert.AreEqual(timestamp.ToUnixTimestamp(), jsonObject.Value<double>("timestamp"));
			Assert.AreEqual(5, jsonObject.Value<int>("level"));

			Assert.AreEqual("TestFacility", jsonObject.Value<string>("_facility"));
			Assert.AreEqual("customvalue1", jsonObject.Value<string>("_customproperty1"));
			Assert.AreEqual("customvalue2", jsonObject.Value<string>("_customproperty2"));
			Assert.AreEqual("GelfConverterTestLogger", jsonObject.Value<string>("_LoggerName"));

			// make sure that there are no other junk in there
			jsonObject.Should().HaveCount(10);
		}

		/// <summary>
		/// Exception logging test.
		/// </summary>
		[Test]
		public void ShouldHandleExceptionsCorrectly()
		{
			var logEvent = new LogEventInfo
			{
				Message = "Test Message",
				Exception = new DivideByZeroException("div by 0"),
			};

			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

			Assert.IsNotNull(jsonObject);
			Assert.AreEqual("Test Message", jsonObject.Value<string>("short_message"));
			Assert.AreEqual("Test Message", jsonObject.Value<string>("full_message"));
			Assert.AreEqual(3, jsonObject.Value<int>("level"));
			Assert.AreEqual("TestFacility", jsonObject.Value<string>("_facility"));
			Assert.AreEqual(null, jsonObject.Value<string>("_ExceptionSource"));
			Assert.AreEqual("System.DivideByZeroException", jsonObject.Value<string>("_Exception.0.Type"));
			Assert.AreEqual("div by 0", jsonObject.Value<string>("_Exception.0.Message"));
			Assert.AreEqual(null, jsonObject.Value<string>("_Exception.0.StackTrace"));
			Assert.AreEqual(null, jsonObject.Value<string>("_LoggerName"));
		}

		/// <summary>
		/// Nested exception logging test.
		/// </summary>
		[Test]
		public void ShouldHandleNestedExceptionCorrectly()
		{
			var logEvent = new LogEventInfo
			{
				Message = "Test Message",
				Exception = new AggregateException("div by 0", new Exception("Nested exception")),
			};

			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

			Assert.IsNotNull(jsonObject);
			Assert.AreEqual("System.Exception", jsonObject.Value<string>("_Exception.1.Type"));
			Assert.AreEqual("Nested exception", jsonObject.Value<string>("_Exception.1.Message"));
		}

		/// <summary>
		/// Long message log test.
		/// </summary>
		[Test]
		public void ShouldHandleLongMessageCorrectly()
		{
			var logEvent = new LogEventInfo
			{
				// The first 300 chars of lorem ipsum...
				Message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus interdum est in est cursus vitae pellentesque felis lobortis. Donec a orci quis ante viverra eleifend ac et quam. Donec imperdiet libero ut justo tincidunt non tristique mauris gravida. Fusce sapien eros, tincidunt a placerat nullam.",
			};

			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

			Assert.IsNotNull(jsonObject);
			Assert.AreEqual(250, jsonObject.Value<string>("short_message").Length);
			Assert.AreEqual(300, jsonObject.Value<string>("full_message").Length);
		}
	}
}
