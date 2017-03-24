Connector Trace API plugin for Connector/Net
Installation and Usage instructions


The intended audience of this readme is developers who have experience with Visual Studio and developing .NET applications.

Preparing your app
-------------------
Include a reference to Mysql.data.dll in your application and build it.  If you are using a build of 6.2, you will
need to include 'logging=true' on your connection string.  If you are using trunk, you don't need to do that.

Once you have your app built against mysql.data.dll then proceed to the next section.

Installation
------------
Installation is easy.  Just unzip and drop it in the same folder as the executable you want to profile.
The final release will include an installer and will install the product in such a way that it doesn't have
to be in the bin folder but this will be suitable for now.


Connection to an application
----------------------------
If the application you want to profile doesn't have an app.config file then you need to make one.

We need to register our trace listeners in the System.Diagnostics section of the app.config file.  
Here is an example. 

  <system.diagnostics>
	<sources>
      <source name="mysql" switchName="SourceSwitch" switchType="System.Diagnostics.SourceSwitch">
        <listeners>
          <add name="EMTrace" type="MySql.EMTrace.EMTraceListener, MySql.EMTrace" 
                initializeData="" 
		Host="http://<emhost>:<emport>" 
		PostInterval="<default post interval in seconds>" 
		UserId="<agent user id for mem>" 
		Password="<agent password for mem>"/>
        </listeners>
      </source>
    </sources>
    <switches>
      <!-- You can set the level at which tracing is to occur -->
      <add name="SourceSwitch" value="All"/>
    </switches>
  </system.diagnostics>

  If Connector/Net is not installed on your system
  ------------------------------------------------
  If you run the Connector/Net installer, the provider is registered in the system configuration files.  
  However, if you did not run the installer, you will need to register the provider either in the system
  configuration file or in the web or app config file for your application.  To do that, please
  include the following snipper in your config file.  Be sure to change the version number of the provider
  to match the version of Connector/Net you are using.

  <system.data>
    <DbProviderFactories>
      <add name="MySQL Data Provider" invariant="MySql.Data.MySqlClient" description=".Net Framework Data Provider for MySQL" type="MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data, Version=6.2.1.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d" />
    </DbProviderFactories>
  </system.data>


Running the application
-----------------------
If you are running under Visual Studio, you should see a significant amount of output in the Output window.  If you
are running it outside of Visual Studio and want to see trace output you will need to register an additional listener
in your config file.  Here is what you would add (this is not necessary for running under VS)

Inside the <system.diagnostics> element, add:

<trace autoflush="false" indentsize="4">
  <listeners>
    <add name="consoleListener" type="System.Diagnostics.ConsoleTraceListener" />
  </listeners>
</trace>
 

