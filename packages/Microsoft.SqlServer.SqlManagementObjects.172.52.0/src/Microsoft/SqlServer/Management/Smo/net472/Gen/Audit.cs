/*
**** This file has been automatically generated. Do not attempt to modify manually! ****
*/
/*
**** The generated file is compatible with SFC attribute (metadata) requirement ****
*/
using System;
using System.Collections;
using System.Net;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
	/// <summary>
	/// Instance class encapsulating : Server[@Name='']/Audit
	/// </summary>
	/// <inheritdoc/>
	[SfcElement( SfcElementFlags.Standalone )]
	public sealed partial class Audit  : ISfcSupportsDesignMode
	{
		public Audit() : base(){ }
		public Audit(Server server, string name) : base()
		{
			ValidateName(name);
			this.key = new SimpleObjectKey(name);
			this.Parent = server;
		}
		[SfcObject(SfcObjectRelationship.ParentObject)]
		public Server Parent
		{
			get
			{
				CheckObjectState();
				return base.ParentColl.ParentInstance as Server;
			}
			set{SetParentImpl(value);}
		}
		/// <summary>
		/// This object extend ISfcSupportsDesignMode.
		/// </summary>
		bool ISfcSupportsDesignMode.IsDesignMode
		{
			get
			{
				// call the base class 
				return IsDesignMode;
			}
		}
		internal override SqlPropertyMetadataProvider GetPropertyMetadataProvider()
		{
			return new PropertyMetadataProvider(this.ServerVersion,this.DatabaseEngineType, this.DatabaseEngineEdition);
		}
		internal class PropertyMetadataProvider : SqlPropertyMetadataProvider
		{
			internal PropertyMetadataProvider(Common.ServerVersion version,DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition) : base(version,databaseEngineType, databaseEngineEdition)
			{
			}
			public override int PropertyNameToIDLookup(string propertyName)
			{
				if(this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
				{
					if(this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
					{
						return -1;
					}
					else
					{
						return -1;
					}
				}
				else
				{
					switch(propertyName)
					{
						case "CreateDate": return 0;
						case "DateLastModified": return 1;
						case "DestinationType": return 2;
						case "Enabled": return 3;
						case "FileName": return 4;
						case "FilePath": return 5;
						case "Guid": return 6;
						case "ID": return 7;
						case "MaximumFileSize": return 8;
						case "MaximumFileSizeUnit": return 9;
						case "MaximumRolloverFiles": return 10;
						case "OnFailure": return 11;
						case "PolicyHealthState": return 12;
						case "QueueDelay": return 13;
						case "ReserveDiskSpace": return 14;
						case "Filter": return 15;
						case "MaximumFiles": return 16;
						case "IsOperator": return 17;
						case "RetentionDays": return 18;
					}
					return -1;
				}
			}
			static int [] versionCount = new int [] { 0, 0, 0, 15, 15, 17, 18, 18, 18, 19, 19, 19 };
			static int [] cloudVersionCount = new int [] { 0, 0, 0 };
			static int sqlDwPropertyCount = 0;
			public override int Count
			{
				get
				{
					if(this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
					{
						if(this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
						{
							return sqlDwPropertyCount;
						}
						else
						{
							int index = (currentVersionIndex < cloudVersionCount.Length) ? currentVersionIndex : cloudVersionCount.Length - 1;
							return cloudVersionCount[index];
						}
					}
					 else 
					{
						int index = (currentVersionIndex < versionCount.Length) ? currentVersionIndex : versionCount.Length - 1;
						return versionCount[index];
					}
				}
			}
			protected override int[] VersionCount
			{
				get
				{
					if(this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
					{
						if(this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
						{
							 return new int[] { sqlDwPropertyCount }; 
						}
						else
						{
							 return cloudVersionCount; 
						}
					}
					 else 
					{
						 return versionCount;  
					}
				}
			}
			new internal static int[] GetVersionArray(DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
			{
				if(databaseEngineType == DatabaseEngineType.SqlAzureDatabase)
				{
					if(databaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
					{
						 return new int[] { sqlDwPropertyCount }; 
					}
					else
					{
						 return cloudVersionCount; 
					}
				}
				 else 
				{
					 return versionCount;  
				}
			}
			public override StaticMetadata GetStaticMetadata(int id)
			{
				if(this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
				{
					if(this.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
					{
						 return sqlDwStaticMetadata[id]; 
					}
					else
					{
						 return cloudStaticMetadata[id]; 
					}
				}
				 else 
				{
					return staticMetadata[id];
				}
			}
			new internal static StaticMetadata[] GetStaticMetadataArray(DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
			{
				if(databaseEngineType == DatabaseEngineType.SqlAzureDatabase)
				{
					if(databaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
					{
						 return sqlDwStaticMetadata; 
					}
					else
					{
						 return cloudStaticMetadata;
					}
				}
				 else 
				{
					return staticMetadata;
				}
			}
			internal static StaticMetadata [] sqlDwStaticMetadata = 
			{
			};
			internal static StaticMetadata [] cloudStaticMetadata = 
			{
			};
			internal static StaticMetadata [] staticMetadata = 
			{
				new StaticMetadata("CreateDate", false, true, typeof(System.DateTime)),
				new StaticMetadata("DateLastModified", false, true, typeof(System.DateTime)),
				new StaticMetadata("DestinationType", false, false, typeof(Microsoft.SqlServer.Management.Smo.AuditDestinationType)),
				new StaticMetadata("Enabled", false, true, typeof(System.Boolean)),
				new StaticMetadata("FileName", false, true, typeof(System.String)),
				new StaticMetadata("FilePath", false, false, typeof(System.String)),
				new StaticMetadata("Guid", false, false, typeof(System.Guid)),
				new StaticMetadata("ID", false, true, typeof(System.Int32)),
				new StaticMetadata("MaximumFileSize", false, false, typeof(System.Int32)),
				new StaticMetadata("MaximumFileSizeUnit", false, false, typeof(Microsoft.SqlServer.Management.Smo.AuditFileSizeUnit)),
				new StaticMetadata("MaximumRolloverFiles", false, false, typeof(System.Int64)),
				new StaticMetadata("OnFailure", false, false, typeof(Microsoft.SqlServer.Management.Smo.OnFailureAction)),
				new StaticMetadata("PolicyHealthState", true, false, typeof(Microsoft.SqlServer.Management.Dmf.PolicyHealthState)),
				new StaticMetadata("QueueDelay", false, false, typeof(System.Int32)),
				new StaticMetadata("ReserveDiskSpace", false, false, typeof(System.Boolean)),
				new StaticMetadata("Filter", false, false, typeof(System.String)),
				new StaticMetadata("MaximumFiles", false, false, typeof(System.Int32)),
				new StaticMetadata("IsOperator", false, false, typeof(System.Boolean)),
				new StaticMetadata("RetentionDays", false, false, typeof(System.Int32)),
			};
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.DateTime CreateDate
		{
			get
			{
				return (System.DateTime)this.Properties.GetValueWithNullReplacement("CreateDate");
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.DateTime DateLastModified
		{
			get
			{
				return (System.DateTime)this.Properties.GetValueWithNullReplacement("DateLastModified");
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public Microsoft.SqlServer.Management.Smo.AuditDestinationType DestinationType
		{
			get
			{
				return (Microsoft.SqlServer.Management.Smo.AuditDestinationType)this.Properties.GetValueWithNullReplacement("DestinationType");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("DestinationType", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.Boolean Enabled
		{
			get
			{
				return (System.Boolean)this.Properties.GetValueWithNullReplacement("Enabled");
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.String FileName
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("FileName");
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.String FilePath
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("FilePath");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("FilePath", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.String Filter
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("Filter");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("Filter", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation |SfcPropertyFlags.Standalone)]
		public System.Guid Guid
		{
			get
			{
				return (System.Guid)this.Properties.GetValueWithNullReplacement("Guid");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("Guid", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.Int32 ID
		{
			get
			{
				return (System.Int32)this.Properties.GetValueWithNullReplacement("ID");
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.Boolean IsOperator
		{
			get
			{
				return (System.Boolean)this.Properties.GetValueWithNullReplacement("IsOperator");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("IsOperator", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.Int32 MaximumFiles
		{
			get
			{
				return (System.Int32)this.Properties.GetValueWithNullReplacement("MaximumFiles");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("MaximumFiles", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.Int32 MaximumFileSize
		{
			get
			{
				return (System.Int32)this.Properties.GetValueWithNullReplacement("MaximumFileSize");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("MaximumFileSize", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public Microsoft.SqlServer.Management.Smo.AuditFileSizeUnit MaximumFileSizeUnit
		{
			get
			{
				return (Microsoft.SqlServer.Management.Smo.AuditFileSizeUnit)this.Properties.GetValueWithNullReplacement("MaximumFileSizeUnit");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("MaximumFileSizeUnit", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.Int64 MaximumRolloverFiles
		{
			get
			{
				return (System.Int64)this.Properties.GetValueWithNullReplacement("MaximumRolloverFiles");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("MaximumRolloverFiles", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public Microsoft.SqlServer.Management.Smo.OnFailureAction OnFailure
		{
			get
			{
				return (Microsoft.SqlServer.Management.Smo.OnFailureAction)this.Properties.GetValueWithNullReplacement("OnFailure");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("OnFailure", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.Int32 QueueDelay
		{
			get
			{
				return (System.Int32)this.Properties.GetValueWithNullReplacement("QueueDelay");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("QueueDelay", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.Boolean ReserveDiskSpace
		{
			get
			{
				return (System.Boolean)this.Properties.GetValueWithNullReplacement("ReserveDiskSpace");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("ReserveDiskSpace", value);
			}
		}
		[SfcProperty(SfcPropertyFlags.Standalone)]
		public System.Int32 RetentionDays
		{
			get
			{
				return (System.Int32)this.Properties.GetValueWithNullReplacement("RetentionDays");
			}
			set
			{
				Properties.SetValueWithConsistencyCheck("RetentionDays", value);
			}
		}
		internal override string[] GetNonAlterableProperties()
		{
			return new string[] { "Guid" };
		}
	}
}
