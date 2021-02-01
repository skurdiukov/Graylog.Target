namespace Graylog.Target
{
	/// <summary>
	/// Convert options.
	/// </summary>
	public interface IConvertOptions
	{
		/// <summary>
		/// Facility name.
		/// </summary>
		string Facility { get; }

		/// <summary>
		/// If <c>true</c> include <see cref="NLog.MappedDiagnosticsLogicalContext"/> properties into message.
		/// </summary>
		bool IncludeMdlcProperties { get; }

		/// <summary>
		/// If <c>true</c> - try include any specified object properties into message.
		/// </summary>
		bool SerializeObjectProperties { get; }
	}
}