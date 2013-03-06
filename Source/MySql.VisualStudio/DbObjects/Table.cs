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
using MySql.Data.VisualStudio.Properties;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;
using MySql.Data.VisualStudio.Editors;
using System.Windows.Forms;

namespace MySql.Data.VisualStudio.DbObjects
{
  public enum InsertMethod
  {
    No, First, Last
  }

  public enum PackKeysMethod
  {
    Default, None, Full
  }

  internal class Table : ICustomTypeDescriptor
  {
    private TableNode owningNode;
    internal Table OldTable;
    private string characterSet;

    private Table()
    {
    }

    public Table(TableNode node, DataRow row, DataTable columns)
    {
      owningNode = node;
      IsNew = row == null;

      Columns = new TablePartCollection<Column>();
      Indexes = new TablePartCollection<Index>();
      ForeignKeys = new TablePartCollection<ForeignKey>();

      // set some defaults that may be overridden with actual table data
      Engine = node.DefaultStorageEngine;
      PackKeys = PackKeysMethod.Default;
      Schema = node.Database;

      if (row != null)
        ParseTableData(row);
      if (columns != null)
        ParseColumns(columns);
      if (!IsNew)
      {
        LoadIndexes();
        LoadForeignKeys();
      }

      // now save our current values as old
      OldTable = new Table();
      ObjectHelper.Copy(this, OldTable);
      node.DataSaved += new EventHandler(node_DataSaved);
    }

    void node_DataSaved(object sender, EventArgs e)
    {
      ObjectHelper.Copy(this, OldTable);
      Columns.Saved();
      Indexes.Saved();
      ForeignKeys.Saved();
    }

    private bool _isNew;
    [Browsable(false)]
    public bool IsNew
    {
      get { return _isNew; }
      private set { _isNew = value; }
    }

    private TablePartCollection<Column> _columns;
    [Browsable(false)]
    public TablePartCollection<Column> Columns
    {
      get { return _columns; }
      private set { _columns = value; }
    }

    private TablePartCollection<Index> _indexes;
    [Browsable(false)]
    public TablePartCollection<Index> Indexes
    {
      get { return _indexes; }
      private set { _indexes = value; }
    }

    private TablePartCollection<ForeignKey> _foreignKeys;
    [Browsable(false)]
    public TablePartCollection<ForeignKey> ForeignKeys
    {
      get { return _foreignKeys; }
      private set { _foreignKeys = value; }
    }

    internal TableNode OwningNode
    {
      get { return owningNode; }
    }

    internal bool SupportsFK
    {
      get
      {
        string engine = Engine.ToLowerInvariant();
        return engine == "innodb" || engine == "falcon";
      }
    }

    #region Table options

    private string _name;
    [Category("(Identity)")]
    [MyDescription("TableNameDesc")]
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    private string _schema;
    [Category("(Identity)")]
    [MyDescription("TableSchemaDesc")]
    public string Schema
    {
      get { return _schema; }
      private set { _schema = value; }
    }

    private string _comment;
    [MyDescription("TableCommentDesc")]
    public string Comment
    {
      get { return _comment; }
      set { _comment = value; }
    }

    [Category("Table Options")]
    [DisplayName("Character Set")]
    [TypeConverter(typeof(CharacterSetTypeConverter))]
    [RefreshProperties(RefreshProperties.All)]
    [MyDescription("TableCharSetDesc")]
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
    [Category("Table Options")]
    [DisplayName("Collation")]
    [TypeConverter(typeof(CollationTypeConverter))]
    [MyDescription("TableCollationDesc")]
    public string Collation
    {
      get { return _collation; }
      set { _collation = value; }
    }

    private ulong _autoInc;
    [Category("Table")]
    [DisplayName("Auto Increment")]
    [MyDescription("TableAutoIncStartDesc")]
    public ulong AutoInc
    {
      get { return _autoInc; }
      set { _autoInc = value; }
    }

