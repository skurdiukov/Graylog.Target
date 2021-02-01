using System;

using Newtonsoft.Json;

using NLog;
using NLog.Targets;

namespace Graylog.Target
{
	/// <summary>
	/// NLog target implementation.
	/// </summary>
	[Target("Graylog")]
	public class GraylogTarget : TargetWithLayout, IConvertOptions
	{
		/// <summary>
		/// Create new instance of <see cref="GraylogTarget"/> class.
		/// </summary>
		public GraylogTarget()
		{
			Transport = new UdpTransport(new UdpTransportClient());
			Converter = new GelfConverter();
		}

		/// <summary>
		/// Create new instance of <see cref="GraylogTarget"/> class.
		/// </summary>
		/// <param name="transport">Udp transport.</param>
		/// <param name="converter">Json converter.</param>
		public GraylogTarget(ITransport transport, IConverter converter)
		{
			Transport = transport;
			Converter = converter;
		}

		/// <summary>
		/// Host ip address.
		/// </summary>
		public string HostIp { get; set; }

		/// <summary>
		/// Host port.
		/// </summary>
		public int HostPort { get; set; }

		/// <summary>
		/// Facility name.
		/// </summary>
		public string Facility { get; set; }

		/// <summary>
		/// If <c>true</c> include <see cref="MappedDiagnosticsLogicalContext"/> properties into message.
		/// </summary>
		public bool IncludeMdlcProperties { get; set; }

		/// <summary>
		/// If <c>true</c> include object properties into message.
		/// </summary>
		public bool SerializeObjectProperties { get; set; }

		/// <summary>
		/// Message converter.
		/// </summary>
		public IConverter Converter { get; }

		/// <summary>
		/// Message transport.
		/// </summary>
		public ITransport Transport { get; }

		/// <summary>
		/// Writes log event info into log.
		/// </summary>
		/// <param name="logEvent">Log event to be written out.</param>
		public void WriteLogEventInfo(LogEventInfo logEvent)
		{
			Write(logEvent);
		}

		/// <summary>
		/// Writes log event to the log target. Must be overridden in inheriting classes.
		/// </summary>
		/// <param name="logEvent">Log event to be written out.</param>
		protected override void Write(LogEventInfo logEvent)
		{
			var jsonObject = Converter.GetGelfJson(logEvent, this);
			if (jsonObject == null) return;
			Transport.Send(HostIp, HostPort, jsonObject.ToString(Formatting.None, null!));
		}
	}
}