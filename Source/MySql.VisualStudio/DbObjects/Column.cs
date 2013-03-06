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
using System.ComponentModel;
using MySql.Data.VisualStudio.Editors;
using MySql.Data.VisualStudio.DbObjects;
using System.Windows.Forms.Design;

namespace MySql.Data.VisualStudio.DbObjects
{
  internal class Column : Object, ITablePart
  {
    private string characterSet;
    internal Column OldColumn;
    private bool isNew;

    private Column()
    {
      AllowNull = true;
    }

    public Column(DataRow row)
      : this()
    {
      isNew = row == null;
      if (row != null)
        ParseColumnInfo(row);
      OldColumn = new Column();
      ObjectHelper.Copy(this, OldColumn);
    }

    #region Properties

    [Browsable(false)]
    internal Table OwningTable;

    private string _columnName;
    [Category("General")]
    [Description("The name of this column")]
    public string ColumnName
    {
      get { return _columnName; }
      set { _columnName = value; }
    }

    private string _dataType;
    [Category("General")]
    [DisplayName("Data Type")]
    [TypeConverter(typeof(DataTypeConverter))]
    [RefreshProperties(RefreshProperties.All)]
    public string DataType
    {
      get { return _dataType; }
      set { _dataType = value; }
    }

    private bool _allowNull;
    [TypeConverter(typeof(YesNoTypeConverter))]
    [Category("Options")]
    [DisplayName("Allow Nulls")]
    public bool AllowNull
    {
      get { return _allowNull; }
      set { _allowNull = value; }
    }

    private bool _isUnsigned;
    [TypeConverter(typeof(YesNoTypeConverter))]
    [Category("Options")]
    [DisplayName("Is Unsigned")]
    public bool IsUnsigned
    {
      get { return _isUnsigned; }
      set { _isUnsigned = value; }
    }

    private bool _isZeroFill;
    [TypeConverter(typeof(YesNoTypeConverter))]
    [Category("Options")]
    [DisplayName("Is Zerofill")]
    public bool IsZerofill
    {
      get { return _isZeroFill; }
      set { _isZeroFill = value; }
    }

    private string _defaultValue;
    [Category("General")]
    [DisplayName("Default Value")]
    public string DefaultValue
    {
      get { return _defaultValue; }
      set { _defaultValue = value; }
    }

    private bool _autoIncrement;
    [TypeConverter(typeof(YesNoTypeConverter))]
    [Category("Options")]
    [DisplayName("Autoincrement")]
    public bool AutoIncrement
    {
      get { return _autoIncrement; }
      set { _autoIncrement = value; }
    }

    //[TypeConverter(typeof(YesNoTypeConverter))]
    //[Category("Options")]
    //[DisplayName("Primary Key")]
    //[RefreshProperties(RefreshProperties.All)]
    [Browsable(false)]
    public bool PrimaryKey;

    private int _precision;
    public int Precision
    {
      get { return _precision; }
      set { _precision = value; }
    }

    private int _scale;
    public int Scale
    {
      get { return _scale; }
      set { _scale = value; }
    }

    [Category("Encoding")]
    [DisplayName("Character Set")]
    [TypeConverter(typeof(CharacterSetTypeConverter))]
    public string CharacterSet
    {
      get { return characterSet; }
      set
      {
        if (value != characterSet)
          Collation = String.Empty;
        characterSet = value;
      }
    }

    private string _collation;
    [Category("Encoding")]
    [TypeConverter(typeof(CollationTypeConverter))]
    public string Collation
    {
      get { return _collation; }
      set { _collation = value; }
    }

    private string _comment;
    [Category("Miscellaneous")]
    public string Comment
    {
      get { return _comment; }
      set { _comment = value; }
    }

    #endregion

