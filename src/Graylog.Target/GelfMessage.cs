using System;

using Newtonsoft.Json;

namespace Graylog.Target
{
	/// <summary>
	/// GELF Payload Specification.
	/// </summary>
	/// <remarks>See http://docs.graylog.org/en/2.4/pages/gelf.html.</remarks>
	[JsonObject(MemberSerialization.OptIn)]
	public class GelfMessage
	{
		/// <summary>
		/// GELF spec version – “1.1”; MUST be set by client library.
		/// </summary>
		[JsonProperty("version")]
		public string Version { get; set; }

		/// <summary>
		/// The name of the host, source or application that sent this message; MUST be set by client library.
		/// </summary>
		[JsonProperty("host")]
		public string Host { get; set; }

		/// <summary>
		/// A short descriptive message; MUST be set by client library.
		/// </summary>
		[JsonProperty("short_message")]
		public string ShortMessage { get; set; }

		/// <summary>
		/// A long message that can i.e. contain a backtrace; optional.
		/// </summary>
		[JsonProperty("full_message")]
		public string FullMessage { get; set; }

		/// <summary>
		/// Seconds since UNIX epoch with optional decimal places for milliseconds;
		/// SHOULD be set by client library. Will be set to the current timestamp (now) by the server if absent.
		/// </summary>
		[JsonProperty("timestamp")]
		public double? Timestamp { get; set; }

		/// <summary>
		/// The level equal to the standard syslog levels; optional, default is 1 (ALERT).
		/// </summary>
		[JsonProperty("level")]
		public int Level { get; set; }
	}
}
