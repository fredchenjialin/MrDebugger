using System.Collections.ObjectModel;
using System.Windows;

namespace TreeViewDirectory1
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static ObservableCollection<string> Arg;
        protected override void OnStartup(StartupEventArgs e)
        {
            Arg = new ObservableCollection<string>();
            if (e.Args.Length >= 1)
            {
                foreach (string s in e.Args)
                {
                    Arg.Add(s);
                }
            }
        }
    }
}