    #endregion

    #region Storage options

    private string _engine;
    [Category("Storage")]
    [DisplayName("Storage Engine")]
    [MyDescription("TableEngineDescription")]
    [TypeConverter(typeof(TableEngineTypeConverter))]
    [RefreshProperties(RefreshProperties.All)]
    public string Engine
    {
      get { return _engine; }
      set { _engine = value; }
    }

    private string _dataDirectory;
    [Category("Storage")]
    [DisplayName("Data Directory")]
    [MyDescription("TableDataDirDesc")]
    public string DataDirectory
    {
      get { return _dataDirectory; }
      set { _dataDirectory = value; }
    }

    private string _indexDirectory;
    [Category("Storage")]
    [DisplayName("Index Directory")]
    [MyDescription("TableIndexDirDesc")]
    public string IndexDirectory
    {
      get { return _indexDirectory; }
      set { _indexDirectory = value; }
    }

    #endregion

    #region Row options

    private RowFormat _rowFormat;
    [Category("Row")]
    [DisplayName("Row Format")]
    [MyDescription("TableRowFormatDesc")]
    public RowFormat RowFormat
    {
      get { return _rowFormat; }
      set { _rowFormat = value; }
    }

    private bool _checkSum;
    [Category("Row")]
    [DisplayName("Compute Checksum")]
    [MyDescription("TableCheckSumDesc")]
    [DefaultValue(false)]
    [TypeConverter(typeof(YesNoTypeConverter))]
    public bool CheckSum
    {
      get { return _checkSum; }
      set { _checkSum = value; }
    }

    private ulong _avgRowLength;
    [Category("Row")]
    [DisplayName("Average Row Length")]
    [MyDescription("TableAvgRowLengthDesc")]
    [TypeConverter(typeof(NumericTypeConverter))]
    public ulong AvgRowLength
    {
      get { return _avgRowLength; }
      set { _avgRowLength = value; }
    }

    private ulong _minRows;
    [Category("Row")]
    [DisplayName("Minimum Rows")]
    [MyDescription("TableMinRowsDesc")]
    [TypeConverter(typeof(NumericTypeConverter))]
    public ulong MinRows
    {
      get { return _minRows; }
      set { _minRows = value; }
    }

    private UInt64 _maxRows;
    [Category("Row")]
    [DisplayName("Maximum Rows")]
    [MyDescription("TableMaxRowsDesc")]
    [TypeConverter(typeof(NumericTypeConverter))]
    public UInt64 MaxRows
    {
      get { return _maxRows; }
      set { _maxRows = value; }
    }

    private PackKeysMethod _packKeys;
    [Category("Row")]
    [DisplayName("Pack Keys")]
    [MyDescription("TablePackKeysDesc")]
    [DefaultValue(PackKeysMethod.Default)]
    public PackKeysMethod PackKeys
    {
      get { return _packKeys; }
      set { _packKeys = value; }
    }

    private InsertMethod _insertMethod;
    [Category("Row")]
    [DisplayName("Insert method")]
    [MyDescription("TableInsertMethodDesc")]
    [DefaultValue(InsertMethod.First)]
    public InsertMethod InsertMethod
    {
      get { return _insertMethod; }
      set { _insertMethod = value; }
    }

    private bool _delayKeyWrite;
    [Category("Row")]
    [DisplayName("Delay Key Write")]
    [MyDescription("DelayKeyWriteDesc")]
    public bool DelayKeyWrite
    {
      get { return _delayKeyWrite; }
      set { _delayKeyWrite = value; }
    }

    #endregion

    #region ShouldSerializeMethods

