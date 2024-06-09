using ICSharpCode.AvalonEdit.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Reflection;

namespace WpfSSH
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        class ShellInteractiveController {
            bool changed = false;
            int maxBuffer = 2 * 1024 * 1024;
            StringBuilder stringBuilder = new StringBuilder();
            System.Windows.Threading.Dispatcher? Dispatcher;

            public event EventHandler<string>? ResetOutputString;

            public ShellInteractiveController(Window main, string inittxt = "")
            {
                Dispatcher = main.Dispatcher;
                if (inittxt.Length > 0)
                {
                    stringBuilder.Append(inittxt);
                }
            }

            public void OnDataReceivedTxt(string txt)
            {
                stringBuilder.Append(txt);
                if (stringBuilder.Length > maxBuffer)
                {
                    stringBuilder.Remove(0, stringBuilder.Length - maxBuffer);
                }
                refOutput(true);
            }
            public void refOutput(bool autoSetChange = true)
            {
                if (autoSetChange)
                {
                    changed = true;
                }
                Task.Run(() => {
                    Thread.Sleep(100);
                    if (changed)
                    {
                        Dispatcher?.Invoke(() =>
                        {
                            Thread.Sleep(10);
                            if (changed)
                            {
                                string output;
                                lock(stringBuilder)
                                {
                                    output = stringBuilder.ToString();
                                }
                                
                                ResetOutputString?.Invoke(this, output);
                                changed = false;
                            }
                        });
                    }
                });                
            }
        }
        class FadeTxtInfoController
        {
            System.Timers.Timer timer_fade = new System.Timers.Timer(4000);

            System.Windows.Threading.Dispatcher? Dispatcher;
            DoubleAnimation da=new DoubleAnimation
            {
                From = 1,
                To = 0.01,
                Duration = new Duration(TimeSpan.FromSeconds(2)),
                AutoReverse = false,
                FillBehavior = FillBehavior.Stop
            };
            Label label_showinfo;

            public FadeTxtInfoController(Window main, Label label)
            {
                label_showinfo = label;
                Dispatcher = main.Dispatcher;

                timer_fade.Elapsed += TimerFade;
                da.Completed += (obj, arg) => {
                    label_showinfo.Content = "";
                };
            }
            void TimerFade(object? obj, System.Timers.ElapsedEventArgs arg)
            {
                timer_fade.Stop();
                Dispatcher?.Invoke(() => {
                    label_showinfo.BeginAnimation(OpacityProperty, da);
                });
            }
            public void ShowInfo(string txt)
            {
                timer_fade.Stop();
                da.BeginTime = new TimeSpan(0, 0, 0);
                label_showinfo.Opacity = 1.0;
                label_showinfo.Content = txt;
                timer_fade.Start();
            }
        }

        ILogger logger;
        FadeTxtInfoController fadeTxtInfoController;
        ShellInteractiveController shellInteractiveController;
        ObservableLinkedList<string> historyList = new ObservableLinkedList<string>();
        SSHConn sshConn = new SSHConn();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<string>? DataReceivedTxt;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        bool _isLogin;
        public bool IsLogin
        {
            get { return _isLogin; }
            set { _isLogin = value; OnPropertyChanged(); OnPropertyChanged("IsNotLogin"); }
        }

        public MainWindow(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<MainWindow>();
            logger.LogInformation("Init Main Window");

            InitializeComponent();
            fadeTxtInfoController = new FadeTxtInfoController(this, label_showinfo);
            shellInteractiveController = new ShellInteractiveController(this, txt_outputcmd.Text);
            shellInteractiveController.ResetOutputString += (sender, txt) =>
            {
                if ((bool)chk_wide.IsChecked)
                {
                    txt = txt.Replace("\n", "\n\n");
                }
                txt_outputcmd.Text = txt + "\n\n\n\n";
                txt_outputcmd.ScrollToEnd();
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    Dispatcher.Invoke(() => { txt_outputcmd.ScrollToEnd(); });
                });
            };
            SearchPanel.Install(txt_outputcmd);

            InitSSH();
            InitUIHistory();

            //lst_his.ItemSource = historyList; 
            string? name = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name; 
            ShowInfo("Show " + name +" v"+ App.GetProductVersion());
        }

        void ShowInfo(string? txt)
        {
            if (txt == null) return;
            fadeTxtInfoController.ShowInfo(txt);
            logger.LogInformation(txt);
        }

        public void InitSSH()
        {
            sshConn.ShellDataReceived += (sender, args) => {
                DataReceivedTxt?.Invoke(sender, Encoding.Default.GetString(args.Data));
            };

            DataReceivedTxt += (sender, txt) =>
            {
                shellInteractiveController.OnDataReceivedTxt(txt);
            };
            sshConn.OnError += (sender, ex) =>
            {
                ShowInfo(ex.Message);
            };
        }
        public void InitUIHistory()
        {
            string txt = tryReadTxt("ServerInfo.xml");
            if (txt.Length > 0) { LoginDataFromXml(txt); }

            txt = tryReadTxt("history.json");
            if (txt.Length > 0)
            {
                JsonArray? array = JsonNode.Parse(txt) as JsonArray;
                if (array != null)
                {
                    foreach (var j in array)
                    {
                        string? str = (string?)j;
                        if (str != null)
                        {
                            historyList.list.AddLast(str);
                        }
                    }
                }
            }
        }

        public void LoginDataFromXml(string txt)
        {
            
        }

        public string LoginDataToXml()
        {
            return "";
        }
        string tryReadTxt(string file)
        {
            string txt= "";
            try { txt = File.ReadAllText(file); } catch (FileNotFoundException ex) { }
            return txt;
        }

        private void btn_login_Click(object sender, RoutedEventArgs e)
        {
            label_showinfo.Content = "";
            ShowInfo("login...");
            DoEvents();
            string xml = LoginDataToXml();
            File.WriteAllText("ServerInfo.xml", xml, Encoding.UTF8);
            string pass = (chk_showpass.IsChecked ?? false) ? txt_pass_show.Text : txt_pass.Password;
            int port;
            if (int.TryParse(txt_port.Text, out port))
            {
                sshConn.Conn(txt_address.Text, port, txt_user.Text, pass);
                if (sshConn.connected)
                {
                    ShowInfo("Login " + txt_address.Text + ":" + port + " success");
                    IsLogin = true;
                    sshConn.UsingShellStream();
                    SwitchLoginSpan();
                }
            }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                
        }

        private void btn_logout_Click(object sender, RoutedEventArgs e)
        {
            sshConn.DisConn();
            IsLogin = false;
        }

        private void txt_inputcmd_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void btn_send_Click(object sender, RoutedEventArgs e)
        {
            string cmd=txt_inputcmd.Text;
            if (!sshConn.SendShellCommand(cmd))
            {
                return;
            }
        }

        private void lst_his_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void txt_inputcmd_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void chk_showpass_Click(object sender, RoutedEventArgs e)
        {
            if((bool)chk_showpass.IsChecked)
            {
                txt_pass_show.Text = txt_pass.Password;
            }
            else
            {
                txt_pass.Password = txt_pass_show.Text;
            }
            txt_pass.Visibility=!(bool)chk_showpass.IsChecked?Visibility.Visible:Visibility.Hidden;
            txt_pass_show.Visibility=(bool)chk_showpass.IsChecked?Visibility.Visible:Visibility.Hidden;
        }

        #region Splitter

        void SwitchLoginSpan()
        {

        }
        #endregion
        


        public void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
    }
}