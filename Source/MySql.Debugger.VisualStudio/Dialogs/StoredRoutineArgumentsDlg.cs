using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MySql.Debugger.VisualStudio
{
  public partial class StoredRoutineArgumentsDlg : Form
  {
    public StoredRoutineArgumentsDlg()
    {
      InitializeComponent();
      Init();
    }

    private void Init()
    {
      Arguments = new DataTable();
      Arguments.Columns.Add("Name", typeof(string));
      Arguments.Columns.Add("Value", typeof(string));
      Arguments.Columns.Add("IsNull", typeof(bool));
      Arguments.Columns.Add("Type", typeof(string));
    }

    private void btnOK_Click(object sender, EventArgs e)
    {
      this.DialogResult = DialogResult.OK;
      this.Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
      this.DialogResult = DialogResult.Cancel;
      this.Close();
    }

    private void StoredRoutineArguments_Load(object sender, EventArgs e)
    {

    }

    internal DataTable Arguments { get; set; }

    internal void AddNameValue( string Name, string Value, string type )
    {
      Arguments.Rows.Add(Name, Value, false, type);
    }

    internal void DataBind()
    {
      gridArguments.AutoGenerateColumns = false;
      gridArguments.DataSource = Arguments;
    }

    internal IEnumerable<NameValue> GetNameValues()
    {
      foreach (DataRow dr in Arguments.Rows)
      {
        yield return new NameValue() { Name = ( string )dr[ 0 ], Value = ( string )dr[ 1 ], IsNull = (bool)dr[2] };
      }
    }

    private void gridArguments_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
      // checkbox change event
      if (e.RowIndex > -1 && e.ColumnIndex == gridArguments.Columns["colNull"].Index)
      {
        bool isChecked = (bool)gridArguments[e.ColumnIndex, e.RowIndex].EditedFormattedValue;
        int valueColumnIndex = gridArguments.Columns["colValue"].Index;
        gridArguments[valueColumnIndex, e.RowIndex].ReadOnly = isChecked;
        gridArguments[valueColumnIndex, e.RowIndex].Style.ForeColor = isChecked ? SystemColors.GrayText : SystemColors.ControlText;
      }
    }

    private void gridArguments_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
      gridArguments_CellContentClick(sender, e);
    }
  }

  internal class NameValue
  {
    internal string Name { get; set; }
    internal string Value { get; set; }
    internal bool IsNull { get; set; }
  }
}
