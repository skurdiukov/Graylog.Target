using System;
using System.Collections.Generic;
using System.Net;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FluentAssertions;
using Moq;
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
			var options = Mock.Of<IConvertOptions>(o => o.Facility == "TestFacility");
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
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, options);

			// assert
			jsonObject.Should().NotBeNull();
			jsonObject.Value<string>("version").Should().Be("1.1");
			jsonObject.Value<string>("host").Should().Be(Dns.GetHostName());
			jsonObject.Value<string>("short_message").Should().Be("Test Log Message");
			jsonObject.Value<string>("full_message").Should().Be("Test Log Message");
			jsonObject.Value<double>("timestamp").Should().Be(timestamp.ToUnixTimestamp());
			jsonObject.Value<int>("level").Should().Be(5);

			jsonObject.Value<string>("_facility").Should().Be(options.Facility);
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
			var options = Mock.Of<IConvertOptions>(o => o.Facility == "TestFacility");
			var logEvent = new LogEventInfo
			{
				Message = "Test Message",
				Exception = new DivideByZeroException("div by 0"),
			};

			// act
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, options);

			// assert
			jsonObject.Should().NotBeNull();
			jsonObject.Value<string>("short_message").Should().Be("Test Message");
			jsonObject.Value<string>("full_message").Should().Be("Test Message");
			jsonObject.Value<int>("level").Should().Be(3);
			jsonObject.Value<string>("_facility").Should().Be(options.Facility);
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
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, Mock.Of<IConvertOptions>());

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
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, Mock.Of<IConvertOptions>());

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
			var jsonObject1 = new GelfConverter().GetGelfJson(logEvent, Mock.Of<IConvertOptions>());
			var jsonObject2 = new GelfConverter().GetGelfJson(logEvent, Mock.Of<IConvertOptions>());

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
				jsonObject = new GelfConverter().GetGelfJson(logEvent, Mock.Of<IConvertOptions>(o => o.IncludeMdlcProperties));
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
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, Mock.Of<IConvertOptions>(o => o.SerializeObjectProperties));

			// assert
			jsonObject.Should().NotBeNull();
		}

		/// <summary>
		/// Test for correct serialization of object in properties, if option <see cref="IConvertOptions.SerializeObjectProperties"/> is enabled.
		/// </summary>
		[Test]
		public void ShouldSerializeObjectsWhenEnabled()
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
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, Mock.Of<IConvertOptions>(o => o.SerializeObjectProperties));

			// assert
			jsonObject["_test"] !["Text"] !.Value<string>().Should().Be(obj.Text);
		}

		/// <summary>
		/// Test for skip serialization of object in properties, if <see cref="IConvertOptions.SerializeObjectProperties"/> option is disabled.
		/// </summary>
		[Test]
		public void ShouldNotSerializeObjectsWhenDisabled()
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
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, Mock.Of<IConvertOptions>(o => !o.SerializeObjectProperties));

			// assert
			jsonObject["_test"].Should().BeNull();
		}

		/// <summary>
		/// Test for skip serialization of object in properties, if <see cref="IConvertOptions.SerializeObjectProperties"/> option is disabled.
		/// </summary>
		/// <param name="serializeObjectProperties"><see cref="IConvertOptions.SerializeObjectProperties"/> value.</param>
		[TestCase(true)]
		[TestCase(false)]
		public void NullPropertiesShouldNotSerialize(bool serializeObjectProperties)
		{
			// arrange
			var logEvent = new LogEventInfo
			{
				Message = "Some text",
				Properties =
				{
					{ "test", null },
				},
			};

			// act
			var jsonObject = new GelfConverter().GetGelfJson(logEvent, Mock.Of<IConvertOptions>(o => o.SerializeObjectProperties == serializeObjectProperties));

			// assert
			jsonObject.Should().NotBeNull();
			jsonObject["_test"].Should().BeNull();
		}

		/// <summary>
		/// Run benchmarks.
		/// </summary>
		[Test]
		[Ignore("Benchmarks")]
		public void RunBenchmarks()
		{
			BenchmarkRunner.Run<Benchmark>();
		}

		/// <summary>
		/// Benchmark fixture.
		/// </summary>
		public class Benchmark
		{
			/// <summary>
			/// Tries count.
			/// </summary>
			private const int TriesCount = 10_000;

			/// <summary>
			/// Create JSON property from string using JValue constructor.
			/// </summary>
			[Benchmark(Description = "Create JSON property from string using new method")]
			public void StringWithNewMethod()
			{
				var obj = new JObject();
				var property = new KeyValuePair<object, object>("key", Guid.NewGuid().ToString());
				for (var i = 0; i < TriesCount; i++)
				{
					GelfConverter.AddAdditionalField(obj, property, true);
				}
			}

			/// <summary>
			/// Create JSON property from using FromObject method.
			/// </summary>
			[Benchmark(Description = "Create JSON property from string using old method")]
			public void StringWithOldMethod()
			{
				var obj = new JObject();
				var property = new KeyValuePair<object, object>("key", Guid.NewGuid().ToString());
				for (var i = 0; i < TriesCount; i++)
				{
					AddAdditionalFieldUsingJTokenFromObject(obj, property);
				}
			}

			/// <summary>
			/// Create JSON property from long using JValue constructor.
			/// </summary>
			[Benchmark(Description = "Create JSON property from long using new method")]
			public void LongWithNewMethod()
			{
				var obj = new JObject();
				var property = new KeyValuePair<object, object>("key", 42L);
				for (var i = 0; i < TriesCount; i++)
				{
					GelfConverter.AddAdditionalField(obj, property, true);
				}
			}

			/// <summary>
			/// Create JSON property from long using FromObject method.
			/// </summary>
			[Benchmark(Description = "Create JSON property from long using old method")]
			public void LongWithOldMethod()
			{
				var obj = new JObject();
				var property = new KeyValuePair<object, object>("key", 42L);
				for (var i = 0; i < TriesCount; i++)
				{
					AddAdditionalFieldUsingJTokenFromObject(obj, property);
				}
			}

			/// <summary>
			/// Create JSON property from object using JValue constructor.
			/// </summary>
			[Benchmark(Description = "Create JSON property from object using new method")]
			public void ObjectWithNewMethod()
			{
				var obj = new JObject();
				var value = new TestObject
				{
					Text = Guid.NewGuid().ToString(),
				};

				var property = new KeyValuePair<object, object>("key", value);
				for (var i = 0; i < TriesCount; i++)
				{
					GelfConverter.AddAdditionalField(obj, property, true);
				}
			}

			/// <summary>
			/// Create JSON property from object using JValue constructor.
			/// </summary>
			[Benchmark(Description = "Create JSON property from object using old method")]
			public void ObjectWithOldMethod()
			{
				var obj = new JObject();
				var value = new TestObject
				{
					Text = Guid.NewGuid().ToString(),
				};

				var property = new KeyValuePair<object, object>("key", value);
				for (var i = 0; i < TriesCount; i++)
				{
					AddAdditionalFieldUsingJTokenFromObject(obj, property);
				}
			}

			/// <summary>
			/// Add additional field like in 1.4.0 version.
			/// </summary>
			/// <param name="jObject"><see cref="JObject"/> to patch.</param>
			/// <param name="property">Adding property.</param>
			private static void AddAdditionalFieldUsingJTokenFromObject(
				JObject jObject,
				KeyValuePair<object, object> property)
			{
				if (!(property.Key is string key) || property.Value == null) return;

				// According to the GELF spec, additional field keys should start with '_' to avoid collision
				if (!key.StartsWith("_", StringComparison.Ordinal))
					key = "_" + key;

				jObject[key] = JToken.FromObject(property.Value, GelfConverter.JsonSerializer);
			}
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
