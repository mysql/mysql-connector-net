// Copyright © 2004,2010, Oracle and/or its affiliates.  All rights reserved.
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

//  This code was contributed by Sean Wright (srwright@alcor.concordia.ca) on 2007-01-12
//  The copyright was assigned and transferred under the terms of
//  the MySQL Contributor License Agreement (CLA)

using System;
using System.Collections.Generic;
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
        //TODO: fix this
//        SessionTests.mtxReader = new ManualResetEvent(false);
  //      SessionTests.WaitSyncCreation(true);
      }
      else if (HttpContext.Current.Request.Path == "/write.aspx")
      {        
    //    SessionTests.mtxWriter = new ManualResetEvent(false);
      }
    }
  }
}
