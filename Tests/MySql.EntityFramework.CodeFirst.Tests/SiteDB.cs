// Copyright © 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace MySql.Data.Entity.CodeFirst.Tests
{   
    [Table("pagina")]
    public class pagina
    {
        [Key]
        public long nCdPagina { get; set; }
        public long nCdVisitante { get; set; }
        public string sDsUrlReferencia { get; set; }
        public string sDsPalavraChave { get; set; }
        public string sDsTitulo { get; set; }

        [ForeignKey("nCdVisitante")]
        public visitante visitante { get; set; }
    }

    public class retorno
    {
        //[Key]
        public long Key { get; set; }
        public int Online { get; set; }
    }

    [Table("site")]
    public class site
    {
        [Key]
        public long nCdSite { get; set; }
        public string sDsTitulo { get; set; }
        public string sDsUrl { get; set; }
        public DateTime tDtCadastro { get; set; }
    }

    [Table("visitante")]
    public class visitante
    {
        [Key]
        public long nCdVisitante { get; set; }
        public long nCdSite { get; set; }
        public string sDsIp { get; set; }
        public DateTime tDtCadastro { get; set; }
        public DateTime tDtAtualizacao { get; set; }

        [ForeignKey("nCdSite")]
        public site site { get; set; }
    }

#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
    public class SiteDbContext : DbContext
    {
        public DbSet<visitante> Visitante { get; set; }
        public DbSet<site> Site { get; set; }
        //public DbSet<retorno> Retorno { get; set; }
        public DbSet<pagina> Pagina { get; set; }

        public SiteDbContext()
        {
            Database.SetInitializer<SiteDbContext>(new SiteDbInitializer());
#if EF6
            Database.SetInitializer<SiteDbContext>(new MigrateDatabaseToLatestVersion<SiteDbContext, Configuration<SiteDbContext>>());
#endif
        }
    }

    public class SiteDbInitializer : DropCreateDatabaseReallyAlways<SiteDbContext>
    {
    }
}
