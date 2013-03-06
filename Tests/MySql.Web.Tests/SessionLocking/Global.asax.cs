// Copyright © 2004,2010, Oracle and/or its affiliates.  All rights reserved.
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

//  This code was contributed by Sean Wright (srwright@alcor.concordia.ca) on 2007-01-12
//  The copyright was assigned and transferred under the terms of
//  the MySQL Contributor License Agreement (CLA)

using System;
using System.Collections.Generic;
#if CLR4
using System.Linq;
#endif
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using MySql.Web.Tests;
using System.Threading;

namespace MySql.Web.Tests
{
  public class Global : System.Web.HttpApplication
  {

    void Application_Start(object sender, EventArgs e)
    {
      // Code that runs on application startup

    }

    void Application_End(object sender, EventArgs e)
    {
      //  Code that runs on application shutdown

    }

    void Application_Error(object sender, EventArgs e)
    {
      // Code that runs when an unhandled error occurs

    }

    void Session_Start(object sender, EventArgs e)
    {
      // Code that runs when a new session is started

    }

    void Session_End(object sender, EventArgs e)
    {
      // Code that runs when a session ends. 
      // Note: The Session_End event is raised only when the sessionstate mode
      // is set to InProc in the Web.config file. If session mode is set to StateServer 
      // or SQLServer, the event is not raised.

    }

    void Application_EndRequest(object sender, EventArgs e)
    {
      
    }

    void Application_BeginRequest(object sender, EventArgs e)
    {
      //System.Diagnostics.Debugger.Break();
      if (HttpContext.Current.Request.Path == "/read.aspx")
      {
        // Signaler        
        SessionTests.mtxReader = new ManualResetEvent(false);
        SessionTests.WaitSyncCreation(true);
      }
      else if (HttpContext.Current.Request.Path == "/write.aspx")
      {        
        SessionTests.mtxWriter = new ManualResetEvent(false);
      }
    }
  }
}
