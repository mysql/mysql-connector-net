// Copyright � 2004, 2013, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.Types;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Collections;
#if !RT
using System.Data;
using System.Data.Common;
#endif

namespace MySql.Data.MySqlClient
{
    /// <summary>
    /// Represents a parameter to a <see cref="MySqlCommand"/>, and optionally, its mapping to <see cref="DataSet"/> columns. This class cannot be inherited.
    /// </summary>
    public sealed partial class MySqlParameter : DbParameter, ICloneable
    {
        private const int UNSIGNED_MASK = 0x8000;
        private object paramValue;
        private string paramName;
        private DbType dbType;
        private MySqlDbType mySqlDbType;
        private bool inferType = true;
        private const int GEOMETRY_LENGTH = 25;

        #region Constructors

        public MySqlParameter()
        {
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlParameter"/> class with the parameter name and a value of the new MySqlParameter.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to map. </param>
        /// <param name="value">An <see cref="Object"/> that is the value of the <see cref="MySqlParameter"/>. </param>
        public MySqlParameter(string parameterName, object value) : this()
        {
            ParameterName = parameterName;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlParameter"/> class with the parameter name and the data type.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to map. </param>
        /// <param name="dbType">One of the <see cref="MySqlDbType"/> values. </param>
        public MySqlParameter(string parameterName, MySqlDbType dbType) : this(parameterName, null)
        {
            MySqlDbType = dbType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlParameter"/> class with the parameter name, the <see cref="MySqlDbType"/>, and the size.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to map. </param>
        /// <param name="dbType">One of the <see cref="MySqlDbType"/> values. </param>
        /// <param name="size">The length of the parameter. </param>
        public MySqlParameter(string parameterName, MySqlDbType dbType, int size) : this(parameterName, dbType)
        {
            Size = size;
        }

        public MySqlParameter(string parameterName, MySqlDbType dbType, int size, string sourceColumn) : this(parameterName, dbType, size)
        {
            SourceColumn = sourceColumn;
        }

        partial void Init();

        #endregion

        #region Properties

        [Category("Misc")]
        public override string ParameterName
        {
            get { return paramName; }
            set { SetParameterName(value); }
        }

        internal MySqlParameterCollection Collection { get; set; }
        internal Encoding Encoding { get; set; }

        internal bool TypeHasBeenSet
        {
            get { return inferType == false; }
        }


        internal string BaseName
        {
            get
            {
                if (ParameterName.StartsWith("@", StringComparison.Ordinal) || ParameterName.StartsWith("?", StringComparison.Ordinal))
                    return ParameterName.Substring(1);
                return ParameterName;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is input-only, output-only, bidirectional, or a stored procedure return value parameter.
        /// As of MySql version 4.1 and earlier, input-only is the only valid choice.
        /// </summary>
        [Category("Data")]
        public override ParameterDirection Direction { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter accepts null values.
        /// </summary>
        [Browsable(false)]
        public override Boolean IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the MySqlDbType of the parameter.
        /// </summary>
        [Category("Data")]
        [DbProviderSpecificTypeProperty(true)]
        public MySqlDbType MySqlDbType
        {
            get { return mySqlDbType; }
            set
            {
                SetMySqlDbType(value);
                inferType = false;
            }
        }
        [Category("Data")]
        public DataRowVersion SourceVersion { get; set; }

        /// <summary>
        /// Gets or sets the name of the source column that is mapped to the <see cref="DataSet"/> and used for loading or returning the <see cref="Value"/>.
        /// </summary>
        [Category("Data")]
        public override String SourceColumn { get; set; }

        /// <summary>
        /// Resets the <b>DbType</b> property to its original settings. 
        /// </summary>
        public override void ResetDbType()
        {
            inferType = true;
        }

        /// <summary>
        /// Sets or gets a value which indicates whether the source column is nullable. 
        /// This allows <see cref="DbCommandBuilder"/> to correctly generate Update statements 
        /// for nullable columns. 
        /// </summary>
        public override bool SourceColumnNullMapping { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbType"/> of the parameter.
        /// </summary>
        public override DbType DbType
        {
            get { return dbType; }
            set
            {
                SetDbType(value);
                inferType = false;
            }
        }

        partial void SetDbTypeFromMySqlDbType()
        {
            switch (mySqlDbType)
            {
                case MySqlDbType.NewDecimal:
                case MySqlDbType.Decimal:
                    dbType = DbType.Decimal;
                    break;
                case MySqlDbType.Byte:
                    dbType = DbType.SByte;
                    break;
                case MySqlDbType.UByte:
                    dbType = DbType.Byte;
                    break;
                case MySqlDbType.Int16:
                    dbType = DbType.Int16;
                    break;
                case MySqlDbType.UInt16:
                    dbType = DbType.UInt16;
                    break;
                case MySqlDbType.Int24:
                case MySqlDbType.Int32:
                    dbType = DbType.Int32;
                    break;
                case MySqlDbType.UInt24:
                case MySqlDbType.UInt32:
                    dbType = DbType.UInt32;
                    break;
                case MySqlDbType.Int64:
                    dbType = DbType.Int64;
                    break;
                case MySqlDbType.UInt64:
                    dbType = DbType.UInt64;
                    break;
                case MySqlDbType.Bit:
                    dbType = DbType.UInt64;
                    break;
                case MySqlDbType.Float:
                    dbType = DbType.Single;
                    break;
                case MySqlDbType.Double:
                    dbType = DbType.Double;
                    break;
                case MySqlDbType.Timestamp:
                case MySqlDbType.DateTime:
                    dbType = DbType.DateTime;
                    break;
                case MySqlDbType.Date:
                case MySqlDbType.Newdate:
                case MySqlDbType.Year:
                    dbType = DbType.Date;
                    break;
                case MySqlDbType.Time:
                    dbType = DbType.Time;
                    break;
                case MySqlDbType.Enum:
                case MySqlDbType.Set:
                case MySqlDbType.VarChar:
                    dbType = DbType.String;
                    break;
                case MySqlDbType.TinyBlob:
                case MySqlDbType.MediumBlob:
                case MySqlDbType.LongBlob:
                case MySqlDbType.Blob:
                    dbType = DbType.Object;
                    break;
                case MySqlDbType.String:
                    dbType = DbType.StringFixedLength;
                    break;
                case MySqlDbType.Guid:
                    dbType = DbType.Guid;
                    break;
            }
        }


        private void SetDbType(DbType db_type)
        {
            dbType = db_type;
            switch (dbType)
            {
                case DbType.Guid:
                    mySqlDbType = MySqlDbType.Guid;
                    break;

                case DbType.AnsiString:
                case DbType.String:
                    mySqlDbType = MySqlDbType.VarChar;
                    break;

                case DbType.AnsiStringFixedLength:
                case DbType.StringFixedLength:
                    mySqlDbType = MySqlDbType.String;
                    break;

                case DbType.Boolean:
                case DbType.Byte:
                    mySqlDbType = MySqlDbType.UByte;
                    break;

                case DbType.SByte:
                    mySqlDbType = MySqlDbType.Byte;
                    break;

                case DbType.Date:
                    mySqlDbType = MySqlDbType.Date;
                    break;
                case DbType.DateTime:
                    mySqlDbType = MySqlDbType.DateTime;
                    break;

                case DbType.Time:
                    mySqlDbType = MySqlDbType.Time;
                    break;
                case DbType.Single:
                    mySqlDbType = MySqlDbType.Float;
                    break;
                case DbType.Double:
                    mySqlDbType = MySqlDbType.Double;
                    break;

                case DbType.Int16:
                    mySqlDbType = MySqlDbType.Int16;
                    break;
                case DbType.UInt16:
                    mySqlDbType = MySqlDbType.UInt16;
                    break;

                case DbType.Int32:
                    mySqlDbType = MySqlDbType.Int32;
                    break;
                case DbType.UInt32:
                    mySqlDbType = MySqlDbType.UInt32;
                    break;

                case DbType.Int64:
                    mySqlDbType = MySqlDbType.Int64;
                    break;
                case DbType.UInt64:
                    mySqlDbType = MySqlDbType.UInt64;
                    break;

                case DbType.Decimal:
                case DbType.Currency:
                    mySqlDbType = MySqlDbType.Decimal;
                    break;

                case DbType.Object:
                case DbType.VarNumeric:
                case DbType.Binary:
                default:
                    mySqlDbType = MySqlDbType.Blob;
                    break;
            }

            if (dbType == DbType.Object)
            {
                var value = this.paramValue as byte[];
                if (value != null && value.Length == GEOMETRY_LENGTH)
                    mySqlDbType = MySqlDbType.Geometry;
            }

            ValueObject = MySqlField.GetIMySqlValue(mySqlDbType);
        }

        /// <summary>
        /// Gets or sets the maximum number of digits used to represent the <see cref="Value"/> property.
        /// </summary>
        [Category("Data")]
        public byte Precision { get; set; }

        /// <summary>
        /// Gets or sets the number of decimal places to which <see cref="Value"/> is resolved.
        /// </summary>
        [Category("Data")]
        public byte Scale { get; set; }

        /// <summary>
        /// Gets or sets the maximum size, in bytes, of the data within the column.
        /// </summary>
        [Category("Data")]
        public override int Size { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
#if !CF && !RT && !NETSTANDARD1_3
    [TypeConverter(typeof(StringConverter))]
    [Category("Data")]
#endif
        public override object Value
        {
            get { return paramValue; }
            set
            {
                paramValue = value;
                byte[] valueAsByte = value as byte[];
                string valueAsString = value as string;

                if (valueAsByte != null)
                    Size = valueAsByte.Length;
                else if (valueAsString != null)
                    Size = valueAsString.Length;
                if (inferType)
                    SetTypeFromValue();
            }
        }

        private IMySqlValue _valueObject;
        internal IMySqlValue ValueObject
        {
            get { return _valueObject; }
            private set
            {
                _valueObject = value;
            }
        }

        /// <summary>
        /// Returns the possible values for this parameter if this parameter is of type
        /// SET or ENUM.  Returns null otherwise.
        /// </summary>
        public IList PossibleValues { get; internal set; }

        #endregion

        private void SetParameterName(string name)
        {
            if (Collection != null)
                Collection.ParameterNameChanged(this, paramName, name);
            paramName = name;
        }

        /// <summary>
        /// Overridden. Gets a string containing the <see cref="ParameterName"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return paramName;
        }

        internal int GetPSType()
        {
            switch (mySqlDbType)
            {
                case MySqlDbType.Bit:
                    return (int)MySqlDbType.Int64 | UNSIGNED_MASK;
                case MySqlDbType.UByte:
                    return (int)MySqlDbType.Byte | UNSIGNED_MASK;
                case MySqlDbType.UInt64:
                    return (int)MySqlDbType.Int64 | UNSIGNED_MASK;
                case MySqlDbType.UInt32:
                    return (int)MySqlDbType.Int32 | UNSIGNED_MASK;
                case MySqlDbType.UInt24:
                    return (int)MySqlDbType.Int32 | UNSIGNED_MASK;
                case MySqlDbType.UInt16:
                    return (int)MySqlDbType.Int16 | UNSIGNED_MASK;
                default:
                    return (int)mySqlDbType;
            }
        }

        internal void Serialize(MySqlPacket packet, bool binary, MySqlConnectionStringBuilder settings)
        {
            if (!binary && (paramValue == null || paramValue == DBNull.Value))
                packet.WriteStringNoNull("NULL");
            else
            {
                if (ValueObject.MySqlDbType == MySqlDbType.Guid)
                {
                    MySqlGuid g = (MySqlGuid)ValueObject;
                    g.OldGuids = settings.OldGuids;
                    ValueObject = g;
                }
#if !CF
                if (ValueObject.MySqlDbType == MySqlDbType.Geometry)
                {
                    MySqlGeometry v = (MySqlGeometry)ValueObject;
                    if (v.IsNull && Value != null)
                    {
                        MySqlGeometry.TryParse(Value.ToString(), out v);
                    }
                    ValueObject = v;
                }
#endif
                ValueObject.WriteValue(packet, binary, paramValue, Size);
            }
        }

        partial void SetDbTypeFromMySqlDbType();

        private void SetMySqlDbType(MySqlDbType mysql_dbtype)
        {
            mySqlDbType = mysql_dbtype;
            ValueObject = MySqlField.GetIMySqlValue(mySqlDbType);
            SetDbTypeFromMySqlDbType();
        }

        private void SetTypeFromValue()
        {
            if (paramValue == null || paramValue == DBNull.Value) return;

            if (paramValue is Guid)
                MySqlDbType = MySqlDbType.Guid;
            else if (paramValue is TimeSpan)
                MySqlDbType = MySqlDbType.Time;
            else if (paramValue is bool)
                MySqlDbType = MySqlDbType.Byte;
            else
            {
                Type t = paramValue.GetType();
                switch (t.Name)
                {
                    case "SByte": MySqlDbType = MySqlDbType.Byte; break;
                    case "Byte": MySqlDbType = MySqlDbType.UByte; break;
                    case "Int16": MySqlDbType = MySqlDbType.Int16; break;
                    case "UInt16": MySqlDbType = MySqlDbType.UInt16; break;
                    case "Int32": MySqlDbType = MySqlDbType.Int32; break;
                    case "UInt32": MySqlDbType = MySqlDbType.UInt32; break;
                    case "Int64": MySqlDbType = MySqlDbType.Int64; break;
                    case "UInt64": MySqlDbType = MySqlDbType.UInt64; break;
                    case "DateTime": MySqlDbType = MySqlDbType.DateTime; break;
                    case "String": MySqlDbType = MySqlDbType.VarChar; break;
                    case "Single": MySqlDbType = MySqlDbType.Float; break;
                    case "Double": MySqlDbType = MySqlDbType.Double; break;

                    case "Decimal": MySqlDbType = MySqlDbType.Decimal; break;
                    case "Object":
                    default:
#if RT || NETSTANDARD1_3
                        if (t.GetTypeInfo().BaseType == typeof(Enum))
#else
            if( t.BaseType == typeof( Enum ) )
#endif
                            MySqlDbType = MySqlDbType.Int32;
                        else
                            MySqlDbType = MySqlDbType.Blob;
                        break;
                }
            }
        }

        #region ICloneable

        public MySqlParameter Clone()
        {
#if RT || NETSTANDARD1_3
            MySqlParameter clone = new MySqlParameter(paramName, mySqlDbType);
#else
      MySqlParameter clone = new MySqlParameter(paramName, mySqlDbType, Direction, SourceColumn, SourceVersion, paramValue);
#endif
            // if we have not had our type set yet then our clone should not either
            clone.inferType = inferType;
            return clone;
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        #endregion

        // this method is pretty dumb but we want it to be fast.  it doesn't return size based
        // on value and type but just on the value.
        internal long EstimatedSize()
        {
            if (Value == null || Value == DBNull.Value)
                return 4; // size of NULL
            if (Value is byte[])
                return (Value as byte[]).Length;
            if (Value is string)
                return (Value as string).Length * 4; // account for UTF-8 (yeah I know)
            if (Value is decimal || Value is float)
                return 64;
            return 32;
        }

    }

}
