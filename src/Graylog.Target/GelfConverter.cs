#region Usings

using System;
using System.Collections.Generic;
using System.Net;

using Newtonsoft.Json.Linq;

using NLog;

#endregion

namespace Graylog.Target
{
	/// <summary>
	/// Класс преобразовывающий сообщения в формат GELF.
	/// </summary>
	public class GelfConverter : IConverter
	{
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

		/// <inheritdoc/>
		public JObject GetGelfJson(LogEventInfo logEventInfo, string facility)
		{
			// Retrieve the formatted message from LogEventInfo
			var logEventMessage = logEventInfo.FormattedMessage;
			var properties = logEventInfo.Properties;

			if (logEventMessage == null) return null;

			// If we are dealing with an exception, pass exception properties to LogEventInfo properties
			var exception = logEventInfo.Exception;
			if (exception != null)
			{
				var exceptionLevel = 0;

				do
				{
					var prefix = $"Exception.{exceptionLevel++}.";
					properties.Add(prefix + "Type", exception.GetType().FullName);
					properties.Add(prefix + "Source", exception.Source);
					properties.Add(prefix + "Message", exception.Message);
					properties.Add(prefix + "StackTrace", exception.StackTrace);

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
			properties.Add("LoggerName", logEventInfo.LoggerName);
			properties.Add("facility", facility);

			// We will persist them "Additional Fields" according to Gelf spec
			foreach (var property in properties)
			{
				AddAdditionalField(jsonObject, property);
			}

			return jsonObject;
		}

		/// <summary>
		/// Добавляет дополнительные поля.
		/// </summary>
		/// <param name="jObject">Объект к которому будут добавлены поля.</param>
		/// <param name="property">Добавляемое свойство.</param>
		private static void AddAdditionalField(JObject jObject, KeyValuePair<object, object> property)
		{
			if (!(property.Key is string key) || property.Value == null) return;

			// According to the GELF spec, additional field keys should start with '_' to avoid collision
			if (!key.StartsWith("_", StringComparison.Ordinal))
				key = "_" + key;

			jObject.Add(key, JToken.FromObject(property.Value));
		}

		/// <summary>
		/// Values from SyslogSeverity enum here: http://marc.info/?l=log4net-dev&amp;m=109519564630799.
		/// </summary>
		/// <param name="level">Уровень логирования.</param>
		/// <returns>Сконвертированный уровень логирования.</returns>
		private static int GetSeverityLevel(LogLevel level)
		{
			return level == null || !LevelMap.ContainsKey(level) ? 3 : LevelMap[level];
		}
	}
}