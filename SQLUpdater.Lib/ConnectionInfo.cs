using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// Encapsulates the information to connecto to a source of information on a database schema
	/// </summary>
	public class ConnectionInfo
	{
		/// <summary>
		/// Gets or sets the connection type.
		/// </summary>
		/// <value>The connection type.</value>
		public ConnectionType Type { get; private set; }

		/// <summary>
		/// Gets or sets the connection path.
		/// </summary>
		/// <value>The connection path.</value>
		public string Path { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionInfo"/> class.
		/// </summary>
		/// <param name="path">The connection path.</param>
		/// <param name="type">The connection type.</param>
		public ConnectionInfo(string path, ConnectionType type)
		{
			Path=path;
			Type=type;
		}
	}
}
