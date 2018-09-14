// Copyright (c) 2018, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Reflection;

namespace MySqlX.Data.Performance.Console.Tests
{
  public class Program
  {
    /// <summary>
    /// Number of times to execute the operation.
    /// </summary>
    private const int EXECUTION_COUNT = 1;

    /// <summary>
    /// Namespaces.
    /// </summary>
    private const string X_DEVAPI_TESTS_MAIN_NAMESPACE = "MySqlX.Data.Tests";
    private const string X_DEVAPI_RELATIONAL_TESTS_NAMESPACE = "MySqlX.Data.Tests.RelationalTests";
    private const string X_DEVAPI_CRUD_TESTS_NAMESPACE = "MySqlX.Data.Tests.CrudTests";
    private const string X_DEVAPI_RESULT_TESTS_NAMESPACE = "MySqlX.Data.Tests.ResultTests";

    static void Main(string[] args)
    {
      if (args.Length > 0)
      {
        switch (args.Length)
        {
          case 1:
            Program.ExecuteCase(Convert.ToInt32(args[0]));
            break;
          case 2:
          case 3:
            Program.ExecuteCase(1, args);
            break;
          default:
            break;
        }
      }
      else
      {
        System.Console.WriteLine("No arguments were found.");
        Program.ExecuteCase(2);
      }

      System.Console.WriteLine("Execution completed.");
      System.Console.ReadKey();
    }

    static void ExecuteCase(int caseToExecute, string[] args = null)
    {
      switch (caseToExecute)
      {
        case 0:
          System.Console.WriteLine("Run tests from the PerformanceTests class.");
          ExecuteAllClassTests("PerformanceTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          break;
        case 1:
          System.Console.WriteLine("Run tests from a specific class.");
          ExecuteAllClassTests(
            args[0], args.Length == 3 ?
              args[1] : X_DEVAPI_TESTS_MAIN_NAMESPACE,
            args.Length == 2 ?
              Convert.ToInt32(args[1]) : args.Length == 3 ?
                Convert.ToInt32(args[2]) : EXECUTION_COUNT);
          break;
        case 2:
          System.Console.WriteLine("Run all unit tests.");
          ExecuteAllClassTests("BasicFindTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("BasicSelectTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("CharsetAndCollationTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("CollectionIndexTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("CollectionTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("CollectionAsyncTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("CrudInsertTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("CrudRemoveTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("CrudUpdateTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("MergePatch", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("SchemaTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("SessionTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("TransactionTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("ExprParserTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("DbDocTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("JsonParserTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("ColumnMetadataTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("DataTypeTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("RowBufferingTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("DateTimeTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("SqlTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("TableAsyncTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("TableDeleteTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("TableInsertTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("TableSelectTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("TableUpdateTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("ViewTests", X_DEVAPI_RELATIONAL_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("RelationalGCTests", X_DEVAPI_RESULT_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("CrudGCTests", X_DEVAPI_RESULT_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("DocBufferingTests", X_DEVAPI_CRUD_TESTS_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("PerformanceTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("UnixSocketsTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          ExecuteAllClassTests("ClientSideFailoverTests", X_DEVAPI_TESTS_MAIN_NAMESPACE, EXECUTION_COUNT);
          break;
        default:
          System.Console.WriteLine("Invalid value. Use 0 to execute PerformanceTests tests," +
            "1 to execute tests from a specific class or 2 to execute all tests.");
          break;
      }
    }

    static void ExecuteAllClassTests(string className, string nameSpace, int executionCount)
    {
      System.Console.WriteLine(string.Format("{0} -------------------------- ", className));
      var type = Type.GetType(string.Format("{0}.{1},{2}", nameSpace, className, X_DEVAPI_TESTS_MAIN_NAMESPACE));
      var methodInfos = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
      foreach (var methodInfo in methodInfos)
      {
        int i = 0;
        try
        {
          for (; i < executionCount; i++)
          {
            methodInfo.Invoke(Activator.CreateInstance(type), null);
          }
          System.Console.WriteLine(string.Format("\t{0} <-- Success({1})", methodInfo.Name, i));
        }
        catch (Exception ex)
        {
          System.Console.WriteLine(string.Format("\t{0} <-- Not executed({1}): Exception: {2}, Inner exception: {3}",
            methodInfo.Name,
            i,
            ex.Message,
            ex.InnerException != null ?
              ex.InnerException.Message :
              "N/A"));
        }
      }
    }
  }
}