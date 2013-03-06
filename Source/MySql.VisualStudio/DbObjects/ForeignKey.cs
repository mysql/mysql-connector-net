// Copyright © 2008, 2010, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace MySql.Data.VisualStudio.DbObjects
{
  class ForeignKey : Object, ITablePart
  {
    bool isNew;
    ForeignKey oldFk;
    Table Table;

    private ForeignKey(Table t)
    {
      Table = t;
      SetName(String.Format("FK_{0}_{0}", t.Name), true);
      Columns = new List<FKColumnPair>();
    }

    public ForeignKey(Table t, DataRow keyData)
      : this(t)
    {
      isNew = keyData == null;
      if (!isNew)
      {
        ParseFKInfo(keyData);
        (this as ITablePart).Saved();
      }
    }

    private void ParseFKInfo(DataRow keyData)
    {
      Name = keyData["CONSTRAINT_NAME"].ToString();
      ReferencedTable = keyData["REFERENCED_TABLE_NAME"].ToString();
      if (keyData["MATCH_OPTION"] != DBNull.Value)
        Match = (MatchOption)Enum.Parse(typeof(MatchOption), keyData["MATCH_OPTION"].ToString(), true);
      if (keyData["UPDATE_RULE"] != DBNull.Value)
        UpdateAction = GetActionForSql(keyData["UPDATE_RULE"].ToString());
      if (keyData["DELETE_RULE"] != DBNull.Value)
        DeleteAction = GetActionForSql(keyData["DELETE_RULE"].ToString());

      string[] restrictions = new string[4] { null, Table.OwningNode.Database, Table.Name, Name };
      DataTable cols = Table.OwningNode.GetSchema("Foreign Key Columns", restrictions);
      foreach (DataRow row in cols.Rows)
      {
        FKColumnPair colPair = new FKColumnPair();
        colPair.Column = row["COLUMN_NAME"].ToString();
        colPair.ReferencedColumn = row["REFERENCED_COLUMN_NAME"].ToString();
        Columns.Add(colPair);
      }
    }    

    private string _name;
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    private string _referencedTable;
    public string ReferencedTable
    {
      get { return _referencedTable; }
      set { _referencedTable = value; }
    }

    private MatchOption _match;
    public MatchOption Match
    {
      get { return _match; }
      set { _match = value; }
    }

    private ReferenceOption _updateAction;
    public ReferenceOption UpdateAction
    {
      get { return _updateAction; }
      set { _updateAction = value; }
    }

    private ReferenceOption _deleteAction;
    public ReferenceOption DeleteAction
    {
      get { return _deleteAction; }
      set { _deleteAction = value; }
    }

    private List<FKColumnPair> _columns;
    public List<FKColumnPair> Columns
    {
      get { return _columns; }
      set { _columns = value; }
    }

    public bool IsNew
    {
      get { return this.isNew; }
    }

    public bool NameSet = true;

    public override string ToString()
    {
      return Name;
    }

    public void SetName(string name, bool makeUnique)
    {
      string proposedName = name;
      int uniqueIndex = 0;

      if (makeUnique)
      {
        while (true)
        {
          bool found = false;
          foreach (ForeignKey k in Table.ForeignKeys)
            if (k.Name == proposedName)
            {
              found = true;
              break;
            }
          if (!found) break;
          proposedName = String.Format("{0}_{1}", name, ++uniqueIndex);
        }
      }
      Name = proposedName;
    }

    #region ITablePart Members

    void ITablePart.Saved()
    {
      if (oldFk == null)
        oldFk = new ForeignKey(Table);
      // copy over the top level properties
      oldFk.DeleteAction = DeleteAction;
      oldFk.Match = Match;
      oldFk.Name = Name;
      oldFk.ReferencedTable = ReferencedTable;
      oldFk.Table = Table;
      oldFk.UpdateAction = UpdateAction;

      // now we need to copy the columns
      oldFk.Columns.Clear();
      foreach (FKColumnPair fc in Columns)
      {
        FKColumnPair old = new FKColumnPair();
        old.ReferencedColumn = fc.ReferencedColumn;
        old.Column = fc.Column;
        oldFk.Columns.Add(old);
      }
    }

    bool ITablePart.HasChanges()
    {
      if (!ObjectHelper.AreEqual(this, oldFk)) return true;

      if (Columns.Count != oldFk.Columns.Count) return true;
      foreach (FKColumnPair fc in Columns)
      {
        int i = 0;
        for (; i < oldFk.Columns.Count; i++)
        {
          FKColumnPair ofc = oldFk.Columns[i];
          if (ofc.ReferencedColumn == fc.ReferencedColumn &&
              ofc.Column == fc.Column) break;
        }
        if (i == oldFk.Columns.Count) return true;
      }
      return false;
    }

    string ITablePart.GetDropSql()
    {
      return String.Format("DROP FOREIGN KEY `{0}`", this.oldFk.Name);
    }

    string ITablePart.GetSql(bool newTable)
    {
      // if we don't have any changes then just return null
      if (!(this as ITablePart).HasChanges()) return null;

      StringBuilder sql = new StringBuilder();
      if (!newTable)
      {
        if (oldFk != null)
          sql.AppendFormat("DROP FOREIGN KEY `{0}`, ", oldFk.Name);
        sql.Append("ADD ");
      }
      sql.AppendFormat("FOREIGN KEY `{0}`", Name);

      sql.Append("(");
      string delimiter = "";
      foreach (FKColumnPair c in Columns)
      {
        sql.AppendFormat("{0}{1}", delimiter, c.Column);
        delimiter = ", ";
      }
      sql.Append(")");
      sql.AppendFormat(" REFERENCES `{0}`(", ReferencedTable);
      delimiter = "";
      foreach (FKColumnPair c in Columns)
      {
        sql.AppendFormat("{0}{1}", delimiter, c.ReferencedColumn);
        delimiter = ", ";
      }
      sql.Append(")");
      sql.AppendFormat(" ON DELETE {0}", GetSqlForAction( DeleteAction ));
      sql.AppendFormat(" ON UPDATE {0}", GetSqlForAction(UpdateAction));

      return sql.ToString();
    }

    private ReferenceOption GetActionForSql(string sqlAction)
    {
      ReferenceOption result = ReferenceOption.Restrict;
      switch (sqlAction.ToUpper())
      {
        case "CASCADE":
          result = ReferenceOption.Cascade;
          break;
        case "RESTRICT":
          result = ReferenceOption.Restrict;
          break;
        case "NO ACTION":
          result = ReferenceOption.NoAction;
          break;
        case "SET NULL":
          result = ReferenceOption.SetNull;
          break;
      }
      return result;
    }

    private string GetSqlForAction( ReferenceOption opt )
    {
      string result = "";
      switch (opt)
      {
        case ReferenceOption.Cascade:
          result = "CASCADE";
          break;
        case ReferenceOption.Restrict:
          result = "RESTRICT";
          break;
        case ReferenceOption.NoAction:
          result = "NO ACTION";
          break;
        case ReferenceOption.SetNull:
          result = "SET NULL";
          break;
      }
      return result;
    }

    bool ITablePart.IsNew()
    {
      return isNew;
    }

    #endregion
  }

  enum MatchOption
  {
    Full, Partial, Simple, None
  }

  enum ReferenceOption : int
  {
    NoAction, Cascade, Restrict, SetNull
  }

  class FKColumnPair
  {
    public string ReferencedColumn;
    public string Column;
  }
}