    private void ParseColumnInfo(DataRow row)
    {
      ColumnName = row["COLUMN_NAME"].ToString();
      AllowNull = row["IS_NULLABLE"] != DBNull.Value && row["IS_NULLABLE"].ToString() == "YES";
      Comment = row["COLUMN_COMMENT"].ToString();
      Collation = row["COLLATION_NAME"].ToString();
      CharacterSet = row["CHARACTER_SET_NAME"].ToString();
      DefaultValue = row["COLUMN_DEFAULT"].ToString();

      string columnType = row["COLUMN_TYPE"].ToString().ToLowerInvariant();
      int index = columnType.IndexOf(' ');
      if (index == -1)
        index = columnType.Length;
      DataType = columnType.Substring(0, index);
      CleanDataType();

      columnType = columnType.Substring(index);
      IsUnsigned = columnType.IndexOf("unsigned") != -1;
      IsZerofill = columnType.IndexOf("zerofill") != -1;

      PrimaryKey = row["COLUMN_KEY"].ToString() == "PRI";
      Precision = DataRowHelpers.GetValueAsInt32(row, "NUMERIC_PRECISION");
      Scale = DataRowHelpers.GetValueAsInt32(row, "NUMERIC_SCALE");

      string extra = row["EXTRA"].ToString().ToLowerInvariant();
      if (extra != null)
        AutoIncrement = extra.IndexOf("auto_increment") != -1;
    }

    private void CleanDataType()
    {
      if (DataType.Contains("char") || DataType.Contains("binary")) return;
      int index = DataType.IndexOf("(");
      if (index == -1) return;
      DataType = DataType.Substring(0, index);
    }

    #region Methods needed so PropertyGrid won't bold our values

    private bool ShouldSerializeColumnName() { return false; }
    private bool ShouldSerializeDataType() { return false; }
    private bool ShouldSerializeAllowNull() { return false; }
    private bool ShouldSerializeIsUnsigned() { return false; }
    private bool ShouldSerializeIsZerofill() { return false; }
    private bool ShouldSerializeDefaultValue() { return false; }
    private bool ShouldSerializeAutoIncrement() { return false; }
    private bool ShouldSerializePrimaryKey() { return false; }
    private bool ShouldSerializePrecision() { return false; }
    private bool ShouldSerializeScale() { return false; }
    private bool ShouldSerializeCharacterSet() { return false; }
    private bool ShouldSerializeCollation() { return false; }
    private bool ShouldSerializeComment() { return false; }

    #endregion

    #region ITablePart Members

    void ITablePart.Saved()
    {
      ObjectHelper.Copy(this, OldColumn);
    }

    bool ITablePart.HasChanges()
    {
      return !ObjectHelper.AreEqual(this, OldColumn);
    }

    bool ITablePart.IsNew()
    {
      return isNew;
    }

    string ITablePart.GetDropSql()
    {
      return String.Format("DROP `{0}`", ColumnName);
    }

    string ITablePart.GetSql(bool newTable)
    {
      if (OldColumn != null &&
          OldColumn.ColumnName != null &&
          ObjectHelper.AreEqual(this, OldColumn))
        return null;

      if (String.IsNullOrEmpty(ColumnName)) return null;

      StringBuilder props = new StringBuilder();

      props.AppendFormat(" {0}", DataType);
      if (!String.IsNullOrEmpty(CharacterSet))
        props.AppendFormat(" CHARACTER SET '{0}'", CharacterSet);
      if (!String.IsNullOrEmpty(Collation))
        props.AppendFormat(" COLLATE '{0}'", Collation);         
        
      if (!String.IsNullOrEmpty(Comment))
        props.AppendFormat(" COMMENT '{0}'", Comment);

      if (IsZerofill) props.Append(" ZEROFILL");
      if (IsUnsigned) props.Append(" UNSIGNED");
      if (!AllowNull) props.Append(" NOT NULL");      
      if (AutoIncrement) props.Append(" AUTO_INCREMENT");

      if (!String.IsNullOrEmpty(DefaultValue))
      {
        if (DataType.IndexOf("char") >= 0)
          props.AppendFormat(" DEFAULT '{0}'", DefaultValue);
        else
          props.AppendFormat(" DEFAULT {0}", DefaultValue);
      }

      if (newTable)
        return String.Format("`{0}`{1}", ColumnName, props.ToString());
      if (isNew)
        return String.Format("ADD `{0}`{1}", ColumnName, props.ToString());
      return String.Format("CHANGE `{0}` `{1}` {2}",
          OldColumn.ColumnName, ColumnName, props.ToString());
    }

    #endregion

    internal void ResetProperties()
    {
      Collation = "";
      CharacterSet = "";
      AutoIncrement = false;
      DefaultValue = "";
      IsUnsigned = false;
      IsZerofill = false;
      Precision = 0;
      Scale = 0;
    }
  }
}
