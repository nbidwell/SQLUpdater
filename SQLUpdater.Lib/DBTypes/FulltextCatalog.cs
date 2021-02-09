/*
 * Copyright 2013 Nathan Bidwell (nbidwell@bidwellfamily.net)
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLUpdater.Lib.DBTypes
{
    /// <summary>
    /// Parsed representation of a full text catalog
    /// </summary>
    public class FulltextCatalog : Item
    {
        /// <summary>
        /// Accent Sensitivity
        /// </summary>
        public string AccentSensitivity;

        /// <summary>
        /// Authorization/Owner
        /// </summary>
        public SmallName Authorization;

        /// <summary>
        /// Is this the default catalog?
        /// </summary>
        public bool Default = false;

		/// <summary>
        /// Initializes a new instance of the <see cref="FulltextCatalog"/> class.
		/// </summary>
		/// <param name="name">The index name.</param>
		public FulltextCatalog(string name) : base(name)
		{
		}

        /// <summary>
        /// Generates a create script.
        /// </summary>
        /// <returns></returns>
        public override Script GenerateCreateScript()
        {
            StringBuilder output = new StringBuilder("CREATE FULLTEXT CATALOG ");
            output.AppendLine(Name.Object.ToString());

            if (AccentSensitivity != null)
            {
                output.Append("WITH ACCENT_SENSITIVITY = ");
                output.AppendLine(AccentSensitivity);
            }

            if (Default)
            {
                output.AppendLine("AS DEFAULT");
            }

            if (Authorization != null)
            {
                output.Append("Authorization ");
                output.AppendLine(Authorization);
            }

            return new Script(output.ToString(), Name, ScriptType.FulltextCatalog);
        }

        /// <summary>
        /// Generates a drop script.
        /// </summary>
        /// <returns></returns>
        public override Script GenerateDropScript()
        {
            if (DateTime.Now.Year > 0) throw new Exception("Did you really want to drop this?");

            StringBuilder output = new StringBuilder("DROP FULLTEXT CATALOG ");
            output.Append(Name.Object);

            return new Script(output.ToString(), Name, ScriptType.DropFulltextCatalog);
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
            FulltextCatalog otherCatalog = other as FulltextCatalog;
            if (otherCatalog == null)
                return new Difference(DifferenceType.Created, Name);

            Difference difference = new Difference(DifferenceType.Modified, Name);
            if (Name != otherCatalog.Name)
            {
                difference.AddMessage("Name", otherCatalog.Name, Name);
                if (!allDifferences)
                    return difference;
            }

            if (AccentSensitivity != null && otherCatalog.AccentSensitivity != null
                && AccentSensitivity != otherCatalog.AccentSensitivity)
            {
                difference.AddMessage("Accent Sensitivity", otherCatalog.AccentSensitivity, AccentSensitivity);
                if (!allDifferences)
                    return difference;
            }

            if (Authorization != null && otherCatalog.Authorization != null
                && Authorization != otherCatalog.Authorization)
            {
                difference.AddMessage("Authorization", otherCatalog.Authorization, Authorization);
                if (!allDifferences)
                    return difference;
            }

            if (Default != otherCatalog.Default)
            {
                difference.AddMessage("Default", otherCatalog.Default, Default);
                if (!allDifferences)
                    return difference;
            }

            return difference.Messages.Count > 0 ? difference : null;
        }
    }
}
