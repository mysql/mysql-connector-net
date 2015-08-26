
using MySql.XDevAPI;
using System.Collections.Generic;
using Xunit;

namespace PortableConnectorNetTests
{
  public class JsonDocTests : BaseTest
  {
    [Fact]
    public void SimpleConverstionToJson()
    {
      JsonDoc d = new JsonDoc();
      d.SetValue("_id", 1);
      d.SetValue("pages", 20);
      string s = d.ToString();
      Assert.Equal(@"{""_id"":""1"", ""pages"":""20""}", d.ToString());
    }

    [Fact]
    public void SimpleParse()
    {
      JsonDoc d = new JsonDoc(@"{ ""id"": ""1"", ""pages"": ""20""}");
      JsonDoc d2 = new JsonDoc();
      d2.SetValue("id", "1");
      d2.SetValue("pages", "20");
      Assert.True(d.Equals(d2));
    }

    [Fact]
    public void NestedParse()
    {
      JsonDoc d = new JsonDoc(@"{ ""id"": ""1"", ""pages"": ""20"", 
          ""person"": { ""name"": ""Fred"", ""age"": ""45"" }
      }");
      JsonDoc d2 = new JsonDoc();
      d2.SetValue("id", "1");
      d2.SetValue("pages", "20");
      d2.SetValue("person", new { name = "Fred", age = "45" });
      Assert.True(d.Equals(d2));
    }

    [Fact]
    public void ParseWithArray()
    {
      JsonDoc d = new JsonDoc(@"{ ""id"": ""1"", ""pages"": ""20"", 
          ""books"": [ 
            { ""_id"" : ""1"", ""title"" : ""Book 1"" },
            { ""_id"" : ""2"", ""title"" : ""Book 2"" }
          ]
      }");

      var docs = new[]
      {
        new {  _id = 1, title = "Book 1" },
        new {  _id = 2, title = "Book 2" },
      };
      JsonDoc d2 = new JsonDoc();
      d2.SetValue("id", "1");
      d2.SetValue("pages", "20");
      d2.SetValue("books", docs);
      Assert.True(d.Equals(d2));
    }
  }
}
