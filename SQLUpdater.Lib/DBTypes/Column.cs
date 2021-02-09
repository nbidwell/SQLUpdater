/*
 * Copyright 2006 Nathan Bidwell (nbidwell@bidwellfamily.net)
 * 
 * This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License version 20 as published by
 *  the Free Software Foundation.
 * 
 * This software is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Text;

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// A parsed table column
	/// </summary>
	public class Column : Item
	{
		private const string DEFAULT_COLLATION="SQL_Latin1_General_CP1_CI_AS";

		/// <summary>
		/// Gets or sets the derived column definition.
		/// </summary>
		/// <value>The derived column definition.</value>
		public string As { get; set; }

		/// <summary>
		/// Gets or sets the column collation.
		/// </summary>
		/// <value>The column collation.</value>
		public string Collate { get; set; }

        /// <summary>
        /// Gets or sets the column default
        /// </summary>
        /// <value>The default value of the column</value>
        /// <remarks>This is only a cache of the default constraint's value</remarks>
        public string Default { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Column"/> is an identity column.
		/// </summary>
		/// <value><c>true</c> if this is an identity column; otherwise, <c>false</c>.</value>
		public bool Identity { get; set; }

		/// <summary>
		/// Gets or sets the identity increment.
		/// </summary>
		/// <value>The identity increment.</value>
		public string IdentityIncrement { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this is a replicated identity column.
		/// </summary>
		/// <value><c>true</c> if this is a replicated identity column; otherwise, <c>false</c>.</value>
		public bool IdentityReplication { get; set; }

		/// <summary>
		/// Gets or sets the identity seed.
		/// </summary>
		/// <value>The identity seed.</value>
		public string IdentitySeed { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Column"/> is nullable.
		/// </summary>
		/// <value><c>true</c> if nullable; otherwise, <c>false</c>.</value>
		public bool Nullable { get; set; }

		/// <summary>
		/// Gets or sets the size of this column.
		/// </summary>
		/// <value>The size of this column.</value>
		public string Size { get; set; }

		/// <summary>
		/// Gets or sets the type of this column.
		/// </summary>
		/// <value>The type of this column.</value>
		public SmallName Type { get; set; }

		/// <summary>
		/// Gets or sets the name of this item.
		/// </summary>
		/// <value>The name of this item.</value>
		public new SmallName Name
		{
			get{ return base.Name.Object; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Column"/> class.
		/// </summary>
		/// <param name="columnName">Name of the column.</param>
		/// <param name="columnType">Type of the column.</param>
		public Column(string columnName, string columnType) : base(columnName)
		{
			Identity=false;
			IdentityIncrement="1";
			IdentityReplication=true;
			IdentitySeed="1";
			Nullable=true;
			Type=(SmallName)columnType;
		}

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateCreateScript()
		{
			StringBuilder output=new StringBuilder();

			output.Append(Name);
			if(As!=null && As!="")
			{
				output.Append(" AS ");
				output.Append(As);
			}
			else
			{
				output.Append(" "+Type);
				if(Size!=null && Size!="")
				{
					output.Append("("+Size+")");
				}
				if(Collate!=null && Collate!="")
				{
					output.Append(" COLLATE "+Collate);
				}
				if(Identity)
				{
					output.Append(" IDENTITY("+IdentitySeed+", "+IdentityIncrement+")");
					if(!IdentityReplication)
					{
						output.Append(" NOT FOR REPLICATION");
					}
				}
				if(!Nullable)
				{
					output.Append(" NOT");
				}
				output.Append(" NULL");
			}

			return new Script(output.ToString(), Name.ToString(), ScriptType.Unknown);
		}

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateDropScript()
		{
			//This doesn't actually make sense, but is required
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the differences between this item and another of the same type
		/// </summary>
		/// <param name="other">Another item of the same type</param>
		/// <param name="allDifferences">if set to <c>true</c> collects all differences,
		/// otherwise only the first difference is collected.</param>
		/// <returns>
		/// A Difference if there are differences between the items, otherwise null
		/// </returns>
		public override Difference GetDifferences(Item other, bool allDifferences)
		{
			Column otherColumn=other as Column;
			if(otherColumn==null)
				return new Difference(DifferenceType.Created, Name);

			Difference difference=new Difference(DifferenceType.Modified, Name);
			if(Name!=otherColumn.Name)
			{
				difference.AddMessage("Name", otherColumn.Name, Name);
				if(!allDifferences)
					return difference;
			}
			if(As!=otherColumn.As)
			{
				difference.AddMessage("Computed column expression", otherColumn.As, As);
				if(!allDifferences)
					return difference;
			}
			if(Collate!=otherColumn.Collate)
			{
				//deal with default collation
				string collate0=Collate==null ? DEFAULT_COLLATION : Collate;
				string collate1=otherColumn.Collate==null ? DEFAULT_COLLATION : otherColumn.Collate;
				if(collate0!=collate1)
				{
					difference.AddMessage("Collation", collate1, collate0);
					if(!allDifferences)
						return difference;
				}
			}
			if(Identity!=otherColumn.Identity)
			{
				difference.AddMessage("Identity", otherColumn.Identity, Identity);
				if(!allDifferences)
					return difference;
			}
			if(IdentityIncrement!=otherColumn.IdentityIncrement)
			{
				difference.AddMessage("Identity increment", otherColumn.IdentityIncrement, IdentityIncrement);
				if(!allDifferences)
					return difference;
			}
			if(IdentityReplication!=otherColumn.IdentityReplication)
			{
				difference.AddMessage("Identity replication", otherColumn.IdentityReplication, IdentityReplication);
				if(!allDifferences)
					return difference;
			}
			if(IdentitySeed!=otherColumn.IdentitySeed)
			{
				difference.AddMessage("Identity seed", otherColumn.IdentitySeed, IdentitySeed);
				if(!allDifferences)
					return difference;
			}
			if(Nullable!=otherColumn.Nullable)
			{
				difference.AddMessage("Nullability", otherColumn.Nullable, Nullable);
				if(!allDifferences)
					return difference;
			}
			if((Size??"").ToLower()!=(otherColumn.Size??"").ToLower())
			{
				difference.AddMessage("Size", otherColumn.Size, Size);
				if(!allDifferences)
					return difference;
			}
			if(Type!=otherColumn.Type)
			{
				difference.AddMessage("Type", otherColumn.Type, Type);
				if(!allDifferences)
					return difference;
			}

			return difference.Messages.Count>0 ? difference : null;
		}
	}
}
