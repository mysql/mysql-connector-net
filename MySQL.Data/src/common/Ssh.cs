using System.Linq;

namespace MySql.Data.Common
{
  /// <summary>
  /// Wrapper class used for handling SSH connections.
  /// </summary>
  internal class Ssh
  {
    #region Fields

    private bool _isXProtocol;
    private uint _port;
    private string _server;
    private string _sshHostName;
    private string _sshKeyFile;
    private string _sshPassphrase;
    private string _sshPassword;
    private uint _sshPort;
    private string _sshUserName;

    #endregion

    internal Ssh(
      string sshHostName,
      string sshUserName,
      string sshPassword,
      string sshKeyFile,
      string sshPassphrase,
      uint sshPort,
      string server,
      uint port,
      bool isXProtocol
    )
    {
      _sshHostName = sshHostName;
      _sshUserName = sshUserName;
      _sshPassword = sshPassword;
      _sshKeyFile = sshKeyFile;
      _sshPassphrase = sshPassphrase;
      _sshPort = sshPort;
      _server = server;
      _port = port;
      _isXProtocol = isXProtocol;
    }

    /// <summary>
    /// Starts the SSH client.
    /// </summary>
    internal void StartClient()
    {
      MySqlSshClientManager.SetupSshClient(
        _sshHostName,
        _sshUserName,
        _sshPassword,
        _sshKeyFile,
        _sshPassphrase,
        _sshPort,
        _server,
        _port,
        _isXProtocol
        );
    }

    /// <summary>
    /// Stops the SSH client.
    /// </summary>
    internal void StopClient()
    {
      var sshClient = MySqlSshClientManager.CurrentSshClient;
      if (sshClient != null && sshClient.IsConnected)
      {
        sshClient.ForwardedPorts.First().Stop();
        sshClient.Disconnect();
      }
    }
  }
}
