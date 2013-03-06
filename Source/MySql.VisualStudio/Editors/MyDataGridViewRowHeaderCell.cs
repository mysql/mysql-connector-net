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

using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using MySql.Data.VisualStudio.Properties;
using MySql.Data.VisualStudio.DbObjects;

namespace MySql.Data.VisualStudio.Editors
{
  class MyDataGridViewRowHeaderCell : DataGridViewRowHeaderCell
  {
    private List<Column> Columns
    {
      get { return (DataGridView.DataSource as BindingSource).DataSource as List<Column>; }
    }

    protected override void Paint(Graphics graphics, Rectangle clipBounds,
        Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState,
        object value, object formattedValue, string errorText,
        DataGridViewCellStyle cellStyle,
        DataGridViewAdvancedBorderStyle advancedBorderStyle,
        DataGridViewPaintParts paintParts)
    {
      if (Columns.Count > rowIndex)
      {
        Column c = Columns[rowIndex];
        if (c.PrimaryKey)
        {
          Bitmap bmp = rowIndex == DataGridView.CurrentRow.Index ?
              Resources.ArrowKey : Resources.Key;
          bmp.MakeTransparent();
          paintParts &= ~DataGridViewPaintParts.ContentBackground;
          base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState,
              value, formattedValue, errorText, cellStyle, advancedBorderStyle,
              paintParts);
          Rectangle r = cellBounds;
          r.Offset(bmp.Width / 2, bmp.Height / 2);
          r.Width = bmp.Width;
          r.Height = bmp.Height;
          graphics.DrawImage(bmp, r);
          return;
        }
      }

      base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState,
          value, formattedValue, errorText, cellStyle, advancedBorderStyle,
          paintParts);
    }
  }
}