    bool ShouldSerializeName() { return false; }
    bool ShouldSerializeSchema() { return false; }
    bool ShouldSerializeComment() { return false; }
    bool ShouldSerializeCharacterSet() { return false; }
    bool ShouldSerializeCollation() { return false; }
    bool ShouldSerializeAutoInc() { return false; }
    bool ShouldSerializeEngine() { return false; }
    bool ShouldSerializeDataDirectory() { return false; }
    bool ShouldSerializeIndexDirectory() { return false; }
    bool ShouldSerializeRowFormat() { return false; }
    bool ShouldSerializeCheckSum() { return false; }
    bool ShouldSerializeAvgRowLength() { return false; }
    bool ShouldSerializeMinRows() { return false; }
    bool ShouldSerializeMaxRows() { return false; }
    bool ShouldSerializePackKeys() { return false; }
    bool ShouldSerializeInsertMethod() { return false; }

    #endregion

    public void NotifyUpdate()
    {
      OnDataUpdated();
    }

    public void DeleteKey(string keyName)
    {
      for (int i = Indexes.Count - 1; i >= 0; i--)
      {
        if ((keyName != null && Indexes[i].Name == keyName) ||
            (keyName == null && Indexes[i].IsPrimary))
        {
          Indexes.Delete(i);
          break;
        }
      }
    }

    public Index CreateIndexWithUniqueName(bool primary)
    {
      Index newIndex = new Index(this, null);
      newIndex.IsPrimary = primary;
      string baseName = String.Format("{0}_{1}", primary ? "PK" : "IX",
          Name);
      string name = baseName;
      int uniqueIndex = 0;
      while (KeyExists(name))
        name = String.Format("{0}_{1}", baseName, ++uniqueIndex);
      newIndex.Name = name;
      return newIndex;
    }

    public List<string> GetColumnNames()
    {
      string sql = @"SELECT column_name FROM INFORMATION_SCHEMA.COLUMNS WHERE 
                TABLE_SCHEMA='{0}' AND TABLE_NAME='{1}'";
      DataTable dt = owningNode.GetDataTable(String.Format(sql, owningNode.Database, Name));
      List<string> cols = new List<string>();
      foreach (DataRow row in dt.Rows)
        cols.Add(row[0].ToString());
      return cols;
    }

    public string GetSql()
    {
      StringBuilder sql = new StringBuilder();
      if (IsNew)
        sql.AppendFormat("CREATE TABLE `{0}` (", Name);
      else
        sql.AppendFormat("ALTER TABLE `{0}` ", OldTable.Name);

      string[] parts = new string[3];
      parts[0] = Columns.GetSql(IsNew);
      parts[1] = Indexes.GetSql(IsNew);
      parts[2] = ForeignKeys.GetSql(IsNew);
      string delimiter = "";
      foreach (string s in parts)
      {
        if (!String.IsNullOrEmpty(s))
        {
          sql.AppendFormat("{0}{1}", delimiter, s);
          delimiter = ", ";
        }
      }
      if (IsNew)
        sql.Append(")");
      sql.Append(GetTableOptionSql(IsNew));
      return sql.ToString();
    }

    public bool HasChanges()
    {
      // first compare our top level properties
      if (!ObjectHelper.AreEqual(this, OldTable))
        return true;      
      if (Columns.HasChanges()) return true;
      if (Indexes.HasChanges()) return true;
      if (ForeignKeys.HasChanges()) return true;
      return false;
    }

    #region Private methods

    private bool KeyExists(string keyName)
    {
      foreach (Index i in Indexes)
        if (String.Compare(i.Name, keyName, true) == 0) return true;
      return false;
    }

    private void ParseTableData(DataRow tableRow)
    {
      Schema = tableRow["TABLE_SCHEMA"].ToString();
      Name = tableRow["TABLE_NAME"].ToString();
      Engine = tableRow["ENGINE"].ToString();
      RowFormat = (RowFormat)Enum.Parse(typeof(RowFormat), tableRow["ROW_FORMAT"].ToString());
      AvgRowLength = DataRowHelpers.GetValueAsUInt64(tableRow, "AVG_ROW_LENGTH");
      AutoInc = DataRowHelpers.GetValueAsUInt64(tableRow, "AUTO_INCREMENT");
      Comment = tableRow["TABLE_COMMENT"].ToString();
      Collation = tableRow["TABLE_COLLATION"].ToString();
      if (Collation != null)
      {
        int index = Collation.IndexOf("_");
        if (index != -1)
          CharacterSet = Collation.Substring(0, index);
      }

      string createOpt = (string)tableRow["CREATE_OPTIONS"];
      if (String.IsNullOrEmpty(createOpt))
        ParseCreateOptions(createOpt.ToLowerInvariant());
    }

