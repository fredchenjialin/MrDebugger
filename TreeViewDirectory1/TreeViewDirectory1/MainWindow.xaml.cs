using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Microsoft.Win32;

namespace TreeViewDirectory1
{
    /// <summary>
    ///     MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string T2 = "SOFTWARE\\Classes\\Directory\\shell"; // 文件夹
        private const string T3 = "SOFTWARE\\Classes\\Directory\\background\\shell"; // 文件空白
        public static HelpSDHxml helpSDHxml = new HelpSDHxml();
        public static UserLog Log = new UserLog();
        public static IEnumerable<HelpMethod> Static_HelpMethods;
        //public static ObservableCollection<string> logData;
        public enum SYNCType
        {
            Sync,
            Echo,
            Combine,
        }

        public MainWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        ///     窗口加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DirectoryLoad();
            _WriteXml();
            RightClick_ContextMenuLoad();
            helpSDHxml = helpSDHxml.Load();
            ListBox_LoadHistoryDirectory(
                AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "//pair.xml");
            SyncComboBox_LoadMethod();
            this.ListBox_Log.ItemsSource = Log.logData;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            File.Delete(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "//SerializationOverview.xml");
        }
        /// <summary>
        ///     载入驱动器信息
        /// </summary>
        private void DirectoryLoad()
        {
            var directory = new ObservableCollection<DirectoryRecord>();
            foreach (DriveInfo drive in DriveInfo.GetDrives().Where(drive => drive.IsReady))
            {
                directory.Add
                    (new DirectoryRecord
                    {
                        Info = new DirectoryInfo(drive.RootDirectory.FullName)
                    }
                    );
            }
            if (directoryTreeView1 != null) directoryTreeView1.ItemsSource = directory;
            if (directoryTreeView2 != null) directoryTreeView2.ItemsSource = directory;
        }
        /// <summary>
        ///     右键菜单载入目录
        /// </summary>
        private void RightClick_ContextMenuLoad()
        {
            int ll = App.Arg.Count;
            string tmpstring1 = string.Empty;
            string tmpstring2 = string.Empty;
            if (ll == 1)
            {
                tmpstring1 = App.Arg[0];
                left_textbox.Text = FormatPath(tmpstring1);
            }
            else if (ll == 2)
            {
                tmpstring1 = App.Arg[0];
                tmpstring2 = App.Arg[1];

                left_textbox.Text = FormatPath(tmpstring1);
                SyncComboBox.SelectedIndex = int.Parse(tmpstring2);
            }
            else if (ll == 3)
            {
                tmpstring1 = App.Arg[0];
                tmpstring2 = App.Arg[1];
                left_textbox.Text = FormatPath(tmpstring1);
                right_textbox.Text = FormatPath(tmpstring2);
                SyncComboBox.SelectedIndex = int.Parse(App.Arg[2]);
            }
        }
        /// <summary>
        /// 测试用同步入口
        /// </summary>
        private void Startup(UserInfo userinfo)
        {
            userinfo.SourcePath = left_textbox.Text;
            userinfo.TargetPath = right_textbox.Text;
            if (userinfo.SourcePath == string.Empty
                ||userinfo.TargetPath == string.Empty
                )
            {
                Log.Add("同步文件为空！！");
            }
            else if (userinfo.SourcePath == userinfo.TargetPath)
            {
                Log.Add("同步文件夹路径相同！！");
            }
            else if (userinfo.SyncType<(int)SYNCType.Sync
                    ||userinfo.SyncType>(int)SYNCType.Combine)
            {
                Log.Add("未选择同步方式！！");
            }
            else
            {
                // 文件备份的问题可以在别的文件夹保存
                DealBackupFile(userinfo);
                Start_Button.IsEnabled = false;
                Log.Add("正在开始同步...");
                try
                {
                    new Thread(() =>
                    {
                        AnalyzerFactory analyzerFactory = new AnalyzerFactory();
                        IAnalysis iAnalysis = analyzerFactory.CreatAnalyser(userinfo.SyncType);
                        ModifyRecord modifyRecord = iAnalysis.Analyze(userinfo);
                        Execute execute = new Execute();
                        execute.Fix(modifyRecord);
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show("同步完成！");
                            Start_Button.IsEnabled = true;
                        }));
                    }).Start();
                }
                catch (ArgumentNullException e)
                {
                    Log.Add(e.Message);
                    Start_Button.IsEnabled = true;
                }
            }
        }
        /// <summary>
        ///     加载ListBox的内容
        /// </summary>
        /// <param name="xmlpath"></param>
        private void ListBox_LoadHistoryDirectory(string xmlpath)
        {
            XDocument xdoc = XDocument.Load(xmlpath);

            //this.listBoxHistory.ItemsSource = helpSDHxml.HelpSyncDirectoryHistories;

            listBoxHistory.ItemsSource =
                from element in xdoc.Descendants("DirectoryPair")
                select new HelpSyncDirectoryHistory
                {
                    Id = int.Parse(element.Attribute("Id").Value),
                    LeftDirectoryName = element.Attribute("LeftDirectoryName").Value,
                    RightDirectoryName = element.Attribute("RightDirectoryName").Value,
                    PairName = element.Attribute("PairName").Value,
                    SyncType = element.Attribute("SyncType").Value,
                    SyncTypeId = int.Parse(element.Attribute("SyncTypeId").Value),
                    LR = element.Attribute("LeftDirectoryName").Value
                         + " < " + element.Attribute("SyncType").Value + " > " +
                         element.Attribute("RightDirectoryName").Value
                };
        }
        /// <summary>
        /// 加载SerializationOverview.xml配置文件，向ComboBox加载同步方式的内容
        /// </summary>
        private void SyncComboBox_LoadMethod()
        {
            string xmlpath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "//SerializationOverview.xml";
            XDocument xdoc = XDocument.Load(xmlpath);
            Static_HelpMethods =
                from element in xdoc.Descendants("Method")
                select new HelpMethod
                {
                    Id = int.Parse(element.Attribute("Id").Value),
                    MethodName = element.Attribute("MethodName").Value
                };
            //HelpMethod sMethod = new HelpMethod() {Id = Settings.Default.SyncId, MethodName = Settings.Default.Sync};
            //HelpMethod eMethod = new HelpMethod() { Id = Settings.Default.EchoId, MethodName = Settings.Default.Echo };
            //HelpMethod cMethod = new HelpMethod() { Id = Settings.Default.CombineId, MethodName = Settings.Default.Combine };
            //syncCollection.HelpMethods
            SyncComboBox.ItemsSource = Static_HelpMethods;
            SyncComboBox.DisplayMemberPath = "MethodName";
        }
        /// <summary>
        ///     ListBox被选中触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBoxHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HelpSyncDirectoryHistory hsdh = ((sender as ListBox).SelectedItem as HelpSyncDirectoryHistory);
            left_textbox.Text = hsdh.LeftDirectoryName;
            right_textbox.Text = hsdh.RightDirectoryName;
            this.SyncComboBox.SelectedIndex = hsdh.SyncTypeId;
        }
        /// <summary>
        ///     方法ConboBox被选中触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HelpMethod hm = ((sender as ComboBox).SelectedItem as HelpMethod);
        }

        private void Button_Click_Registe(object sender, RoutedEventArgs e)
        {
            //registryOne(t1, "File", "C:\\Windows\\regedit.exe");
            //registryOne(t2, "Directory", "C:\\Windows\\regedit.exe");
            if (Registe_Button.Content.ToString() == "注册")
            {
                if (RegistryOne(T3, "CombineTo", GetType().Assembly.Location, " %V 2")
                    && RegistryOne(T3, "EchoTo", GetType().Assembly.Location, " %V 1")
                    && RegistryOne(T3, "SyncTo", GetType().Assembly.Location, " %V 0")
                    && RegistryOne(T2, "CombineTo", GetType().Assembly.Location, " %1 2")
                    && RegistryOne(T2, "EchoTo", GetType().Assembly.Location, " %1 1")
                    && RegistryOne(T2, "SyncTo", GetType().Assembly.Location, " %1 0")
                    )
                {
                    MessageBox.Show("已成功写入注册表，现在可以在文件夹以及文件空白处右键操作");
                }
                Registe_Button.Content = "反注册";
            }
            else
            {
                string[] regs = new string[2];
                regs[0] = T2;
                regs[1] = T3;
                CleanRegistry(regs);
                Registe_Button.Content = "注册";
            }
        }

        private void Button_Click_Start(object sender, RoutedEventArgs e)
        {
            UserInfo Static_userinfo = new UserInfo();
            Static_userinfo.SourcePath = left_textbox.Text;
            Static_userinfo.TargetPath = right_textbox.Text;
            Static_userinfo.SyncType = SyncComboBox.SelectedIndex;
            if (MessageBoxResult.OK ==
                MessageBox.Show(
                    "源目录：" + Static_userinfo.SourcePath + "\r\n目标目录：" + Static_userinfo.TargetPath + "\r\n同步方式：" +
                    GetMethodName(Static_userinfo.SyncType),
                    "请确定您的操作", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel))
            {
                Startup(Static_userinfo);
            }
        }

        private void Button_Click_ReBoot(object sender, RoutedEventArgs e)
        {
            UserInfo Static_userinfo = new UserInfo();
            Static_userinfo.SourcePath = FormatPath(left_textbox.Text);
            Static_userinfo.TargetPath = FormatPath(right_textbox.Text);
            if (MessageBoxResult.OK ==
                MessageBox.Show(
                    Static_userinfo.SourcePath + "-" + Static_userinfo.TargetPath + "-" + Static_userinfo.SyncType,
                    "显示userinfo", MessageBoxButton.OKCancel))
                AutoReboot(Static_userinfo);
        }
        /// <summary>
        ///     用于写入一项注册表信息
        /// </summary>
        /// <param name="registryPath">注册位置</param>
        /// <param name="methodName">显示的字符串</param>
        /// <param name="command">注册方法的命令</param>
        /// <param name="parameter">注册方法的命令的参数</param>
        /// <returns></returns>
        private bool RegistryOne(string registryPath, string methodName, string command, string parameter)
        {
            try
            {
                RegistryKey shellKey = Registry.CurrentUser.OpenSubKey(registryPath, true);
                if (shellKey == null)
                    shellKey = Registry.CurrentUser.CreateSubKey(registryPath);
                RegistryKey namekey =
                    shellKey.CreateSubKey(methodName, RegistryKeyPermissionCheck.ReadWriteSubTree);
                namekey.SetValue("Icon", command, RegistryValueKind.ExpandString);
                RegistryKey commandKey =
                    namekey.CreateSubKey("command", RegistryKeyPermissionCheck.ReadWriteSubTree);
                commandKey.SetValue("", command + parameter, RegistryValueKind.String);
                commandKey.Close();
                namekey.Close();
                shellKey.Close();
                return true;
            }
            catch (SecurityException)
            {
                MessageBox.Show("请以管理员权限运行程序");
                return false;
            }
        }
        /// <summary>
        /// 清理注册表
        /// </summary>
        /// <param name="registryPath">需要清理的表项集合</param>
        private void CleanRegistry(string[] registryPath)
        {
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    Debug.WriteLine(registryPath[i]);
                    RegistryKey shellKey = Registry.CurrentUser.OpenSubKey(registryPath[i]);
                    if (shellKey != null)
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(registryPath[i]);
                    }
                }
                MessageBox.Show("反注册成功，相关内容已从注册表删除！");
            }
            catch (ArgumentNullException error)
            {
                Debug.WriteLine(error);
            }
            catch (ArgumentException error)
            {
                Console.WriteLine(error);
            }
        }
        /// <summary>
        /// 以管理员权限重启程序
        /// </summary>
        private static void AutoReboot(UserInfo staticUserinfo)
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);

            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show("现在我是管理员");
            }
            else
            {
                MessageBoxResult dr = MessageBox.Show("你好，是否允许获取管理员权限,并重启应用？", "获取管理员权限", MessageBoxButton.OKCancel,
                    MessageBoxImage.Question, MessageBoxResult.OK);

                if (dr == MessageBoxResult.OK)
                {
                    Assembly.GetEntryAssembly();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.UseShellExecute = true;
                    startInfo.WorkingDirectory = Environment.CurrentDirectory;
                    startInfo.FileName = Application.Current.MainWindow.GetType().Assembly.Location;
                    startInfo.Verb = "runas";
                    startInfo.Arguments = "\"" + staticUserinfo.SourcePath + "\" \"" + staticUserinfo.TargetPath +
                                          "\" " + staticUserinfo.SyncType;
                    try
                    {
                        Process.Start(startInfo);
                    }
                    catch (InvalidOperationException e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    Application.Current.Shutdown();
                }
            }
        }
        /// <summary>
        /// 格式化字符串，删除以\结尾的字符串的\
        /// </summary>
        /// <param name="l">需要格式化的字符串</param>
        /// <returns></returns>
        public static string FormatPath(string l)
        {
            if (l.EndsWith(@"\"))
            {
                l = l.Substring(0, l.Length - 1);
            }
            return l;
        }
        /// <summary>
        /// 获取文件的MD5
        /// </summary>
        /// <param name="fileName">指定文件的全路径</param>
        /// <returns></returns>
        public static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                if (!File.Exists(fileName)) return string.Empty;
                FileStream file = new FileStream(fileName, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (FileNotFoundException ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
            catch (SecurityException ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }
        /// <summary>
        /// 获取方法名字符串
        /// </summary>
        /// <param name="id">方法Id</param>
        /// <returns>string</returns>
        public static string GetMethodName(int id)
        {
            foreach (HelpMethod helpMethod in Static_HelpMethods)
            {
                if (helpMethod.Id == id) return helpMethod.MethodName;
            }
            return "";
        }
        /// <summary>
        /// 写入xml文件
        /// </summary>
        private void _WriteXml()
        {
            string configpath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "//SerializationOverview.xml";
            if (!File.Exists(configpath))
            {
                FileStream fs = new FileStream(configpath,FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                /*
                 * <?xml version="1.0" encoding="utf-8" ?>
                    <MethodList>
                      <Method Id="0" MethodName="Sync"/>
                      <Method Id="1" MethodName="Echo"/>
                      <Method Id="2" MethodName="Combine"/>
                    </MethodList>
                 */
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                sw.WriteLine("<MethodList>");
                sw.WriteLine("  <Method Id=\"0\" MethodName=\"Sync\"/>");
                sw.WriteLine("  <Method Id=\"1\" MethodName=\"Echo\"/>");
                sw.WriteLine("  <Method Id=\"2\" MethodName=\"Combine\"/>");
                sw.WriteLine("</MethodList>");
                sw.Close();
                fs.Close();
            }
        }
        /// <summary>
        /// 处理记录文件
        /// </summary>
        /// <param name="userinfo"></param>
        private void DealBackupFile(UserInfo userinfo)
        {
            string soutarspecificText = CreateBackupFileName(FormatPath(userinfo.SourcePath), userinfo.SourcePath, userinfo.TargetPath,
                                                            userinfo.SyncType.ToString());
            string tarsouspecificText = CreateBackupFileName(FormatPath(userinfo.SourcePath), userinfo.TargetPath, userinfo.SourcePath,
                                                            userinfo.SyncType.ToString());
            
            if (!File.Exists(soutarspecificText) && !File.Exists(tarsouspecificText))
            {
                string[] soudelete = Directory.GetFiles(userinfo.SourcePath, "MrDebugger-*",
                    SearchOption.TopDirectoryOnly);
                for (int i = 0; i < soudelete.Length; i++)
                {
                    File.Delete(soudelete[i]);
                }

                string[] tardelete = Directory.GetFiles(userinfo.TargetPath, "MrDebugger-*",
                    SearchOption.TopDirectoryOnly);
                for (int i = 0; i < tardelete.Length; i++)
                {
                    File.Delete(tardelete[i]);
                }
                //加入xml
                helpSDHxml.Add(new HelpSyncDirectoryHistory
                {
                    Id = 1,
                    PairName = userinfo.SourcePath,
                    LeftDirectoryName = userinfo.SourcePath,
                    RightDirectoryName = userinfo.TargetPath,
                    SyncType = GetMethodName(userinfo.SyncType),
                    SyncTypeId = userinfo.SyncType
                });
            }
        }
        /// <summary>
        /// 创建记录文件名
        /// </summary>
        /// <param name="direction">相对文件夹</param>
        /// <param name="front">前文件夹名</param>
        /// <param name="back">后文件夹名</param>
        /// <param name="Type">同步方式</param>
        /// <returns></returns>
        public static string CreateBackupFileName(string direction, string front, string back, string Type)
        {
            return direction + "\\MrDebugger-" + front.Replace(@":\", "&&").Replace(@"\", "&") + "&&&" + back.Replace(@":\", "&&").Replace(@"\", "&") + Type;
        }
    }
}