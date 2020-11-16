using System;
using System.Net;

using FluentAssertions;
using Newtonsoft.Json.Linq;

using NLog;
using NUnit.Framework;

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
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility", false);

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
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility", false);

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
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility", false);

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
				Message = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus interdum est in est cursus vitae pellentesque felis lobortis. Donec a orci quis ante viverra eleifend ac et quam. Donec imperdiet libero ut justo tincidunt non tristique mauris gravida. Fusce sapien eros, tincidunt a placerat nullam.",
			};

			// act
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility", false);

			// assert
			jsonObject.Should().NotBeNull();
			jsonObject.Value<string>("short_message").Length.Should().Be(250);
			jsonObject.Value<string>("full_message").Length.Should().Be(300);
		}

		/// <summary>
		/// Object conversion should not change original object.
		/// </summary>
		[Test]
		public void ShouldNotChangeLogEvent()
		{
			// arrange
			var logEvent = new LogEventInfo
			{
				Message = "Message",
			};

			// act
			var jsonObject1 = new GelfConverter().GetGelfJson(logEvent, "TestFacility", false);
			var jsonObject2 = new GelfConverter().GetGelfJson(logEvent, "TestFacility", false);

			// assert
			jsonObject1.Should().BeEquivalentTo(jsonObject2);
		}

		/// <summary>
		/// <see cref="MappedDiagnosticsLogicalContext"/> properties should be included into event.
		/// </summary>
		[Test]
		public void ShouldIncludeMdlcProperties()
		{
			// arrange
			var logEvent = new LogEventInfo
			{
				Message = "Message",
			};

			// act
			JObject jsonObject;
			using (MappedDiagnosticsLogicalContext.SetScoped("mdlcItem", "value1"))
			{
				jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility", true);
			}

			// assert
			jsonObject.Should().NotBeNull();
			jsonObject.Value<string>("_mdlcItem").Should().Be("value1");
		}

		/// <summary>
		/// Test for correct serialization of recursive object in properties.
		/// </summary>
		[Test]
		public void ShouldHandleMessageWithSelfReferenceLoopCorrectly()
		{
			// arrange
			var obj = new TestObject { Text = "Hello world!" };
			obj.InnerObject = obj;
			var logEvent = new LogEventInfo
			{
				Message = obj.Text,
				Properties =
				{
					{ "test", obj },
				},
			};

			// act
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility", false);

			// assert
			jsonObject.Should().NotBeNull();
		}

		/// <summary>
		/// test object.
		/// </summary>
		public class TestObject
		{
			/// <summary>
			/// Text property.
			/// </summary>
			public string Text { get; set; }

			/// <summary>
			/// Object property.
			/// </summary>
			public TestObject InnerObject { get; set; }
		}
	}
}
