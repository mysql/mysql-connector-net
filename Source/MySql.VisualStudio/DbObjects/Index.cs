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
using System.ComponentModel;
using System.Collections.Generic;
using MySql.Data.VisualStudio.Editors;
using System.Data;
using System.Text;

namespace MySql.Data.VisualStudio.DbObjects
{
  class Index : Object, ICustomTypeDescriptor, ITablePart
  {
    Table table;
    List<IndexColumn> indexColumns = new List<IndexColumn>();
    bool isNew;
    Index oldIndex;
    private enum PropertyDescriptorStyles { Add, AddReadOnly, Skip };

    private Index(Table t)
    {
      table = t;
      isNew = true;
    }

    public Index(Table t, DataRow indexData)
      : this(t)
    {
      isNew = indexData == null;
      oldIndex = new Index(t);
      if (!isNew)
      {
        ParseIndexInfo(indexData);
        (this as ITablePart).Saved();
      }
    }

    private void ParseIndexInfo(DataRow indexData)
    {
      Name = indexData["INDEX_NAME"].ToString();
      IsPrimary = (bool)indexData["PRIMARY"];
      IsUnique = (bool)indexData["UNIQUE"] || IsPrimary;
      Comment = indexData["COMMENT"].ToString();
      string type = indexData["TYPE"].ToString();
      switch (type)
      {
        case "BTREE": IndexUsing = IndexUsingType.BTREE; break;
        case "RTREE": IndexUsing = IndexUsingType.RTREE; break;
        case "HASH": IndexUsing = IndexUsingType.HASH; break;
      }
      FullText = type == "FULLTEXT";
      Spatial = type == "SPATIAL";

      string[] restrictions = new string[5] { null, table.OwningNode.Database, table.Name, Name, null };
      DataTable dt = table.OwningNode.GetSchema("IndexColumns", restrictions);
      foreach (DataRow row in dt.Rows)
      {
        IndexColumn col = new IndexColumn();
        col.OwningIndex = this;
        col.ColumnName = row["COLUMN_NAME"].ToString();
        string sortOrder = row["SORT_ORDER"].ToString();
        //                if (sortOrder == "D")
        //                  col.SortOrder = IndexSortOrder.Descending;
        //            else if (sortOrder == null)
        //              col.SortOrder = IndexSortOrder.Unsorted;
        //        else
        col.SortOrder = IndexSortOrder.Ascending;
        Columns.Add(col);
      }

      if (IsPrimary)
        Type = IndexType.Primary;

      //KeyBlockSize
      //Parser
    }

    #region Properties

    [Browsable(false)]
    public Table Table
    {
      get { return table; }
    }

    private string _name;
    [Category("Identity")]
    [DisplayName("(Name)")]
    [Description("The name of this index/key")]
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    private string _comment;
    [Category("Identity")]
    [Description("A description or comment about this index/key")]
    public string Comment
    {
      get { return _comment; }
      set { _comment = value; }
    }

    [Category("(General)")]
    [Description("The columns of this index/key and their associated sort order")]
    [TypeConverter(typeof(IndexColumnTypeConverter))]
    [Editor(typeof(IndexColumnEditor), typeof(System.Drawing.Design.UITypeEditor))]
    public List<IndexColumn> Columns
    {
      get { return indexColumns; }
    }

    private IndexType _indexType;
    [Category("(General)")]
    [Description("Specifies if this object is an index or primary key")]
    public IndexType Type
    {
      get { return _indexType; }
      set
      { 
        _indexType = value;
        _isPrimary = (_indexType == IndexType.Primary);
      }
    }

    private bool _isUnique;
    [Category("(General)")]
    [DisplayName("Is Unique")]
    [Description("Specifies if this index/key uniquely identifies every row")]
    [TypeConverter(typeof(YesNoTypeConverter))]
    public bool IsUnique
    {
      get { return _isUnique; }
      set { _isUnique = value; }
    }

    private bool _isPrimary;
    [Browsable(false)]
    public bool IsPrimary
    {
      get { return _isPrimary; }
      set { _isPrimary = value; }
    }

    private IndexUsingType _indexUsing;
    [Category("Storage")]
    [DisplayName("Index Algorithm")]
    [Description("Specifies the algorithm that should be used for storing the index/key")]
    public IndexUsingType IndexUsing
    {
      get { return _indexUsing; }
      set { _indexUsing = value; }
    }

    private int _keyBlockSize;
    [Category("Storage")]
    [DisplayName("Key Block Size")]
    [Description("Suggested size in bytes to use for index key blocks.  A zero value means to use the storage engine default.")]
    public int KeyBlockSize
    {
      get { return _keyBlockSize; }
      set { _keyBlockSize = value; }
    }

    private string _parser;
    [Description("Specifies a parser plugin to be used for this index/key.  This is only valid for full-text indexes or keys.")]
    public string Parser
    {
      get { return _parser; }
      set { _parser = value; }
    }

    private bool _fullText;
    [DisplayName("Is Full-text Index/Key")]
    [Description("Specifies if this is a full-text index or key.  This is only supported on MyISAM tables.")]
    [TypeConverter(typeof(YesNoTypeConverter))]
    [RefreshProperties(RefreshProperties.All)]
    public bool FullText
    {
      get { return _fullText; }
      set { _fullText = value; }
    }

    private bool _spatial;
    [DisplayName("Is Spatial Index/Key")]
    [Description("Specifies if this is a spatial index or key.  This is only supported on MyISAM tables.")]
    [TypeConverter(typeof(YesNoTypeConverter))]
    [RefreshProperties(RefreshProperties.All)]
    public bool Spatial
    {
      get { return _spatial; }
      set { _spatial = value; }
    }

    #endregion

