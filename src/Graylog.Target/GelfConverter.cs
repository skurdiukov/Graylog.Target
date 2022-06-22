using System;
using System.Collections.Generic;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Graylog.Target
{
	/// <summary>
	/// Класс преобразовывающий сообщения в формат GELF.
	/// </summary>
	public class GelfConverter : IConverter
	{
		/// <summary>
		/// Default serializer for object properties.
		/// </summary>
		internal static readonly JsonSerializer JsonSerializer = new JsonSerializer
		{
			NullValueHandling = NullValueHandling.Ignore,
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
		};

		/// <summary>
		/// Максимальная длина короткого сообщения.
		/// </summary>
		private const int ShortMessageMaxLength = 250;

		/// <summary>
		/// Версия формата сообщения.
		/// </summary>
		private const string GelfVersion = "1.1";

		/// <summary>
		/// Таблица соответствия уровней логирования в GrayLog и <see cref="NLog.LogLevel"/>.
		/// </summary>
		private static readonly IDictionary<LogLevel, int> LevelMap = new Dictionary<LogLevel, int>
		{
			{ LogLevel.Fatal, 2 },
			{ LogLevel.Error, 3 },
			{ LogLevel.Warn, 4 },
			{ LogLevel.Info, 5 },
			{ LogLevel.Debug, 6 },
			{ LogLevel.Trace, 7 },
		};

		/// <inheritdoc />
		public JObject GetGelfJson(LogEventInfo logEventInfo, IConvertOptions options)
		{
			if (logEventInfo == null)
				throw new ArgumentNullException(nameof(logEventInfo));

			if (options == null)
				throw new ArgumentNullException(nameof(options));

			// Retrieve the formatted message from LogEventInfo
			var logEventMessage = logEventInfo.FormattedMessage;
			var properties = new Dictionary<object, object>(logEventInfo.Properties);

			if (options.IncludeMdlcProperties)
			{
				foreach (var property in ScopeContext.GetAllProperties())
				{
					properties[property.Key] = property.Value;
				}
			}

			if (logEventMessage == null)
			{
				return null;
			}

			// If we are dealing with an exception, pass exception properties to LogEventInfo properties
			var exception = logEventInfo.Exception;
			if (exception != null)
			{
				var exceptionLevel = 0;

				do
				{
					var prefix = $"Exception.{exceptionLevel++}.";
					properties[prefix + "Type"] = exception.GetType().FullName;
					properties[prefix + "Source"] = exception.Source;
					properties[prefix + "Message"] = exception.Message;
					properties[prefix + "StackTrace"] = exception.StackTrace;

					exception = exception.InnerException;
				}
				while (exception != null);
			}

			// Figure out the short message
			var shortMessage = logEventMessage;
			if (shortMessage.Length > ShortMessageMaxLength)
			{
				shortMessage = shortMessage.Substring(0, ShortMessageMaxLength);
			}

			// Construct the instance of GelfMessage
			// See http://docs.graylog.org/en/2.4/pages/gelf.html "Specification (version 1.1)"
			var gelfMessage = new GelfMessage
			{
				Version = GelfVersion,
				Host = Dns.GetHostName(),
				ShortMessage = shortMessage,
				FullMessage = logEventMessage,
				Timestamp = logEventInfo.TimeStamp.ToUnixTimestamp(),
				Level = GetSeverityLevel(logEventInfo.Level),
			};

			// Convert to JSON
			var jsonObject = JObject.FromObject(gelfMessage);

			// Add any other interesting data to LogEventInfo properties
			properties["LoggerName"] = logEventInfo.LoggerName;
			properties["facility"] = options.Facility;

			// We will persist them "Additional Fields" according to Gelf spec
			foreach (var property in properties)
			{
				AddAdditionalField(jsonObject, property, options.SerializeObjectProperties);
			}

			return jsonObject;
		}

		/// <summary>
		/// Добавляет дополнительные поля.
		/// </summary>
		/// <param name="jObject">Объект к которому будут добавлены поля.</param>
		/// <param name="property">Добавляемое свойство.</param>
		/// <param name="serializeObjectProperties">If <c>true</c> - try include any specified object properties into message.</param>
		internal static void AddAdditionalField(
			JObject jObject,
			KeyValuePair<object, object> property,
			bool serializeObjectProperties)
		{
			if (!(property.Key is string key) || property.Value == null) return;

			// According to the GELF spec, additional field keys should start with '_' to avoid collision
			if (!key.StartsWith("_", StringComparison.Ordinal))
				key = "_" + key;

			JToken value = GetJValue(property.Value);

			if (value == null && serializeObjectProperties)
			{
				value = JToken.FromObject(property.Value, JsonSerializer);
			}

			if (value != null)
			{
				jObject[key] = value;
			}
		}

		/// <summary>
		/// Get <see cref="JValue"/> if value has known type, else <c>null</c>.
		/// </summary>
		/// <param name="value">Source value.</param>
		/// <returns>Resolved <see cref="JValue"/> object.</returns>
		private static JValue GetJValue(object value)
		{
			return value switch
			{
				null => JValue.CreateNull(),
				string s => JValue.CreateString(s),
				long l => new JValue(l),
				int i => new JValue(i),
				short s => new JValue(s),
				sbyte sb => new JValue(sb),
				ulong ul => new JValue(ul),
				uint ui => new JValue(ui),
				ushort us => new JValue(us),
				byte b => new JValue(b),
				Enum e => new JValue(e),
				double d => new JValue(d),
				float f => new JValue(f),
				decimal dec => new JValue(dec),
				DateTime dt => new JValue(dt),
				DateTimeOffset dto => new JValue(dto),
				byte[] bs => new JValue(bs),
				bool b => new JValue(b),
				Guid g => new JValue(g),
				Uri u => new JValue(u),
				TimeSpan t => new JValue(t),
				_ => null,
			};
		}

		/// <summary>
		/// Values from SyslogSeverity enum here: http://marc.info/?l=log4net-dev&amp;m=109519564630799.
		/// </summary>
		/// <param name="level">Log level.</param>
		/// <returns>Mapper log level.</returns>
		private static int GetSeverityLevel(LogLevel level)
		{
			return level == null || !LevelMap.ContainsKey(level) ? 3 : LevelMap[level];
		}
	}
}