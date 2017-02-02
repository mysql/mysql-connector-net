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
  public class ViewCreateStatement : TargetedBaseStatement<Schema, Result>
  {
    internal string name;
    internal bool replace;
    internal string[] columns;
    internal ViewAlgorithmEnum algorithm = ViewAlgorithmEnum.Undefined;
    internal ViewSqlSecurityEnum sqlSecurity = ViewSqlSecurityEnum.Definer;
    internal string definer = string.Empty;
    internal ViewCheckOptionEnum checkOption = ViewCheckOptionEnum.Local;
    internal QueryStatement queryStatement = null;


    internal ViewCreateStatement(Schema schema, string name, bool replace) : base(schema)
    {
      this.name = name;
      this.replace = replace;
    }

    /// <summary>
    /// Defines the column names of the created View
    /// </summary>
    /// <param name="columns">Alias list for the view columns</param>
    /// <returns>ViewCreate chaining object</returns>
    public ViewCreateStatement Columns(params string[] columns)
    {
      this.columns = columns;
      return this;
    }

    /// <summary>
    /// Defines the View’s algorithm
    /// </summary>
    /// <param name="algorithm">View Algorithm</param>
    /// <returns>ViewCreate chaining object</returns>
    public ViewCreateStatement Algorithm(ViewAlgorithmEnum algorithm)
    {
      this.algorithm = algorithm;
      return this;
    }

    /// <summary>
    /// Defines the View’s security scheme.
    /// </summary>
    /// <param name="sqlSecurity">View security scheme</param>
    /// <returns>ViewCreate chaining object</returns>
    public ViewCreateStatement Security(ViewSqlSecurityEnum sqlSecurity)
    {
      this.sqlSecurity = sqlSecurity;
      return this;
    }

    /// <summary>
    /// Defines the View’s definer.
    /// </summary>
    /// <param name="user">Definer user</param>
    /// <returns>ViewCreate chaining object</returns>
    public ViewCreateStatement Definer(string user)
    {
      this.definer = user;
      return this;
    }

    /// <summary>
    /// Defines the collection find statement to generate the View.
    /// </summary>
    /// <param name="findFunction">Find statement</param>
    /// <returns>ViewCreate chaining object</returns>
    public ViewCreateStatement DefinedAs(FindStatement findFunction)
    {
      this.queryStatement = new QueryStatement(findFunction.Clone());
      return this;
    }

    /// <summary>
    /// Defines the table select statement to generate the View.
    /// </summary>
    /// <param name="selectFunction">Table select statement</param>
    /// <returns>ViewCreate chaining object</returns>
    public ViewCreateStatement DefinedAs(TableSelectStatement selectFunction)
    {
      this.queryStatement = new QueryStatement(selectFunction.Clone());
      return this;
    }

    /// <summary>
    /// Sets insert/update constraints on the View.
    /// </summary>
    /// <param name="checkOption">Check option</param>
    /// <returns>ViewCreate chaining object</returns>
    public ViewCreateStatement WithCheckOption(ViewCheckOptionEnum checkOption)
    {
      this.checkOption = checkOption;
      return this;
    }

    /// <summary>
    /// Executes the view create statement
    /// </summary>
    /// <returns>Result of execution</returns>
    public override Result Execute()
    {
      if(string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("View name");
      if (queryStatement == null) throw new ArgumentNullException("Query");
      if (definer == null) throw new ArgumentNullException("Definer");

      return Session.XSession.ViewCreate(this);
    }
  }
}
