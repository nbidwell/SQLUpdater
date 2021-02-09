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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// The set of scripted permissions in a database.
	/// </summary>
	public class PermissionSet
	{
		//permissions[grantingObject][account][permissionType]
		private Dictionary<Name, Dictionary<SmallName, Dictionary<SmallName, SmallName>>> permissions=new Dictionary<Name, Dictionary<SmallName, Dictionary<SmallName, SmallName>>>();

		/// <summary>
		/// Copies the permissions to another permission set.
		/// </summary>
		/// <param name="sink">The permission set to fill.</param>
		public void CopyTo(PermissionSet sink)
		{
			foreach(Name grantingObject in permissions.Keys)
			{
				foreach(SmallName account in permissions[grantingObject].Keys)
				{
					foreach(SmallName permissionType in permissions[grantingObject][account].Keys)
					{
						sink.SetPermission(permissionType, account, grantingObject, permissions[grantingObject][account][permissionType]);
					}
				}
			}
		}

		/// <summary>
		/// Denies the specified permission.
		/// </summary>
		/// <param name="permissionType">Type of the permission.</param>
		/// <param name="account">The account.</param>
		/// <param name="grantingObject">The granting object.</param>
		public void Deny(string permissionType, SmallName account, Name grantingObject)
		{
			SetPermission(permissionType, account, grantingObject, "DENY");
		}

		/// <summary>
		/// Generates the create script.
		/// </summary>
        /// <param name="granted">The object with permissions being granted on</param>
		/// <returns></returns>
		public Script GenerateCreateScript(Item granted)
		{
			if(!permissions.ContainsKey(granted.Name))
			{
				return null;
			}

			StringBuilder script=new StringBuilder();
			Dictionary<SmallName, Dictionary<SmallName, SmallName>> objectPermissions=permissions[granted.Name];
			foreach(SmallName account in objectPermissions.Keys)
			{
				Dictionary<SmallName, SmallName> accountPermissions=objectPermissions[account];
				foreach(SmallName permission in accountPermissions.Keys)
				{
					script.Append(accountPermissions[permission].Unescaped);
					script.Append(" ");
					script.Append(permission.Unescaped);
					script.Append(" ON ");
                    if (granted is Table && ((Table)granted).IsType) script.Append("TYPE::");
					script.Append(granted.Name);
					script.Append(" TO ");
					script.Append(account);
					script.Append("\r\n\r\nGO\r\n\r\n");
				}
			}

			return script.Length>0 ? new Script(script.ToString(), granted.Name, ScriptType.Permission) : null;
		}

        /// <summary>
        /// Gets the differences between two sets.
        /// </summary>
        /// <param name="other">The other set of permissions to compare against.</param>
        /// <param name="differences">The set of differences to fill.</param>
        public void GetDifferences(PermissionSet other, DifferenceSet differences)
        {
            foreach (Name name in permissions.Keys)
            {
                if (IsDifferent(other, differences, name))
                {
                    differences.Add(new Difference(DifferenceType.CreatedPermission, name), null);
                }
            }
        }

        /// <summary>
        /// Grants the specified permission.
        /// </summary>
        /// <param name="permissionType">Type of the permission.</param>
        /// <param name="account">The account.</param>
        /// <param name="grantingObject">The granting object.</param>
        public void Grant(SmallName permissionType, SmallName account, Name grantingObject)
        {
            SetPermission(permissionType, account, grantingObject, "GRANT");
        }

        private bool IsDifferent(PermissionSet other, DifferenceSet differences, Name name)
        {
            if (!other.permissions.ContainsKey(name))
            {
                return true;
            }

            foreach (SmallName account in permissions[name].Keys)
            {
                if (!other.permissions[name].ContainsKey(account))
                {
                    return true;
                }

                foreach (SmallName grantType in permissions[name][account].Keys)
                {
                    SmallName permission = permissions[name][account][grantType];
                    SmallName otherPermission = null;

                    bool granted=other.permissions[name][account].ContainsKey(grantType);
                    if (!granted && grantType == "exec" && other.permissions[name][account].ContainsKey("execute"))
                    {
                        granted = true;
                        otherPermission = other.permissions[name][account]["execute"];
                    }
                    if (!granted && grantType == "execute" && other.permissions[name][account].ContainsKey("exec"))
                    {
                        granted = true;
                        otherPermission = other.permissions[name][account]["exec"];
                    }
                    if (!granted)
                    {
                        return true;
                    }

                    if (otherPermission == null)
                    {
                        otherPermission = other.permissions[name][account][grantType];
                    }

                    if(permission!=otherPermission)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

		/// <summary>
		/// Sets a permission.
		/// </summary>
		/// <param name="permissionType">Type of the permission.</param>
		/// <param name="account">The account.</param>
		/// <param name="grantingObject">The granting object.</param>
		/// <param name="grantType">Type of the grant.</param>
		private void SetPermission(SmallName permissionType, SmallName account, Name grantingObject, SmallName grantType)
		{
			if(!permissions.ContainsKey(grantingObject))
			{
				permissions[grantingObject]=new Dictionary<SmallName, Dictionary<SmallName, SmallName>>();
			}
			Dictionary<SmallName, Dictionary<SmallName, SmallName>> granting=permissions[grantingObject];

			if(!granting.ContainsKey(account))
			{
				granting[account]=new Dictionary<SmallName, SmallName>();
			}
			Dictionary<SmallName, SmallName> permission=granting[account];

			permission[permissionType]=grantType;
		}
	}
}