    private void ParseCreateOptions(string createOptions)
    {
      string[] options = createOptions.Split(' ');
      foreach (string option in options)
      {
        string[] parts = option.Split('=');
        if (parts.Length != 2) continue;
        switch (parts[0])
        {
          case "min_rows":
            MinRows = UInt64.Parse(parts[1]);
            break;
          case "max_rows":
            MaxRows = UInt64.Parse(parts[1]);
            break;
          case "checksum":
            CheckSum = Boolean.Parse(parts[1]);
            break;
          case "pack_keys":
            PackKeys = parts[1] == "1" ? PackKeysMethod.Full : PackKeysMethod.None;
            break;
          case "delay_key_write":
            DelayKeyWrite = parts[1] == "1";
            break;
          // data directory
          // index directory
          // insert method
        }
      }
    }

    private void ParseColumns(DataTable columnData)
    {
      foreach (DataRow row in columnData.Rows)
      {
        Column c = new Column(row);
        c.OwningTable = this;
        Columns.Add(c);
      }
    }

    private void LoadIndexes()
    {
      string[] restrictions = new string[4] { null, owningNode.Database, Name, null };
      DataTable dt = owningNode.GetSchema("Indexes", restrictions);
      foreach (DataRow row in dt.Rows)
      {
        Index i = new Index(this, row);
        Indexes.Add(i);
      }
    }

    private void LoadForeignKeys()
    {
      string[] restrictions = new string[4] { null, owningNode.Database, Name, null };
      DataTable dt = owningNode.GetSchema("Foreign Keys", restrictions);
      foreach (DataRow row in dt.Rows)
      {
        ForeignKey key = new ForeignKey(this, row);
        ForeignKeys.Add(key);
      }
    }

    private string GetTableOptionSql(bool newTable)
    {
      List<string> options = new List<string>();
      StringBuilder sql = new StringBuilder(" ");

      if (!newTable)
      {
        if (Name != OldTable.Name)
          options.Add(String.Format("RENAME TO `{0}` ", Name));
      }
      if (AutoInc != OldTable.AutoInc)
        options.Add(String.Format("AUTO_INCREMENT={0}", AutoInc));
      if (AvgRowLength != OldTable.AvgRowLength)
        options.Add(String.Format("AVG_ROW_LENGTH={0}", AvgRowLength));
      if (CheckSum != OldTable.CheckSum)
        options.Add(String.Format("CHECKSUM={0}", CheckSum ? 1 : 0));
      if (Engine != OldTable.Engine)
        options.Add(String.Format("ENGINE={0}", Engine));
      if (InsertMethod != OldTable.InsertMethod)
        options.Add(String.Format("INSERT_METHOD={0}", InsertMethod.ToString()));
      if (MaxRows != OldTable.MaxRows)
        options.Add(String.Format("MAX_ROWS={0}", MaxRows));
      if (MinRows != OldTable.MinRows)
        options.Add(String.Format("MIN_ROWS={0}", MinRows));
      if (PackKeys != OldTable.PackKeys)
        options.Add(String.Format("PACK_KEYS={0}", PackKeys.ToString()));
      if (RowFormat != OldTable.RowFormat)
        options.Add(String.Format("ROW_FORMAT={0}", RowFormat.ToString()));
      if (StringPropertyHasChanged(Comment, OldTable.Comment))
        options.Add(String.Format("COMMENT='{0}'", Comment));
      if (StringPropertyHasChanged(CharacterSet, OldTable.CharacterSet))
        options.Add(String.IsNullOrEmpty(CharacterSet) ? "DEFAULT CHARACTER SET" :
            String.Format("CHARACTER SET='{0}'", CharacterSet));
      if (StringPropertyHasChanged(Collation, OldTable.Collation))
        options.Add(String.IsNullOrEmpty(Collation) ? "DEFAULT COLLATE" :
            String.Format("COLLATE='{0}'", Collation));
      if (StringPropertyHasChanged(DataDirectory, OldTable.DataDirectory))
        options.Add(String.Format("DATA DIRECTORY='{0}' ", DataDirectory));
      if (StringPropertyHasChanged(IndexDirectory, OldTable.IndexDirectory))
        options.Add(String.Format("INDEX DIRECTORY='{0}' ", IndexDirectory));

      string delimiter = "";
      foreach (string option in options)
      {
        sql.AppendFormat("{0}{1}", delimiter, option);
        delimiter = ",\r\n";
      }
      return sql.ToString();
    }

