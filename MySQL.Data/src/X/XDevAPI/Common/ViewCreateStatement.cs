// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySqlX.DataAccess;
using MySqlX.XDevAPI.CRUD;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySqlX.XDevAPI.Common
{
  /// <summary>
  /// Represents a chaining view create statement.
  /// </summary>
  public class ViewCreateStatement : TargetedBaseStatement<Schema, Result>
  {
    internal string name;
    internal bool replace;
    internal string[] columns;
    internal ViewAlgorithmEnum algorithm;
    internal ViewSqlSecurityEnum sqlSecurity;
    internal string definer = string.Empty;
    internal ViewCheckOptionEnum checkOption;
    internal QueryStatement queryStatement = null;


    internal ViewCreateStatement(Schema schema, string name, bool replace) : base(schema)
    {
      this.name = name;
      this.replace = replace;
    }

    /// <summary>
    /// Defines the column names (alias) for the view.
    /// </summary>
    /// <param name="columns">The alias list for the view columns.</param>
    /// <returns>A <see cref="ViewCreateStatement"/> chaining object set with the given columns.</returns>
    public ViewCreateStatement Columns(params string[] columns)
    {
      this.columns = columns;
      return this;
    }

    /// <summary>
    /// Defines the algorithm of the view.
    /// </summary>
    /// <param name="algorithm">The algorithm of the view.</param>
    /// <returns>A <see cref="ViewCreateStatement"/> chaining object set with the given algorithm.</returns>
    public ViewCreateStatement Algorithm(ViewAlgorithmEnum algorithm)
    {
      this.algorithm = algorithm;
      return this;
    }

    /// <summary>
    /// Defines the security scheme of the view.
    /// </summary>
    /// <param name="sqlSecurity">The security scheme of the view.</param>
    /// <returns>A <see cref="ViewCreateStatement"/> chaining object set with the given security option.</returns>
    public ViewCreateStatement Security(ViewSqlSecurityEnum sqlSecurity)
    {
      this.sqlSecurity = sqlSecurity;
      return this;
    }

    /// <summary>
    /// Represents the definer for a view.
    /// </summary>
    /// <param name="user">The definer user.</param>
    /// <returns>A <see cref="ViewCreateStatement"/> chaining object set with the given definer user.</returns>
    /// <remarks>
    /// <para>Users should take note on the following behavior:</para>
    /// <para>- schema.CreateView("v").Definer("CURRENT_USER").Execute() ...</para>
    /// <para>is equivalent to executing the statement:</para>
    /// <para>- CREATE definer="CURRENT_USER" VIEW v ...</para>
    /// <para>which, due to the way user names work in the server, is itself equivalent to:</para>
    /// <para>- CREATE definer='CURRENT_USER'@'%' VIEW v ...</para>
    /// </remarks>
    public ViewCreateStatement Definer(string user)
    {
      this.definer = user;
      return this;
    }

    /// <summary>
    /// Defines the table Select statement used to generate the view.
    /// </summary>
    /// <param name="selectFunction">The table select statement.</param>
    /// <returns>A <see cref="ViewCreateStatement"/> chaining object set with the given select statement.</returns>
    public ViewCreateStatement DefinedAs(TableSelectStatement selectFunction)
    {
      this.queryStatement = new QueryStatement(selectFunction.Clone());
      return this;
    }

    /// <summary>
    /// Sets insert and update constraints on the view.
    /// </summary>
    /// <param name="checkOption">The check option.</param>
    /// <returns>A <see cref="ViewCreateStatement"/> chaining object set with the given check option.</returns>
    public ViewCreateStatement WithCheckOption(ViewCheckOptionEnum checkOption)
    {
      this.checkOption = checkOption;
      return this;
    }

    /// <summary>
    /// Executes the view create statement.
    /// </summary>
    /// <returns>A <see cref="Result"/> object set with the results of the execution.</returns>
    public override Result Execute()
    {
      if(string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("Name");
      if (queryStatement == null) throw new ArgumentNullException("Query");
      if (definer == null) throw new ArgumentNullException("Definer");

      return Session.XSession.ViewCreate(this);
    }
  }
}