    #region ShouldSerialize

    bool ShouldSerializeName() { return false; }
    bool ShouldSerializeComment() { return false; }
    bool ShouldSerializeColumns() { return false; }
    bool ShouldSerializeType() { return false; }
    bool ShouldSerializeIsUnique() { return false; }
    bool ShouldSerializeIndexUsing() { return false; }
    bool ShouldSerializeKeyBlockSize() { return false; }
    bool ShouldSerializeParser() { return false; }
    bool ShouldSerializeFullText() { return false; }
    bool ShouldSerializeSpatial() { return false; }

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
      PropertyDescriptorStyles setStyle;

      foreach (PropertyDescriptor pd in coll)
      {
        if (!pd.IsBrowsable)
          continue;

        if (this.isNew)
          setStyle = PropertyDescriptorStyles.Add;
        else
        {
          switch (pd.Name)
          {
            case "Type":
              setStyle = PropertyDescriptorStyles.AddReadOnly;
              break;
            case "IsUnique":
            case "Name":
              setStyle = (IsPrimary ? PropertyDescriptorStyles.AddReadOnly : PropertyDescriptorStyles.Add);
              break;
            case "FullText":
              setStyle = (Spatial || String.Compare(table.Engine, "myisam", true) != 0 ? PropertyDescriptorStyles.AddReadOnly : PropertyDescriptorStyles.Skip);
              break;
            case "Spatial":
              setStyle = (FullText || String.Compare(table.Engine, "myisam", true) != 0 ? PropertyDescriptorStyles.AddReadOnly : PropertyDescriptorStyles.Skip);
              break;
            default:
              setStyle = PropertyDescriptorStyles.Add;
              break;
          }
        }

        switch (setStyle)
        {
          case PropertyDescriptorStyles.Add:
            props.Add(pd);
            break;
          case PropertyDescriptorStyles.AddReadOnly:
            CustomPropertyDescriptor newPd = new CustomPropertyDescriptor(pd);
            newPd.SetReadOnly(true);
            props.Add(newPd);
            break;
          // when PropertyDescriptorStyles.Skip nothing is added to the props list.
        }
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

    #region ITablePart Members

    void ITablePart.Saved()
    {
      oldIndex.Comment = Comment;
      oldIndex.FullText = FullText;
      oldIndex.IndexUsing = IndexUsing;
      oldIndex.IsPrimary = IsPrimary;
      oldIndex.IsUnique = IsUnique;
      oldIndex.KeyBlockSize = KeyBlockSize;
      oldIndex.Name = Name;
      oldIndex.Parser = Parser;
      oldIndex.Spatial = Spatial;
      oldIndex.Type = Type;

      // now we need to copy the columns
      oldIndex.Columns.Clear();

      foreach (IndexColumn ic in Columns)
      {
        IndexColumn old = new IndexColumn();
        old.ColumnName = ic.ColumnName;
        old.SortOrder = ic.SortOrder;
        old.OwningIndex = oldIndex;
        oldIndex.Columns.Add(old);
      }
    }

    bool ITablePart.HasChanges()
    {
      if (!ObjectHelper.AreEqual(this, oldIndex)) return true;
      if (Columns.Count != oldIndex.Columns.Count) return true;
      foreach (IndexColumn ic in Columns)
      {
        int i = 0;
        for (; i < oldIndex.Columns.Count; i++)
        {
          IndexColumn oic = oldIndex.Columns[i];
          if (oic.ColumnName == ic.ColumnName && oic.SortOrder == ic.SortOrder) break;
        }
        if (i == oldIndex.Columns.Count) return true;
      }
      return false;
    }

    string ITablePart.GetDropSql()
    {
      if (IsPrimary)
        return "DROP PRIMARY KEY";
      return String.Format("DROP KEY `{0}`", Name);
    }

    string ITablePart.GetSql(bool newTable)
    {
      StringBuilder sql = new StringBuilder();

      // if we don't have any changes then just return null
      ITablePart part = this as ITablePart;
      if (!part.HasChanges()) return null;

      if (!newTable)
      {
        if (!String.IsNullOrEmpty(oldIndex.Name))
          sql.AppendFormat("DROP INDEX `{0}`, ", oldIndex.Name);
        sql.Append("ADD ");
      }
      if (IsPrimary)
        sql.Append("PRIMARY KEY ");
      else if (IsUnique)
        sql.Append("UNIQUE ");
      else if (FullText)
        sql.Append("FULLTEXT ");
      else if (Spatial)
        sql.Append("SPATIAL ");

      if (!IsPrimary)
        sql.AppendFormat("{0} ", Type.ToString().ToUpperInvariant());
      sql.AppendFormat("`{0}` (", Name);
      string delimiter = "";
      foreach (IndexColumn c in Columns)
      {
        sql.AppendFormat("{0}{1}", delimiter, c.ColumnName);
        if (c.SortOrder == IndexSortOrder.Ascending)
          sql.Append(" ASC ");
        else
          sql.Append(" DESC ");

        delimiter = ", ";
      }
      sql.Append(")");

      sql.AppendFormat(" USING {0} ", IndexUsing);

      if (!String.IsNullOrEmpty(Comment))
        sql.AppendFormat(" COMMENT '{0}' ", Comment);              
      
      return sql.ToString();
    }

    bool ITablePart.IsNew()
    {
      return isNew;
    }

    #endregion
  }

  enum IndexType
  {
    Index, Primary
  }

  enum IndexUsingType
  {
    BTREE, HASH, RTREE
  }

  public enum IndexSortOrder
  {
    Ascending, Descending
  }

  class IndexColumn
  {
    public Index OwningIndex;
    public string ColumnName;
    public IndexSortOrder SortOrder;
  }
}
