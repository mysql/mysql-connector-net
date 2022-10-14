// Copyright (c) 2015, 2022, Oracle and/or its affiliates.
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

using MySqlX.Protocol.X;
using Mysqlx.Crud;
using Mysqlx.Datatypes;
using Mysqlx.Expr;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MySqlX.Data.Tests
{
  public class ExprParserTests
  {
    /// <summary>
    /// Check that a string doesn't parse.
    /// </summary>
    /// <param name="s"></param>
    private void CheckBadParse(string s)
    {
      //_testOutput.WriteLine(s);
      Assert.Throws<ArgumentException>(() => { Expr e = new ExprParser(s).Parse(); });
    }

    [Test]
    public void TestUnparseables()
    {
      CheckBadParse("1 + ");
      CheckBadParse("x 1,2,3)");
      CheckBadParse("x(1,2,3");
      CheckBadParse("x(1 2,3)");
      CheckBadParse("x(1,, 2,3)");
      CheckBadParse("x not y");
      CheckBadParse("x like");
      CheckBadParse("like");
      CheckBadParse("like x");
      CheckBadParse("x + interval 1 MACROsecond");
      CheckBadParse("x + interval 1 + 1");
      CheckBadParse("x * interval 1 hour");
      CheckBadParse("1.1.1");
      CheckBadParse("a$**");
      CheckBadParse("a.b.c.d > 1");
      CheckBadParse("a$[1.1]");
      CheckBadParse("a$[-1]");
      CheckBadParse("a$1");
      CheckBadParse("a$.1");
      CheckBadParse("a$a");
      CheckBadParse("a$.+");
      CheckBadParse("a$(x)");
      CheckBadParse("\"xyz");
      CheckBadParse("x between 1");
      CheckBadParse("x.1 > 1");
      CheckBadParse("x$ > 1");
      CheckBadParse(":>1");
      CheckBadParse(":1.1");
      CheckBadParse("cast(x as varchar)");
      CheckBadParse("not");
      CheckBadParse("$.a[-1]");
      // TODO: test bad JSON identifiers (quoting?)
    }

    /**
     * Check that a string parses and is reconstituted as a string that we expect. Futher we parse the canonical version to make sure it doesn't change.
     */
    private void CheckParseRoundTrip(string input, string expected)
    {
      if (expected == null)
      {
        expected = input;
      }
      Expr expr = new ExprParser(input).Parse();
      string canonicalized = ExprUnparser.ExprToString(expr);
      Assert.AreEqual(expected, canonicalized);

      // System.err.println("Canonicalized: " + canonicalized);
      Expr expr2 = new ExprParser(canonicalized).Parse();
      string recanonicalized = ExprUnparser.ExprToString(expr2);
      Assert.AreEqual(expected, recanonicalized);
    }

    /**
     * Test that expressions parsed and serialize back to the expected form.
     */
    [Test]
    public void TestRoundTrips()
    {
      //CheckParseRoundTrip("_id == 20", "($.pages > 20)");
      CheckParseRoundTrip("$.pages > 20", "($.pages > 20)");
      CheckParseRoundTrip("now () - interval '10:20' hour_MiNuTe", "date_sub(now(), \"10:20\", \"HOUR_MINUTE\")");
      CheckParseRoundTrip("now () - interval 1 hour - interval 2 minute - interval 3 second",
              "date_sub(date_sub(date_sub(now(), 1, \"HOUR\"), 2, \"MINUTE\"), 3, \"SECOND\")");
      // this needs parens around 1+1 in interval expression
      CheckParseRoundTrip("a + interval 1 hour + 1 + interval (1 + 1) second", "(date_add(a, 1, \"HOUR\") + date_add(1, (1 + 1), \"SECOND\"))");
      CheckParseRoundTrip("a + interval 1 hour + 1 + interval 1 * 1 second", "(date_add(a, 1, \"HOUR\") + date_add(1, (1 * 1), \"SECOND\"))");
      CheckParseRoundTrip("now () - interval -2 day", "date_sub(now(), -2, \"DAY\")"); // interval exprs compile to date_add/date_sub calls
      CheckParseRoundTrip("1", "1");
      CheckParseRoundTrip("1address", "1address");
      CheckParseRoundTrip("11employee", "11employee");
      CheckParseRoundTrip("1^0", "(1 ^ 0)");
      CheckParseRoundTrip("1e1", "10");
      CheckParseRoundTrip("-1e1", "-10");
      CheckParseRoundTrip("!0", "!0");
      CheckParseRoundTrip("1e4", "10000");
      CheckParseRoundTrip("12e-4", "0.0012");
      CheckParseRoundTrip("a + 314.1592e-2", "(a + 3.141592)");
      CheckParseRoundTrip("a + 0.0271e+2", "(a + 2.71)");
      CheckParseRoundTrip("a + 0.0271e2", "(a + 2.71)");
      CheckParseRoundTrip("10+1", "(10 + 1)");
      CheckParseRoundTrip("(abC == 1)", "(abC == 1)");
      CheckParseRoundTrip("(abC = 1)", "(abC == 1)");
      CheckParseRoundTrip("(Func(abc)==1)", "(Func(abc) == 1)");
      CheckParseRoundTrip("(abc == \"jess\")", "(abc == \"jess\")");
      CheckParseRoundTrip("(abc == \"with \\\"\")", "(abc == \"with \"\"\")"); // we escape with two internal quotes
      CheckParseRoundTrip("(abc != .10)", "(abc != 0.1)");
      CheckParseRoundTrip("(abc != \"xyz\")", "(abc != \"xyz\")");
      CheckParseRoundTrip("a + b * c + d", "((a + (b * c)) + d)"); // shows precedence and associativity
      CheckParseRoundTrip("a * b + c * d", "((a * b) + (c * d))");
      CheckParseRoundTrip("(a + b) * c + d", "(((a + b) * c) + d)");
      CheckParseRoundTrip("(field not in ('a',func('b', 2.0),'c'))", "field not in(\"a\", func(\"b\", 2), \"c\")");
      CheckParseRoundTrip("jess.age beTwEEn 30 and death", "(jess.age between 30 AND death)");
      CheckParseRoundTrip("jess.age not BeTweeN 30 and death", "(jess.age not between 30 AND death)");
      CheckParseRoundTrip("a + b * c + d", "((a + (b * c)) + d)");
      CheckParseRoundTrip("x > 10 and Y >= -20", "((x > 10) && (Y >= -20))");
      CheckParseRoundTrip("a is true and b is null and C + 1 > 40 and (thetime == now() or hungry())",
              "((((a is TRUE) && (b is NULL)) && ((C + 1) > 40)) && ((thetime == now()) || hungry()))");
      CheckParseRoundTrip("a + b + -c > 2", "(((a + b) + -c) > 2)");
      CheckParseRoundTrip("now () + b + c > 2", "(((now() + b) + c) > 2)");
      CheckParseRoundTrip("now () + $.b + c > 2", "(((now() + $.b) + c) > 2)");
      CheckParseRoundTrip("now () - interval +2 day > some_other_time() or something_else IS NOT NULL",
              "((date_sub(now(), 2, \"DAY\") > some_other_time()) || is_not(something_else, NULL))");
      CheckParseRoundTrip("\"two quotes to one\"\"\"", null);
      CheckParseRoundTrip("'two quotes to one'''", "\"two quotes to one'\"");
      CheckParseRoundTrip("'different quote \"'", "\"different quote \"\"\"");
      CheckParseRoundTrip("\"different quote '\"", "\"different quote '\"");
      CheckParseRoundTrip("`ident`", "ident"); // doesn't need quoting
      CheckParseRoundTrip("`ident```", "`ident```");
      CheckParseRoundTrip("`ident\"'`", "`ident\"'`");
      CheckParseRoundTrip(":0 > x and func(:3, :2, :1)", "((:0 > x) && func(:1, :2, :3))"); // serialized in order of position (needs mapped externally)
      CheckParseRoundTrip("a > now() + interval (2 + x) MiNuTe", "(a > date_add(now(), (2 + x), \"MINUTE\"))");
      CheckParseRoundTrip("a between 1 and 2", "(a between 1 AND 2)");
      CheckParseRoundTrip("a not between 1 and 2", "(a not between 1 AND 2)");
      CheckParseRoundTrip("a in (1,2,a.b(3),4,5,x)", "a in(1, 2, a.b(3), 4, 5, x)");
      CheckParseRoundTrip("a not in (1,2,3,4,5,$.x)", "a not in(1, 2, 3, 4, 5, $.x)");
      CheckParseRoundTrip("a like b escape c", "a like b ESCAPE c");
      CheckParseRoundTrip("a not like b escape c", "a not like b ESCAPE c");
      CheckParseRoundTrip("(1 + 3) in (3, 4, 5)", "(1 + 3) in(3, 4, 5)");
      CheckParseRoundTrip("`a crazy \"function\"``'name'`(1 + 3) in (3, 4, 5)", "`a crazy \"function\"``'name'`((1 + 3)) in(3, 4, 5)");
      CheckParseRoundTrip("a$.b", "a$.b");
      CheckParseRoundTrip("a$.\"bcd\"", "a$.bcd");
      CheckParseRoundTrip("a$.*", "a$.*");
      CheckParseRoundTrip("a$[0].*", "a$[0].*");
      CheckParseRoundTrip("a$[*].*", "a$[*].*");
      CheckParseRoundTrip("a$**[0].*", "a$**[0].*");
      CheckParseRoundTrip("$._id", "$._id");
      CheckParseRoundTrip("$._id == :0", "($._id == :0)");
      CheckParseRoundTrip("'Monty!' REGEXP '.*'", "(\"Monty!\" regexp \".*\")");
      CheckParseRoundTrip("a regexp b regexp c", "((a regexp b) regexp c)");
      CheckParseRoundTrip("a + b + c", "((a + b) + c)");
      CheckParseRoundTrip("a + cast(b as json)", "(a + cast(b AS JSON))");
      CheckParseRoundTrip("a + cast(b as decimal)", "(a + cast(b AS DECIMAL))");
      CheckParseRoundTrip("a + cast(b as decimal(2))", "(a + cast(b AS DECIMAL(2)))");
      CheckParseRoundTrip("a + cast(b as decimal(1, 2))", "(a + cast(b AS DECIMAL(1,2)))");
      CheckParseRoundTrip("a + cast(b as binary)", "(a + cast(b AS BINARY))");
      CheckParseRoundTrip("a + cast(b as DaTe)", "(a + cast(b AS DATE))");
      CheckParseRoundTrip("a + cast(b as char)", "(a + cast(b AS CHAR))");
      CheckParseRoundTrip("a + cast(b as DaTeTiMe)", "(a + cast(b AS DATETIME))");
      CheckParseRoundTrip("a + cast(b as time)", "(a + cast(b AS TIME))");
      CheckParseRoundTrip("a + cast(b as binary(3))", "(a + cast(b AS BINARY(3)))");
      CheckParseRoundTrip("a + cast(b as unsigned)", "(a + cast(b AS UNSIGNED))");
      CheckParseRoundTrip("a + cast(b as unsigned integer)", "(a + cast(b AS UNSIGNED))");
      CheckParseRoundTrip("a is true or a is false", "((a is TRUE) || (a is FALSE))");
      // TODO: this isn't serialized correctly by the unparser
      //checkParseRoundTrip("a$.b[0][0].c**.d.\"a weird\\\"key name\"", "");
      CheckParseRoundTrip("*", "*");
      CheckParseRoundTrip("count(*) + 1", "(count(*) + 1)");
    }

    /**
     * Explicit test inspecting the expression tree.
     */
    [Test]
    public void TestExprTree()
    {
      Expr expr = new ExprParser("a like 'xyz' and $.count > 10 + 1").Parse();
      Assert.AreEqual(Expr.Types.Type.Operator, expr.Type);
      Assert.AreEqual("&&", expr.Operator.Name);
      Assert.AreEqual(2, expr.Operator.Param.Count);

      // check left side of AND: (a like 'xyz')
      Expr andLeft = expr.Operator.Param[0];
      Assert.AreEqual(Expr.Types.Type.Operator, andLeft.Type);
      Assert.AreEqual("like", andLeft.Operator.Name);
      Assert.AreEqual(2, andLeft.Operator.Param.Count);
      Expr identA = andLeft.Operator.Param[0];
      Assert.AreEqual(Expr.Types.Type.Ident, identA.Type);
      Assert.AreEqual("a", identA.Identifier.Name);
      Expr literalXyz = andLeft.Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Literal, literalXyz.Type);
      Scalar scalarXyz = literalXyz.Literal;
      Assert.AreEqual(Scalar.Types.Type.VString, scalarXyz.Type);
      Assert.AreEqual("xyz", scalarXyz.VString.Value.ToStringUtf8());

      // check right side of AND: ($.count > 10 + 1)
      Expr andRight = expr.Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Operator, andRight.Type);
      Assert.AreEqual(">", andRight.Operator.Name);
      Assert.AreEqual(2, andRight.Operator.Param.Count);
      Expr countDocPath = andRight.Operator.Param[0];
      Assert.AreEqual(Expr.Types.Type.Ident, countDocPath.Type);
      ColumnIdentifier countId = countDocPath.Identifier;
      Assert.AreEqual(string.Empty, countId.Name);
      Assert.AreEqual(string.Empty, countId.TableName);
      Assert.AreEqual(string.Empty, countId.SchemaName);
      Assert.That(countId.DocumentPath.Count==1);
      Assert.AreEqual(DocumentPathItem.Types.Type.Member, countId.DocumentPath[0].Type);
      Assert.AreEqual("count", countId.DocumentPath[0].Value);
      Expr addition = andRight.Operator.Param[1];
      Scalar addLeftScalar = addition.Operator.Param[0].Literal;
      Scalar addRightScalar = addition.Operator.Param[1].Literal;
      Assert.AreEqual(Expr.Types.Type.Operator, addition.Type);
      Assert.AreEqual("+", addition.Operator.Name);
      Assert.AreEqual(2, addition.Operator.Param.Count);
      Assert.AreEqual(Expr.Types.Type.Literal, addition.Operator.Param[0].Type);
      Assert.AreEqual(Expr.Types.Type.Literal, addition.Operator.Param[1].Type);
      Assert.AreEqual(Scalar.Types.Type.VSint, addLeftScalar.Type);
      Assert.AreEqual(Scalar.Types.Type.VSint, addRightScalar.Type);
      Assert.AreEqual(10, addLeftScalar.VSignedInt);
      Assert.AreEqual(1, addRightScalar.VSignedInt);
    }

    [Test]
    public void TestOrderByParserBasic()
    {
      List<Order> orderSpec = new ExprParser("a, b desc").ParseOrderSpec();
      Assert.AreEqual(2, orderSpec.Count);
      Order o1 = orderSpec[0];
      Assert.False(o1.HasDirection);
      Assert.AreEqual("a", ExprUnparser.ExprToString(o1.Expr));
      Order o2 = orderSpec[1];
      Assert.True(o2.HasDirection);
      Assert.AreEqual(Order.Types.Direction.Desc, o2.Direction);
      Assert.AreEqual("b", ExprUnparser.ExprToString(o2.Expr));
    }

    [Test]
    public void TestOrderByParserComplexExpressions()
    {
      List<Order> orderSpec = new ExprParser("field not in ('a',func('b', 2.0),'c') desc, 1-a$**[0].*, now () + $.b + c > 2 asc").ParseOrderSpec();
      Assert.AreEqual(3, orderSpec.Count);
      Order o1 = orderSpec[0];
      Assert.True(o1.HasDirection);
      Assert.AreEqual(Order.Types.Direction.Desc, o1.Direction);
      Assert.AreEqual("field not in(\"a\", func(\"b\", 2), \"c\")", ExprUnparser.ExprToString(o1.Expr));
      Order o2 = orderSpec[1];
      Assert.False(o2.HasDirection);
      Assert.AreEqual("(1 - a$**[0].*)", ExprUnparser.ExprToString(o2.Expr));
      Order o3 = orderSpec[2];
      Assert.True(o3.HasDirection);
      Assert.AreEqual(Order.Types.Direction.Asc, o3.Direction);
      Assert.AreEqual("(((now() + $.b) + c) > 2)", ExprUnparser.ExprToString(o3.Expr));
    }

    [Test]
    public void TestNamedPlaceholders()
    {
      ExprParser parser = new ExprParser("a = :a and b = :b and (c = 'x' or d = :b)");
      Assert.AreEqual("IDENT(a)", parser.tokens[0].ToString());
      Assert.AreEqual("EQ", parser.tokens[1].ToString());
      Expr e = parser.Parse();
      Assert.AreEqual(0, parser.placeholderNameToPosition["a"]);
      Assert.AreEqual(1, parser.placeholderNameToPosition["b"]);
      Assert.AreEqual(2, parser.positionalPlaceholderCount);

      Expr aEqualsPlaceholder = e.Operator.Param[0].Operator.Param[0].Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Placeholder, aEqualsPlaceholder.Type);
      Assert.AreEqual((uint)0, aEqualsPlaceholder.Position);
      Expr bEqualsPlaceholder = e.Operator.Param[0].Operator.Param[1].Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Placeholder, bEqualsPlaceholder.Type);
      Assert.AreEqual((uint)1, bEqualsPlaceholder.Position);
      Expr dEqualsPlaceholder = e.Operator.Param[1].Operator.Param[1].Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Placeholder, dEqualsPlaceholder.Type);
      Assert.AreEqual((uint)1, dEqualsPlaceholder.Position);
    }

    [Test]
    public void TestNumberedPlaceholders()
    {
      ExprParser parser = new ExprParser("a == :1 and b == :3 and (c == :2 or d == :2)");
      Expr e = parser.Parse();
      Assert.AreEqual(0, parser.placeholderNameToPosition["1"]);
      Assert.AreEqual(1, parser.placeholderNameToPosition["3"]);
      Assert.AreEqual(2, parser.placeholderNameToPosition["2"]);
      Assert.AreEqual(3, parser.positionalPlaceholderCount);

      Expr aEqualsPlaceholder = e.Operator.Param[0].Operator.Param[0].Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Placeholder, aEqualsPlaceholder.Type);
      Assert.AreEqual((uint)0, aEqualsPlaceholder.Position);
      Expr bEqualsPlaceholder = e.Operator.Param[0].Operator.Param[1].Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Placeholder, bEqualsPlaceholder.Type);
      Assert.AreEqual((uint)1, bEqualsPlaceholder.Position);
      Expr cEqualsPlaceholder = e.Operator.Param[1].Operator.Param[0].Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Placeholder, cEqualsPlaceholder.Type);
      Assert.AreEqual((uint)2, cEqualsPlaceholder.Position);
      Expr dEqualsPlaceholder = e.Operator.Param[1].Operator.Param[1].Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Placeholder, dEqualsPlaceholder.Type);
      Assert.AreEqual((uint)2, dEqualsPlaceholder.Position);
    }

    [Test]
    public void TestUnnumberedPlaceholders()
    {
      ExprParser parser = new ExprParser("a = ? and b = ? and (c = 'x' or d = ?)");
      Expr e = parser.Parse();
      Assert.AreEqual(0, parser.placeholderNameToPosition["0"]);
      Assert.AreEqual(1, parser.placeholderNameToPosition["1"]);
      Assert.AreEqual(2, parser.placeholderNameToPosition["2"]);
      Assert.AreEqual(3, parser.positionalPlaceholderCount);

      Expr aEqualsPlaceholder = e.Operator.Param[0].Operator.Param[0].Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Placeholder, aEqualsPlaceholder.Type);
      Assert.AreEqual((uint)0, aEqualsPlaceholder.Position);
      Expr bEqualsPlaceholder = e.Operator.Param[0].Operator.Param[1].Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Placeholder, bEqualsPlaceholder.Type);
      Assert.AreEqual((uint)1, bEqualsPlaceholder.Position);
      Expr dEqualsPlaceholder = e.Operator.Param[1].Operator.Param[1].Operator.Param[1];
      Assert.AreEqual(Expr.Types.Type.Placeholder, dEqualsPlaceholder.Type);
      Assert.AreEqual((uint)2, dEqualsPlaceholder.Position);
    }

    [Test]
    public void TestJsonLiteral()
    {
      Expr e = new ExprParser("{'a':1, 'b':\"a string\"}").Parse();

      Assert.AreEqual("{'a':1, 'b':\"a string\"}", ExprUnparser.ExprToString(e));

      Assert.AreEqual(Expr.Types.Type.Object, e.Type);
      Mysqlx.Expr.Object o = e.Object;
      Assert.AreEqual(2, o.Fld.Count);
      Mysqlx.Expr.Object.Types.ObjectField of;

      of = o.Fld[0];
      Assert.AreEqual("a", of.Key);
      e = of.Value;
      Assert.AreEqual(Expr.Types.Type.Literal, e.Type);
      Assert.AreEqual(1, e.Literal.VSignedInt);

      of = o.Fld[1];
      Assert.AreEqual("b", of.Key);
      e = of.Value;
      Assert.AreEqual(Expr.Types.Type.Literal, e.Type);
      Assert.AreEqual("a string", e.Literal.VString.Value.ToStringUtf8());
    }

    [Test]
    public void TestTrivialDocumentProjection()
    {
      List<Projection> proj;

      proj = new ExprParser("$.a as a").ParseDocumentProjection();
      Assert.That(proj, Has.One.Items);
      Assert.AreNotEqual(string.Empty, proj[0].Alias);
      Assert.AreEqual("a", proj[0].Alias);

      proj = new ExprParser("$.a as a, $.b as b, $.c as c").ParseDocumentProjection();
    }

    [Test]
    public void TestExprAsPathDocumentProjection()
    {
      List<Projection> projList = new ExprParser("$.a as b, (1 + 1) * 100 as x, 2 as j42").ParseDocumentProjection();

      Assert.AreEqual(3, projList.Count);

      // check $.a as b
      Projection proj = projList[0];
      IList<DocumentPathItem> paths = proj.Source.Identifier.DocumentPath;
      Assert.AreEqual(1, paths.Count);
      Assert.AreEqual(DocumentPathItem.Types.Type.Member, paths[0].Type);
      Assert.AreEqual("a", paths[0].Value);

      Assert.AreEqual("b", proj.Alias);

      // check (1 + 1) * 100 as x
      proj = projList[1];
      Assert.AreEqual("((1 + 1) * 100)", ExprUnparser.ExprToString(proj.Source));
      Assert.AreEqual("x", proj.Alias);

      // check 2 as j42
      proj = projList[2];
      Assert.AreEqual("2", ExprUnparser.ExprToString(proj.Source));
      Assert.AreEqual("j42", proj.Alias);
    }

    [Test]
    public void TestJsonConstructorAsDocumentProjection()
    {
      // same as we use in find().field("{...}")
      string projString = "{'a':'value for a', 'b':1+1, 'c'::bindvar, 'd':$.member[22], 'e':{'nested':'doc'}}";
      Projection proj = new Projection();
      proj.Source = new ExprParser(projString, false).Parse();
      Assert.AreEqual(Expr.Types.Type.Object, proj.Source.Type);

      IEnumerator<Mysqlx.Expr.Object.Types.ObjectField> fields = proj.Source.Object.Fld.GetEnumerator();
      string[][] array = new string[][] {
                    new string[] {"a", "\"value for a\""},
                    new string[] {"b", "(1 + 1)"},
                    new string[] {"c", ":0"},
                    new string[] {"d", "$.member[22]"},
                    new string[] {"e", "{'nested':\"doc\"}"}};
      array.ToList().ForEach(pair =>
      {
        fields.MoveNext();
        Mysqlx.Expr.Object.Types.ObjectField f = fields.Current;
        Assert.AreEqual(pair[0], f.Key);
        Assert.AreEqual(pair[1], ExprUnparser.ExprToString(f.Value));
      });
      Assert.False(fields.MoveNext());
    }

    [Test]
    public void TestJsonExprsInDocumentProjection()
    {
      // this is not a single doc as the project but multiple docs as embedded fields
      string projString = "{'a':1} as a, {'b':2} as b";
      List<Projection> projList = new ExprParser(projString).ParseDocumentProjection();
      Assert.AreEqual(2, projList.Count);
      // TODO: verification of remaining elements
    }

    [Test]
    public void TestTableInsertProjection()
    {
      Column col = new ExprParser("a").ParseTableInsertField();
      Assert.AreEqual("a", col.Name);

      col = new ExprParser("`double weird `` string`").ParseTableInsertField();
      Assert.AreEqual("double weird ` string", col.Name);
    }

    [Test]
    public void TestTableUpdateField()
    {
      ColumnIdentifier col;
      col = new ExprParser("a").ParseTableUpdateField();
      Assert.AreEqual("a", col.Name);

      col = new ExprParser("b.c").ParseTableUpdateField();
      Assert.AreEqual("b", col.TableName);
      Assert.AreEqual("c", col.Name);

      col = new ExprParser("d.e$.the_path[2]").ParseTableUpdateField();
      Assert.AreEqual("d", col.TableName);
      Assert.AreEqual("e", col.Name);
      Assert.AreEqual(2, col.DocumentPath.Count);
      Assert.AreEqual("the_path", col.DocumentPath[0].Value);
      Assert.AreEqual((uint)2, col.DocumentPath[1].Index);

      col = new ExprParser("`zzz\\``").ParseTableUpdateField();
      Assert.AreEqual("zzz`", col.Name);
    }

    [Test]
    public void TestTrivialTableSelectProjection()
    {
      List<Projection> proj = new ExprParser("a, b as c").ParseTableSelectProjection();
      Assert.AreEqual(2, proj.Count);
      Assert.AreEqual("a", ExprUnparser.ExprToString(proj[0].Source));
      Assert.AreEqual(string.Empty, proj[0].Alias);
      Assert.AreEqual("b", ExprUnparser.ExprToString(proj[1].Source));
      Assert.AreNotEqual(string.Empty, proj[1].Alias);
      Assert.AreEqual("c", proj[1].Alias);
    }

    [Test]
    public void TestStarTableSelectProjection()
    {
      List<Projection> proj = new ExprParser("*, b as c").ParseTableSelectProjection();
      Assert.AreEqual(2, proj.Count);
      Assert.AreEqual("*", ExprUnparser.ExprToString(proj[0].Source));
      Assert.AreEqual(string.Empty, proj[0].Alias);
      Assert.AreEqual("b", ExprUnparser.ExprToString(proj[1].Source));
      Assert.AreNotEqual(string.Empty, proj[1].Alias);
      Assert.AreEqual("c", proj[1].Alias);
    }

    [Test]
    public void TestComplexTableSelectProjection()
    {
      string projectionString = "(1 + 1) * 100 as `one-o-two`, 'a is \\'a\\'' as `what is 'a'`";
      List<Projection> proj = new ExprParser(projectionString).ParseTableSelectProjection();
      Assert.AreEqual(2, proj.Count);

      Assert.AreEqual("((1 + 1) * 100)", ExprUnparser.ExprToString(proj[0].Source));
      Assert.AreEqual("one-o-two", proj[0].Alias);

      Assert.AreEqual("a is 'a'", proj[1].Source.Literal.VString.Value.ToStringUtf8());
      Assert.AreEqual("what is 'a'", proj[1].Alias);
    }

    [Test]
    public void TestRandom()
    {
      // tests generated by the random expression generator
      CheckParseRoundTrip("x - INTERVAL { } DAY_HOUR * { } + { }", "((date_sub(x, {}, \"DAY_HOUR\") * {}) + {})");
      CheckParseRoundTrip("NULL - INTERVAL $ ** [ 89 ] << { '' : { } - $ . V << { '' : { } + { } REGEXP ? << { } - { } < { } | { } << { '' : : 8 + : 26 ^ { } } + { } >> { } } || { } } & { } SECOND", "date_sub(NULL, (($**[89] << {'':((({} - $.V) << {'':(({} + {}) regexp ((:0 << ({} - {})) < ({} | (({} << ({'':((:1 + :2) ^ {})} + {})) >> {}))))}) || {})}) & {}), \"SECOND\")");
      // TODO: check the validity of this:
      // checkParseRoundTrip("_XJl . F ( `ho` @ [*] [*] - ~ ! { '' : { } LIKE { } && : rkc & 1 & y @ ** . d [*] [*] || { } ^ { } REGEXP { } } || { } - { } ^ { } < { } IN ( ) >= { } IN ( ) )", "");
    }

    [Test]
    public void UnqualifiedDocPaths()
    {
      Expr expr = new ExprParser("1 + b[0]", false).Parse();
      Assert.AreEqual("(1 + $.b[0])", ExprUnparser.ExprToString(expr));
      expr = new ExprParser("a.*", false).Parse();
      Assert.AreEqual("$.a.*", ExprUnparser.ExprToString(expr));
      expr = new ExprParser("bL . vT .*", false).Parse();
      Assert.AreEqual("$.bL.vT.*", ExprUnparser.ExprToString(expr));
      expr = new ExprParser("dd ** .X", false).Parse();
      Assert.AreEqual("$.dd**.X", ExprUnparser.ExprToString(expr));
    }

    [TestCase("info$.additionalinfo.hobbies", "info$.additionalinfo.hobbies", true)]
    [TestCase("info->$.additionalinfo.hobbies", "info$.additionalinfo.hobbies", true)]
    [TestCase("info->>$.additionalinfo.hobbies", null, true)]
    [TestCase("info$.additionalinfo.hobbies", null, false)]
    [TestCase("info->$.additionalinfo.hobbies", null, false)]
    [TestCase("info->>$.additionalinfo.hobbies", null, false)]
    [TestCase("$.additionalinfo.hobbies", "$.additionalinfo.hobbies", false)]
    public void JsonColumnPath(string exprString, string unparserString, bool isRelational)
    {
      if(unparserString == null)
      {
        Assert.AreEqual($"Unable to parse query '{exprString}'", 
          Assert.Throws<ArgumentException>(() => new ExprParser(exprString, isRelational).Parse()).Message);
      }
      else
      {
        Expr expr = new ExprParser(exprString, isRelational).Parse();
        Assert.AreEqual(unparserString, ExprUnparser.ExprToString(expr));
      }
    }
  }
}
