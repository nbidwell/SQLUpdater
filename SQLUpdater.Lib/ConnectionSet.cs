using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// A set of connections
	/// </summary>
	public class ConnectionSet
	{
		/// <summary>
		/// Gets or sets the connections to reference databases.
		/// </summary>
		/// <value>The connections to reference databases.</value>
		public List<ConnectionInfo> References { get; private set; }

		/// <summary>
		/// Gets or sets the connections to target databases.
		/// </summary>
		/// <value>The connections to target databases.</value>
		public List<ConnectionInfo> Targets { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionSet"/> class.
		/// </summary>
		public ConnectionSet()
		{
			References=new List<ConnectionInfo>();
			Targets=new List<ConnectionInfo>();
		}
	}
}
