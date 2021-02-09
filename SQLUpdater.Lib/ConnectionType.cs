using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// Types of data sources to connect to
	/// </summary>
	public enum ConnectionType
	{
		/// <summary>
		/// A database
		/// </summary>
		Database,

		/// <summary>
		/// A directory containing database scripts
		/// </summary>
		Directory,

		/// <summary>
		/// A single script
		/// </summary>
		File
	}
}
