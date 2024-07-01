// Copyright © 2015, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using NUnit.Framework.Legacy;

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
      Assert.That(canonicalized, Is.EqualTo(expected));

      // System.err.println("Canonicalized: " + canonicalized);
      Expr expr2 = new ExprParser(canonicalized).Parse();
      string recanonicalized = ExprUnparser.ExprToString(expr2);
      Assert.That(recanonicalized, Is.EqualTo(expected));
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
      Assert.That(expr.Type, Is.EqualTo(Expr.Types.Type.Operator));
      Assert.That(expr.Operator.Name, Is.EqualTo("&&"));
      Assert.That(expr.Operator.Param.Count, Is.EqualTo(2));

      // check left side of AND: (a like 'xyz')
      Expr andLeft = expr.Operator.Param[0];
      Assert.That(andLeft.Type, Is.EqualTo(Expr.Types.Type.Operator));
      Assert.That(andLeft.Operator.Name, Is.EqualTo("like"));
      Assert.That(andLeft.Operator.Param.Count, Is.EqualTo(2));
      Expr identA = andLeft.Operator.Param[0];
      Assert.That(identA.Type, Is.EqualTo(Expr.Types.Type.Ident));
      Assert.That(identA.Identifier.Name, Is.EqualTo("a"));
      Expr literalXyz = andLeft.Operator.Param[1];
      Assert.That(literalXyz.Type, Is.EqualTo(Expr.Types.Type.Literal));
      Scalar scalarXyz = literalXyz.Literal;
      Assert.That(scalarXyz.Type, Is.EqualTo(Scalar.Types.Type.VString));
      Assert.That(scalarXyz.VString.Value.ToStringUtf8(), Is.EqualTo("xyz"));

      // check right side of AND: ($.count > 10 + 1)
      Expr andRight = expr.Operator.Param[1];
      Assert.That(andRight.Type, Is.EqualTo(Expr.Types.Type.Operator));
      Assert.That(andRight.Operator.Name, Is.EqualTo(">"));
      Assert.That(andRight.Operator.Param.Count, Is.EqualTo(2));
      Expr countDocPath = andRight.Operator.Param[0];
      Assert.That(countDocPath.Type, Is.EqualTo(Expr.Types.Type.Ident));
      ColumnIdentifier countId = countDocPath.Identifier;
      Assert.That(countId.Name, Is.EqualTo(string.Empty));
      Assert.That(countId.TableName, Is.EqualTo(string.Empty));
      Assert.That(countId.SchemaName, Is.EqualTo(string.Empty));
      Assert.That(countId.DocumentPath.Count==1);
      Assert.That(countId.DocumentPath[0].Type, Is.EqualTo(DocumentPathItem.Types.Type.Member));
      Assert.That(countId.DocumentPath[0].Value, Is.EqualTo("count"));
      Expr addition = andRight.Operator.Param[1];
      Scalar addLeftScalar = addition.Operator.Param[0].Literal;
      Scalar addRightScalar = addition.Operator.Param[1].Literal;
      Assert.That(addition.Type, Is.EqualTo(Expr.Types.Type.Operator));
      Assert.That(addition.Operator.Name, Is.EqualTo("+"));
      Assert.That(addition.Operator.Param.Count, Is.EqualTo(2));
      Assert.That(addition.Operator.Param[0].Type, Is.EqualTo(Expr.Types.Type.Literal));
      Assert.That(addition.Operator.Param[1].Type, Is.EqualTo(Expr.Types.Type.Literal));
      Assert.That(addLeftScalar.Type, Is.EqualTo(Scalar.Types.Type.VSint));
      Assert.That(addRightScalar.Type, Is.EqualTo(Scalar.Types.Type.VSint));
      Assert.That(addLeftScalar.VSignedInt, Is.EqualTo(10));
      Assert.That(addRightScalar.VSignedInt, Is.EqualTo(1));
    }

    [Test]
    public void TestOrderByParserBasic()
    {
      List<Order> orderSpec = new ExprParser("a, b desc").ParseOrderSpec();
      Assert.That(orderSpec.Count, Is.EqualTo(2));
      Order o1 = orderSpec[0];
      Assert.That(o1.HasDirection, Is.False);
      Assert.That(ExprUnparser.ExprToString(o1.Expr), Is.EqualTo("a"));
      Order o2 = orderSpec[1];
      Assert.That(o2.HasDirection);
      Assert.That(o2.Direction, Is.EqualTo(Order.Types.Direction.Desc));
      Assert.That(ExprUnparser.ExprToString(o2.Expr), Is.EqualTo("b"));
    }

    [Test]
    public void TestOrderByParserComplexExpressions()
    {
      List<Order> orderSpec = new ExprParser("field not in ('a',func('b', 2.0),'c') desc, 1-a$**[0].*, now () + $.b + c > 2 asc").ParseOrderSpec();
      Assert.That(orderSpec.Count, Is.EqualTo(3));
      Order o1 = orderSpec[0];
      Assert.That(o1.HasDirection);
      Assert.That(o1.Direction, Is.EqualTo(Order.Types.Direction.Desc));
      Assert.That(ExprUnparser.ExprToString(o1.Expr), Is.EqualTo("field not in(\"a\", func(\"b\", 2), \"c\")"));
      Order o2 = orderSpec[1];
      Assert.That(o2.HasDirection, Is.False);
      Assert.That(ExprUnparser.ExprToString(o2.Expr), Is.EqualTo("(1 - a$**[0].*)"));
      Order o3 = orderSpec[2];
      Assert.That(o3.HasDirection);
      Assert.That(o3.Direction, Is.EqualTo(Order.Types.Direction.Asc));
      Assert.That(ExprUnparser.ExprToString(o3.Expr), Is.EqualTo("(((now() + $.b) + c) > 2)"));
    }

    [Test]
    public void TestNamedPlaceholders()
    {
      ExprParser parser = new ExprParser("a = :a and b = :b and (c = 'x' or d = :b)");
      Assert.That(parser.tokens[0].ToString(), Is.EqualTo("IDENT(a)"));
      Assert.That(parser.tokens[1].ToString(), Is.EqualTo("EQ"));
      Expr e = parser.Parse();
      Assert.That(parser.placeholderNameToPosition["a"], Is.EqualTo(0));
      Assert.That(parser.placeholderNameToPosition["b"], Is.EqualTo(1));
      Assert.That(parser.positionalPlaceholderCount, Is.EqualTo(2));

      Expr aEqualsPlaceholder = e.Operator.Param[0].Operator.Param[0].Operator.Param[1];
      Assert.That(aEqualsPlaceholder.Type, Is.EqualTo(Expr.Types.Type.Placeholder));
      Assert.That(aEqualsPlaceholder.Position, Is.EqualTo((uint)0));
      Expr bEqualsPlaceholder = e.Operator.Param[0].Operator.Param[1].Operator.Param[1];
      Assert.That(bEqualsPlaceholder.Type, Is.EqualTo(Expr.Types.Type.Placeholder));
      Assert.That(bEqualsPlaceholder.Position, Is.EqualTo((uint)1));
      Expr dEqualsPlaceholder = e.Operator.Param[1].Operator.Param[1].Operator.Param[1];
      Assert.That(dEqualsPlaceholder.Type, Is.EqualTo(Expr.Types.Type.Placeholder));
      Assert.That(dEqualsPlaceholder.Position, Is.EqualTo((uint)1));
    }

    [Test]
    public void TestNumberedPlaceholders()
    {
      ExprParser parser = new ExprParser("a == :1 and b == :3 and (c == :2 or d == :2)");
      Expr e = parser.Parse();
      Assert.That(parser.placeholderNameToPosition["1"], Is.EqualTo(0));
      Assert.That(parser.placeholderNameToPosition["3"], Is.EqualTo(1));
      Assert.That(parser.placeholderNameToPosition["2"], Is.EqualTo(2));
      Assert.That(parser.positionalPlaceholderCount, Is.EqualTo(3));

      Expr aEqualsPlaceholder = e.Operator.Param[0].Operator.Param[0].Operator.Param[1];
      Assert.That(aEqualsPlaceholder.Type, Is.EqualTo(Expr.Types.Type.Placeholder));
      Assert.That(aEqualsPlaceholder.Position, Is.EqualTo((uint)0));
      Expr bEqualsPlaceholder = e.Operator.Param[0].Operator.Param[1].Operator.Param[1];
      Assert.That(bEqualsPlaceholder.Type, Is.EqualTo(Expr.Types.Type.Placeholder));
      Assert.That(bEqualsPlaceholder.Position, Is.EqualTo((uint)1));
      Expr cEqualsPlaceholder = e.Operator.Param[1].Operator.Param[0].Operator.Param[1];
      Assert.That(cEqualsPlaceholder.Type, Is.EqualTo(Expr.Types.Type.Placeholder));
      Assert.That(cEqualsPlaceholder.Position, Is.EqualTo((uint)2));
      Expr dEqualsPlaceholder = e.Operator.Param[1].Operator.Param[1].Operator.Param[1];
      Assert.That(dEqualsPlaceholder.Type, Is.EqualTo(Expr.Types.Type.Placeholder));
      Assert.That(dEqualsPlaceholder.Position, Is.EqualTo((uint)2));
    }

    [Test]
    public void TestUnnumberedPlaceholders()
    {
      ExprParser parser = new ExprParser("a = ? and b = ? and (c = 'x' or d = ?)");
      Expr e = parser.Parse();
      Assert.That(parser.placeholderNameToPosition["0"], Is.EqualTo(0));
      Assert.That(parser.placeholderNameToPosition["1"], Is.EqualTo(1));
      Assert.That(parser.placeholderNameToPosition["2"], Is.EqualTo(2));
      Assert.That(parser.positionalPlaceholderCount, Is.EqualTo(3));

      Expr aEqualsPlaceholder = e.Operator.Param[0].Operator.Param[0].Operator.Param[1];
      Assert.That(aEqualsPlaceholder.Type, Is.EqualTo(Expr.Types.Type.Placeholder));
      Assert.That(aEqualsPlaceholder.Position, Is.EqualTo((uint)0));
      Expr bEqualsPlaceholder = e.Operator.Param[0].Operator.Param[1].Operator.Param[1];
      Assert.That(bEqualsPlaceholder.Type, Is.EqualTo(Expr.Types.Type.Placeholder));
      Assert.That(bEqualsPlaceholder.Position, Is.EqualTo((uint)1));
      Expr dEqualsPlaceholder = e.Operator.Param[1].Operator.Param[1].Operator.Param[1];
      Assert.That(dEqualsPlaceholder.Type, Is.EqualTo(Expr.Types.Type.Placeholder));
      Assert.That(dEqualsPlaceholder.Position, Is.EqualTo((uint)2));
    }

    [Test]
    public void TestJsonLiteral()
    {
      Expr e = new ExprParser("{'a':1, 'b':\"a string\"}").Parse();

      Assert.That(ExprUnparser.ExprToString(e), Is.EqualTo("{'a':1, 'b':\"a string\"}"));

      Assert.That(e.Type, Is.EqualTo(Expr.Types.Type.Object));
      Mysqlx.Expr.Object o = e.Object;
      Assert.That(o.Fld.Count, Is.EqualTo(2));
      Mysqlx.Expr.Object.Types.ObjectField of;

      of = o.Fld[0];
      Assert.That(of.Key, Is.EqualTo("a"));
      e = of.Value;
      Assert.That(e.Type, Is.EqualTo(Expr.Types.Type.Literal));
      Assert.That(e.Literal.VSignedInt, Is.EqualTo(1));

      of = o.Fld[1];
      Assert.That(of.Key, Is.EqualTo("b"));
      e = of.Value;
      Assert.That(e.Type, Is.EqualTo(Expr.Types.Type.Literal));
      Assert.That(e.Literal.VString.Value.ToStringUtf8(), Is.EqualTo("a string"));
    }

    [Test]
    public void TestTrivialDocumentProjection()
    {
      List<Projection> proj;

      proj = new ExprParser("$.a as a").ParseDocumentProjection();
      Assert.That(proj, Has.One.Items);
      Assert.That(proj[0].Alias, Is.Not.EqualTo(string.Empty));
      Assert.That(proj[0].Alias, Is.EqualTo("a"));

      proj = new ExprParser("$.a as a, $.b as b, $.c as c").ParseDocumentProjection();
    }

    [Test]
    public void TestExprAsPathDocumentProjection()
    {
      List<Projection> projList = new ExprParser("$.a as b, (1 + 1) * 100 as x, 2 as j42").ParseDocumentProjection();

      Assert.That(projList.Count, Is.EqualTo(3));

      // check $.a as b
      Projection proj = projList[0];
      IList<DocumentPathItem> paths = proj.Source.Identifier.DocumentPath;
      Assert.That(paths.Count, Is.EqualTo(1));
      Assert.That(paths[0].Type, Is.EqualTo(DocumentPathItem.Types.Type.Member));
      Assert.That(paths[0].Value, Is.EqualTo("a"));

      Assert.That(proj.Alias, Is.EqualTo("b"));

      // check (1 + 1) * 100 as x
      proj = projList[1];
      Assert.That(ExprUnparser.ExprToString(proj.Source), Is.EqualTo("((1 + 1) * 100)"));
      Assert.That(proj.Alias, Is.EqualTo("x"));

      // check 2 as j42
      proj = projList[2];
      Assert.That(ExprUnparser.ExprToString(proj.Source), Is.EqualTo("2"));
      Assert.That(proj.Alias, Is.EqualTo("j42"));
    }

    [Test]
    public void TestJsonConstructorAsDocumentProjection()
    {
      // same as we use in find().field("{...}")
      string projString = "{'a':'value for a', 'b':1+1, 'c'::bindvar, 'd':$.member[22], 'e':{'nested':'doc'}}";
      Projection proj = new Projection();
      proj.Source = new ExprParser(projString, false).Parse();
      Assert.That(proj.Source.Type, Is.EqualTo(Expr.Types.Type.Object));

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
        Assert.That(f.Key, Is.EqualTo(pair[0]));
        Assert.That(ExprUnparser.ExprToString(f.Value), Is.EqualTo(pair[1]));
      });
      Assert.That(fields.MoveNext(), Is.False);
    }

    [Test]
    public void TestJsonExprsInDocumentProjection()
    {
      // this is not a single doc as the project but multiple docs as embedded fields
      string projString = "{'a':1} as a, {'b':2} as b";
      List<Projection> projList = new ExprParser(projString).ParseDocumentProjection();
      Assert.That(projList.Count, Is.EqualTo(2));
      // TODO: verification of remaining elements
    }

    [Test]
    public void TestTableInsertProjection()
    {
      Column col = new ExprParser("a").ParseTableInsertField();
      Assert.That(col.Name, Is.EqualTo("a"));

      col = new ExprParser("`double weird `` string`").ParseTableInsertField();
      Assert.That(col.Name, Is.EqualTo("double weird ` string"));
    }

    [Test]
    public void TestTableUpdateField()
    {
      ColumnIdentifier col;
      col = new ExprParser("a").ParseTableUpdateField();
      Assert.That(col.Name, Is.EqualTo("a"));

      col = new ExprParser("b.c").ParseTableUpdateField();
      Assert.That(col.TableName, Is.EqualTo("b"));
      Assert.That(col.Name, Is.EqualTo("c"));

      col = new ExprParser("d.e$.the_path[2]").ParseTableUpdateField();
      Assert.That(col.TableName, Is.EqualTo("d"));
      Assert.That(col.Name, Is.EqualTo("e"));
      Assert.That(col.DocumentPath.Count, Is.EqualTo(2));
      Assert.That(col.DocumentPath[0].Value, Is.EqualTo("the_path"));
      Assert.That(col.DocumentPath[1].Index, Is.EqualTo((uint)2));

      col = new ExprParser("`zzz\\``").ParseTableUpdateField();
      Assert.That(col.Name, Is.EqualTo("zzz`"));
    }

    [Test]
    public void TestTrivialTableSelectProjection()
    {
      List<Projection> proj = new ExprParser("a, b as c").ParseTableSelectProjection();
      Assert.That(proj.Count, Is.EqualTo(2));
      Assert.That(ExprUnparser.ExprToString(proj[0].Source), Is.EqualTo("a"));
      Assert.That(proj[0].Alias, Is.EqualTo(string.Empty));
      Assert.That(ExprUnparser.ExprToString(proj[1].Source), Is.EqualTo("b"));
      Assert.That(proj[1].Alias, Is.Not.EqualTo(string.Empty));
      Assert.That(proj[1].Alias, Is.EqualTo("c"));
    }

    [Test]
    public void TestStarTableSelectProjection()
    {
      List<Projection> proj = new ExprParser("*, b as c").ParseTableSelectProjection();
      Assert.That(proj.Count, Is.EqualTo(2));
      Assert.That(ExprUnparser.ExprToString(proj[0].Source), Is.EqualTo("*"));
      Assert.That(proj[0].Alias, Is.EqualTo(string.Empty));
      Assert.That(ExprUnparser.ExprToString(proj[1].Source), Is.EqualTo("b"));
      Assert.That(proj[1].Alias, Is.Not.EqualTo(string.Empty));
      Assert.That(proj[1].Alias, Is.EqualTo("c"));
    }

    [Test]
    public void TestComplexTableSelectProjection()
    {
      string projectionString = "(1 + 1) * 100 as `one-o-two`, 'a is \\'a\\'' as `what is 'a'`";
      List<Projection> proj = new ExprParser(projectionString).ParseTableSelectProjection();
      Assert.That(proj.Count, Is.EqualTo(2));

      Assert.That(ExprUnparser.ExprToString(proj[0].Source), Is.EqualTo("((1 + 1) * 100)"));
      Assert.That(proj[0].Alias, Is.EqualTo("one-o-two"));

      Assert.That(proj[1].Source.Literal.VString.Value.ToStringUtf8(), Is.EqualTo("a is 'a'"));
      Assert.That(proj[1].Alias, Is.EqualTo("what is 'a'"));
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
      Assert.That(ExprUnparser.ExprToString(expr), Is.EqualTo("(1 + $.b[0])"));
      expr = new ExprParser("a.*", false).Parse();
      Assert.That(ExprUnparser.ExprToString(expr), Is.EqualTo("$.a.*"));
      expr = new ExprParser("bL . vT .*", false).Parse();
      Assert.That(ExprUnparser.ExprToString(expr), Is.EqualTo("$.bL.vT.*"));
      expr = new ExprParser("dd ** .X", false).Parse();
      Assert.That(ExprUnparser.ExprToString(expr), Is.EqualTo("$.dd**.X"));
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
        Assert.That(Assert.Throws<ArgumentException>(() => new ExprParser(exprString, isRelational).Parse()).Message, Is.EqualTo($"Unable to parse query '{exprString}'"));
      }
      else
      {
        Expr expr = new ExprParser(exprString, isRelational).Parse();
        Assert.That(ExprUnparser.ExprToString(expr), Is.EqualTo(unparserString));
      }
    }
  }
}
