#region Usings

using System;

using Newtonsoft.Json.Linq;

using NLog;

#endregion

namespace Graylog.Target
{
	/// <summary>
	/// Converter to GELF-style JSON.
	/// </summary>
	public interface IConverter
	{
		/// <summary>
		/// Create JSON object from <see cref="LogEventInfo"/>.
		/// </summary>
		/// <param name="logEventInfo"><see cref="LogEventInfo"/>.</param>
		/// <param name="options">Convert options.</param>
		/// <returns>GELF-style JSON.</returns>
		JObject GetGelfJson(LogEventInfo logEventInfo, IConvertOptions options);
	}
}