' Copyright (C) 2004-2005 MySQL AB
'
' This program is free software; you can redistribute it and/or modify
' it under the terms of the GNU General Public License version 2 as published by
' the Free Software Foundation
'
' There are special exceptions to the terms and conditions of the GPL 
' as it is applied to this software. View the full text of the 
' exception in file EXCEPTIONS in the directory of this software 
' distribution.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License
' along with this program; if not, write to the Free Software
' Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

Imports System
Imports System.Data
Imports System.Windows.Forms
Imports MySql.Data.MySqlClient

Public Class Form1
    Inherits System.Windows.Forms.Form

    Dim conn As MySqlConnection
    Dim data As DataTable
    Dim da As MySqlDataAdapter
    Dim cb As MySqlCommandBuilder


#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call

    End Sub

    'Form overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    Friend WithEvents databaseList As System.Windows.Forms.ComboBox
    Friend WithEvents label5 As System.Windows.Forms.Label
    Friend WithEvents updateBtn As System.Windows.Forms.Button
    Friend WithEvents dataGrid As System.Windows.Forms.DataGrid
    Friend WithEvents tables As System.Windows.Forms.ComboBox
    Friend WithEvents connectBtn As System.Windows.Forms.Button
    Friend WithEvents password As System.Windows.Forms.TextBox
    Friend WithEvents label3 As System.Windows.Forms.Label
    Friend WithEvents userid As System.Windows.Forms.TextBox
    Friend WithEvents label2 As System.Windows.Forms.Label
    Friend WithEvents server As System.Windows.Forms.TextBox
    Friend WithEvents label1 As System.Windows.Forms.Label
    Friend WithEvents label4 As System.Windows.Forms.Label
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.databaseList = New System.Windows.Forms.ComboBox
        Me.label5 = New System.Windows.Forms.Label
        Me.updateBtn = New System.Windows.Forms.Button
        Me.dataGrid = New System.Windows.Forms.DataGrid
        Me.tables = New System.Windows.Forms.ComboBox
        Me.connectBtn = New System.Windows.Forms.Button
        Me.password = New System.Windows.Forms.TextBox
        Me.label3 = New System.Windows.Forms.Label
        Me.userid = New System.Windows.Forms.TextBox
        Me.label2 = New System.Windows.Forms.Label
        Me.server = New System.Windows.Forms.TextBox
        Me.label1 = New System.Windows.Forms.Label
        Me.label4 = New System.Windows.Forms.Label
        CType(Me.dataGrid, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'databaseList
        '
        Me.databaseList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.databaseList.Location = New System.Drawing.Point(80, 80)
        Me.databaseList.Name = "databaseList"
        Me.databaseList.Size = New System.Drawing.Size(296, 21)
        Me.databaseList.TabIndex = 24
        '
        'label5
        '
        Me.label5.Location = New System.Drawing.Point(8, 88)
        Me.label5.Name = "label5"
        Me.label5.Size = New System.Drawing.Size(64, 16)
        Me.label5.TabIndex = 23
        Me.label5.Text = "Databases"
        '
        'updateBtn
        '
        Me.updateBtn.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.updateBtn.Location = New System.Drawing.Point(464, 104)
        Me.updateBtn.Name = "updateBtn"
        Me.updateBtn.Size = New System.Drawing.Size(80, 23)
        Me.updateBtn.TabIndex = 22
        Me.updateBtn.Text = "Update"
        '
        'dataGrid
        '
        Me.dataGrid.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dataGrid.DataMember = ""
        Me.dataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText
        Me.dataGrid.Location = New System.Drawing.Point(8, 136)
        Me.dataGrid.Name = "dataGrid"
        Me.dataGrid.Size = New System.Drawing.Size(536, 384)
        Me.dataGrid.TabIndex = 21
        '
        'tables
        '
        Me.tables.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.tables.Location = New System.Drawing.Point(80, 104)
        Me.tables.Name = "tables"
        Me.tables.Size = New System.Drawing.Size(296, 21)
        Me.tables.TabIndex = 20
        '
        'connectBtn
        '
        Me.connectBtn.Location = New System.Drawing.Point(400, 8)
        Me.connectBtn.Name = "connectBtn"
        Me.connectBtn.TabIndex = 19
        Me.connectBtn.Text = "Connect"
        '
        'password
        '
        Me.password.Location = New System.Drawing.Point(264, 32)
        Me.password.Name = "password"
        Me.password.PasswordChar = Microsoft.VisualBasic.ChrW(42)
        Me.password.Size = New System.Drawing.Size(116, 20)
        Me.password.TabIndex = 18
        Me.password.Text = ""
        '
        'label3
        '
        Me.label3.Location = New System.Drawing.Point(192, 40)
        Me.label3.Name = "label3"
        Me.label3.Size = New System.Drawing.Size(56, 16)
        Me.label3.TabIndex = 17
        Me.label3.Text = "Password:"
        '
        'userid
        '
        Me.userid.Location = New System.Drawing.Point(56, 32)
        Me.userid.Name = "userid"
        Me.userid.Size = New System.Drawing.Size(120, 20)
        Me.userid.TabIndex = 16
        Me.userid.Text = ""
        '
        'label2
        '
        Me.label2.Location = New System.Drawing.Point(8, 40)
        Me.label2.Name = "label2"
        Me.label2.Size = New System.Drawing.Size(48, 16)
        Me.label2.TabIndex = 15
        Me.label2.Text = "User Id:"
        '
        'server
        '
        Me.server.Location = New System.Drawing.Point(56, 8)
        Me.server.Name = "server"
        Me.server.Size = New System.Drawing.Size(320, 20)
        Me.server.TabIndex = 14
        Me.server.Text = ""
        '
        'label1
        '
        Me.label1.Location = New System.Drawing.Point(8, 16)
        Me.label1.Name = "label1"
        Me.label1.Size = New System.Drawing.Size(48, 16)
        Me.label1.TabIndex = 12
        Me.label1.Text = "Server:"
        '
        'label4
        '
        Me.label4.Location = New System.Drawing.Point(8, 112)
        Me.label4.Name = "label4"
        Me.label4.Size = New System.Drawing.Size(64, 16)
        Me.label4.TabIndex = 13
        Me.label4.Text = "Tables"
        '
        'Form1
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(552, 525)
        Me.Controls.Add(Me.databaseList)
        Me.Controls.Add(Me.label5)
        Me.Controls.Add(Me.updateBtn)
        Me.Controls.Add(Me.dataGrid)
        Me.Controls.Add(Me.tables)
        Me.Controls.Add(Me.connectBtn)
        Me.Controls.Add(Me.password)
        Me.Controls.Add(Me.label3)
        Me.Controls.Add(Me.userid)
        Me.Controls.Add(Me.label2)
        Me.Controls.Add(Me.server)
        Me.Controls.Add(Me.label1)
        Me.Controls.Add(Me.label4)
        Me.Name = "Form1"
        Me.Text = "Form1"
        CType(Me.dataGrid, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

#End Region

    Private Sub connectBtn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles connectBtn.Click
        If Not conn Is Nothing Then conn.Close()

        Dim connStr As String
        connStr = String.Format("server={0};user id={1}; password={2}; database=mysql; pooling=false", _
				server.Text, userid.Text, password.Text )

        Try
            conn = New MySqlConnection(connStr)
            conn.Open()

            GetDatabases()
        Catch ex As MySqlException
            MessageBox.Show("Error connecting to the server: " + ex.Message)
        End Try
    End Sub

    Private Sub GetDatabases()
        Dim reader As MySqlDataReader
        reader = Nothing

        Dim cmd As New MySqlCommand("SHOW DATABASES", conn)
        Try
            reader = cmd.ExecuteReader()
            databaseList.Items.Clear()

            While (reader.Read())
                databaseList.Items.Add(reader.GetString(0))
            End While
        Catch ex As MySqlException
            MessageBox.Show("Failed to populate database list: " + ex.Message)
        Finally
            If Not reader Is Nothing Then reader.Close()
        End Try

    End Sub

    Private Sub databaseList_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles databaseList.SelectedIndexChanged
        Dim reader As MySqlDataReader

        conn.ChangeDatabase(databaseList.SelectedItem.ToString())

        Dim cmd As New MySqlCommand("SHOW TABLES", conn)

        Try
            reader = cmd.ExecuteReader()
            tables.Items.Clear()

            While (reader.Read())
                tables.Items.Add(reader.GetString(0))
            End While

        Catch ex As MySqlException
            MessageBox.Show("Failed to populate table list: " + ex.Message)
        Finally
            If Not reader Is Nothing Then reader.Close()
        End Try
    End Sub

    Private Sub tables_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles tables.SelectedIndexChanged
        data = New DataTable

        da = New MySqlDataAdapter("SELECT * FROM " + tables.SelectedItem.ToString(), conn)
        cb = New MySqlCommandBuilder(da)

        da.Fill(data)

        dataGrid.DataSource = data
    End Sub

    Private Sub updateBtn_Click(ByVal sender As Object, ByVal e As EventArgs) Handles updateBtn.Click
        Dim changes As DataTable = data.GetChanges()
        da.Update(changes)
        data.AcceptChanges()
    End Sub

End Class
