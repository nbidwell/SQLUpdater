/*
 * Copyright 2009 Nathan Bidwell (nbidwell@bidwellfamily.net)
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
using System.Text;

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// Scripted ordering for a trigger
	/// </summary>
	public class TriggerOrder : Item
	{
		/// <summary>
		/// The defined order.
		/// </summary>
		public SmallName Order;

		/// <summary>
		/// Type of the statement.
		/// </summary>
		public SmallName StatementType;

		/// <summary>
		/// The trigger.
		/// </summary>
		public Name Trigger;

		/// <summary>
		/// Initializes a new instance of the <see cref="TriggerOrder"/> class.
		/// </summary>
		/// <param name="table">The table this trigger is defined on.</param>
		/// <param name="statementType">Type of the statement.</param>
		/// <param name="order">The defined order.</param>
		/// <param name="trigger">The trigger.</param>
		public TriggerOrder(Name table, SmallName statementType, SmallName order, Name trigger)
			: base(table.Unescaped+"_"+statementType.Unescaped+"_"+order)
		{
			Order=order;
			StatementType=statementType;
			Trigger=trigger;
		}

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateCreateScript()
		{
			return new Script("EXEC sp_settriggerorder @triggername='"+Trigger+"', @order="+Order.Unescaped
				+", @stmttype="+StatementType.Unescaped, Name, ScriptType.TriggerOrder);
		}

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateDropScript()
		{
			return new Script("EXEC sp_settriggerorder @triggername='"+Trigger
				+"', @order='NONE', @stmttype="+StatementType.Unescaped, Name, ScriptType.DropTriggerOrder);
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
			TriggerOrder otherOrder=other as TriggerOrder;
			if(otherOrder==null)
				return new Difference(DifferenceType.Created, Name);

			Difference difference=new Difference(DifferenceType.Modified, Name);
			if(Order!=otherOrder.Order)
			{
				difference.AddMessage("Order", otherOrder.Order, Order);
				if(!allDifferences)
					return difference;
			}
			if(StatementType!=otherOrder.StatementType)
			{
				difference.AddMessage("Statement Type", otherOrder.StatementType, StatementType);
				if(!allDifferences)
					return difference;
			}
			if(Trigger!=otherOrder.Trigger)
			{
				difference.AddMessage("Trigger", otherOrder.Trigger, Trigger);
				if(!allDifferences)
					return difference;
			}

			return difference.Messages.Count>0 ? difference : null;
		}
	}
}
