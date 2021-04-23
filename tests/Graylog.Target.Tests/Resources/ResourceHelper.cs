#region Usings

using System;
using System.IO;
using System.Reflection;

#endregion

namespace Graylog.Target.Tests.Resources
{
	/// <summary>
	/// Resource helper class.
	/// </summary>
	internal static class ResourceHelper
	{
		/// <summary>
		/// GetResource method.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns>The TextReader.</returns>
		internal static TextReader GetResource(string filename)
		{
			var thisAssembly = Assembly.GetExecutingAssembly();
			var resourceFullName = typeof(ResourceHelper).Namespace + "." + filename;
			var manifestResourceStream = thisAssembly.GetManifestResourceStream(resourceFullName);
			return new StreamReader(manifestResourceStream);
		}
	}
}