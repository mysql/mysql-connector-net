// Copyright (C) 2004-2007 MySQL AB
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
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
	/// <summary>
	/// Summary description for BlobTests.
	/// </summary>
	[TestFixture]
	public class CursorTests : BaseTest
	{
		protected override void FixtureSetup()
		{
			Open();

			execSQL("DROP TABLE IF EXISTS Test");
			execSQL("DROP TABLE IF EXISTS Test2");
			execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), PRIMARY KEY(id))");
			execSQL("CREATE TABLE Test2 (id INT NOT NULL, parent INT, PRIMARY KEY(id))");
		}

		protected override void TestFixtureTearDown() 
		{
			execSQL("DROP TABLE IF EXISTS Test2");
			Close();
		}

/*		[Test]
		public void NestedCursors() 
		{
			execSQL("INSERT INTO Test VALUES (1, 'Test1')");
			execSQL("INSERT INTO Test VALUES (2, 'Test2')");
			execSQL("INSERT INTO Test VALUES (3, 'Test3')");
			execSQL("INSERT INTO Test2 VALUES (66, 1)");
			execSQL("INSERT INTO Test2 VALUES (77, 2)");
			execSQL("INSERT INTO Test2 VALUES (88, 3)");

			MySqlCommand cmdOuter = new MySqlCommand("SELECT * FROM Test", conn);
			MySqlDataReader reader = null;
			MySqlDataReader reader2 = null;
			try 
			{
				cmdOuter.Prepare(1);
				reader = cmdOuter.ExecuteReader();
				Assert.IsTrue( reader.Read() );
				Assert.AreEqual( 1, reader.GetInt32(0) );
				Assert.AreEqual( "Test1", reader.GetString(1) );

				MySqlCommand cmd = new MySqlCommand( "SELECT * FROM Test2 WHERE parent=" + reader.GetInt32(0), conn );
				cmd.Prepare(1);
				reader2 = cmd.ExecuteReader();
				Assert.IsTrue( reader2.Read() );
				Assert.AreEqual( 66, reader2.GetInt32(0) );
				reader2.Close();

				Assert.IsTrue( reader.Read() );
				Assert.AreEqual( 2, reader.GetInt32(0) );
				Assert.AreEqual( "Test2", reader.GetString(1) );

				cmd.CommandText = "SELECT * FROM Test2 WHERE parent=" + reader.GetInt32(0);
				cmd.Prepare(1);
				reader2 = cmd.ExecuteReader();
				Assert.IsTrue( reader2.Read() );
				Assert.AreEqual( 77, reader2.GetInt32(0) );
				reader2.Close();

				Assert.IsTrue( reader.Read() );
				Assert.AreEqual( 3, reader.GetInt32(0) );
				Assert.AreEqual( "Test3", reader.GetString(1) );

				cmd.CommandText = "SELECT * FROM Test2 WHERE parent=" + reader.GetInt32(0);
				cmd.Prepare(1);
				reader2 = cmd.ExecuteReader();
				Assert.IsTrue( reader2.Read() );
				Assert.AreEqual( 88, reader2.GetInt32(0) );
				reader2.Close();

				Assert.IsFalse( reader.NextResult() );
			}
			catch (Exception ex) 
			{
				Assert.Fail( ex.Message );
			}
			finally 
			{
				if (reader != null) reader.Close();
			}
		}
*/
/*		[Test]
		public void SimpleCursors() 
		{
			execSQL("INSERT INTO Test VALUES (1, 'Test1')");
			execSQL("INSERT INTO Test VALUES (2, 'Test2')");
			execSQL("INSERT INTO Test VALUES (3, 'Test3')");
			execSQL("INSERT INTO Test VALUES (4, 'Test4')");
			execSQL("INSERT INTO Test VALUES (5, 'Test5')");

			MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
			MySqlDataReader reader =null;
			try 
			{
				cmd.Prepare(1);
				reader = cmd.ExecuteReader();
				Assert.IsTrue( reader.Read() );
				Assert.AreEqual( 1, reader.GetInt32(0) );
				Assert.AreEqual( "Test1", reader.GetString(1) );
				Assert.IsTrue( reader.Read() );
				Assert.AreEqual( 2, reader.GetInt32(0) );
				Assert.AreEqual( "Test2", reader.GetString(1) );
				Assert.IsTrue( reader.Read() );
				Assert.AreEqual( 3, reader.GetInt32(0) );
				Assert.AreEqual( "Test3", reader.GetString(1) );
				Assert.IsTrue( reader.Read() );
				Assert.AreEqual( 4, reader.GetInt32(0) );
				Assert.AreEqual( "Test4", reader.GetString(1) );
				Assert.IsTrue( reader.Read() );
				Assert.AreEqual( 5, reader.GetInt32(0) );
				Assert.AreEqual( "Test5", reader.GetString(1) );

				Assert.IsFalse( reader.NextResult() );
			}
			catch (Exception ex) 
			{
				Assert.Fail( ex.Message );
			}
			finally 
			{
				if (reader != null) reader.Close();
			}
		}*/

	}
}
