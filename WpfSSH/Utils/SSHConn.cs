using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfSSH
{
    internal class SSHConn : IDisposable
    {
        SshClient? sshClient;
        public bool connected = false;
        ShellStream? shell;

        public event EventHandler<Exception>? OnError;
        public event EventHandler<ShellDataEventArgs>? ShellDataReceived;

        public bool Conn(string host, int port, string username, string password, bool disConnLastFirst = false)
        {
            if (disConnLastFirst)
            {
                DisConn();
            }
            var newSshClient = new SshClient(host, port, username, password);
            try
            {
                newSshClient.Connect();
                if (newSshClient.IsConnected)
                {
                    if (!disConnLastFirst)
                    {
                        DisConn();
                    }
                    sshClient = newSshClient;
                    newSshClient = null;
                    connected = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
                return false;
            }
            finally { if (newSshClient != null) { newSshClient.Dispose(); } }
            return false;
        }
        public void UsingShellStream()
        {
            if (shell == null)
            {
                if ((bool)sshClient?.IsConnected)
                {
                    shell = sshClient.CreateShellStream("", 200, 50, 1000, 600, 4 * 1024 * 1024);

                    shell.DataReceived += (sender, arg) =>
                    {
                        ShellDataReceived?.Invoke(sender, arg);
                    };
                }
            }
        }

        public bool SendShellCommand(string cmd, bool isLine = true)
        {
            if (shell != null)
            {
                try
                {
                    StreamWriter writer = new StreamWriter(shell);
                    writer.AutoFlush = true;
                    if (isLine)
                    {
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.Write(cmd);
                    }
                    if (shell.Length == 0)
                    {
                        Thread.Sleep(100);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, ex);
                }
            }
            OnError?.Invoke(this, new Exception("shell not inited"));
            return false;
        }

        public void DisConn()
        {
            if (sshClient != null) {
                if (sshClient.IsConnected) { sshClient.Disconnect(); }
            }
            if (shell != null) { shell.Dispose(); shell = null; }
            connected = false;
        }

        public void Dispose()
        {
            sshClient?.Dispose();
            shell?.Dispose();
        }

        public bool RunCommand(string cmd)
        {
            if ((bool)sshClient?.IsConnected)
            {
                sshClient.RunCommand(cmd);
                return true;
            }
            return false;
        }

    }
}
