#region Usings

using System;
using System.IO;
using System.Reflection;

using NUnit.Framework;

#endregion

namespace Graylog.Target.Tests.Resources
{
	/// <summary>
	/// Resourse helper class.
	/// </summary>
	internal class ResourceHelper
	{
		/// <summary>
		/// GetResource method.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns>The TextReader.</returns>
		internal static TextReader GetResource(string filename)
		{
			Assert.IsNotNull(filename);
			var thisAssembly = Assembly.GetExecutingAssembly();
			var resourceFullName = typeof(ResourceHelper).Namespace + "." + filename;
			var manifestResourceStream = thisAssembly.GetManifestResourceStream(resourceFullName);
			Assert.IsNotNull(manifestResourceStream, "Resource not found in this assembly: " + resourceFullName);

			return new StreamReader(manifestResourceStream);
		}
	}
}