    private bool StringPropertyHasChanged(string newVal, string oldVal)
    {
      if (newVal == oldVal) return false;
      if (newVal != null && newVal.Length > 0) return true;
      if (oldVal != null && oldVal.Length > 0) return true;
      return false;
    }

    #endregion

    #region ICustomTypeDescriptor Members

    public TypeConverter GetConverter()
    {
      return TypeDescriptor.GetConverter(this, true);
    }

    public EventDescriptorCollection GetEvents(Attribute[] attributes)
    {
      return TypeDescriptor.GetEvents(this, attributes, true);
    }

    EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents()
    {
      return TypeDescriptor.GetEvents(this, true);
    }

    public string GetComponentName()
    {
      return TypeDescriptor.GetComponentName(this, true);
    }

    public object GetPropertyOwner(PropertyDescriptor pd)
    {
      return this;
    }

    public AttributeCollection GetAttributes()
    {
      return TypeDescriptor.GetAttributes(this, true);
    }

    public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
    {
      PropertyDescriptorCollection coll = TypeDescriptor.GetProperties(this, attributes, true);

      List<PropertyDescriptor> props = new List<PropertyDescriptor>();

      string engine = Engine.ToLowerInvariant();
      bool engineIsMyIsam = engine == "myisam";

      foreach (PropertyDescriptor pd in coll)
      {
        if (!pd.IsBrowsable) continue;

        if (pd.Name == "DataDirectory" || pd.Name == "IndexDirectory")
        {
          CustomPropertyDescriptor newPd = new CustomPropertyDescriptor(pd);
          newPd.SetReadOnly(!engineIsMyIsam);
          props.Add(newPd);
        }
        else if ((pd.Name == "DelayKeyWrite" || pd.Name == "CheckSum" || pd.Name == "PackKeys") &&
                !engineIsMyIsam)
        {
        }
        else if (pd.Name == "InsertMethod" && engine != "mrg_myisam") { }
        else
          props.Add(pd);
      }
      return new PropertyDescriptorCollection(props.ToArray());
    }

    PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties()
    {
      return TypeDescriptor.GetProperties(this, true);
    }

    public object GetEditor(Type editorBaseType)
    {
      return TypeDescriptor.GetEditor(this, editorBaseType, true);
    }

    public PropertyDescriptor GetDefaultProperty()
    {
      return TypeDescriptor.GetDefaultProperty(this, true);
    }

    public EventDescriptor GetDefaultEvent()
    {
      return TypeDescriptor.GetDefaultEvent(this, true);
    }

    public string GetClassName()
    {
      return TypeDescriptor.GetClassName(this, true);
    }

    #endregion

    #region Events
    public event EventHandler DataUpdated;

    private void OnDataUpdated()
    {
      if (DataUpdated != null)
        DataUpdated(this, null);
    }
    #endregion
  }
}
