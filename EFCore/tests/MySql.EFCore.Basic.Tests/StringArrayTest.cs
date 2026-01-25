using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestModels.EntitySplitting;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using static MySql.EntityFrameworkCore.Basic.Tests.BasicGuidTests;
namespace MySql.EntityFrameworkCore.Basic.Tests
{
    public class StringArrayTest
    {
        [Test]
        public void TestStringArray()
        {
            using var context = new ContextStringArray();
            string[] stringArray = new string[] { "TEST" };
            var stockCodeMAtch = context.StringCodeMatch.FirstOrDefault(q => stringArray.Contains(q.Code));
        }

        public class StringCodeMatch
        {
            [Key]
            public int RecordID { get; set; }
            public string Code { get; set; }
        }
        public class ContextStringArray : DbContext
        {
            public DbSet<StringCodeMatch> StringCodeMatch { get; set; }
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (!optionsBuilder.IsConfigured
                  || optionsBuilder.Options.FindExtension<MySQLOptionsExtension>() == null)
                {
                    optionsBuilder.UseMySQL($"server=localhost;user=root;database=DbContextStringArray;CharSet=utf8;Pooling=false;");
                }
            }
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
            }
        }
    }
}
