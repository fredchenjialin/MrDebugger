using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace MrDebugger
{
    class DirectoryRecord
    {
        public DirectoryInfo Info { get; set; }
        private static bool chongqi = false;
        public IEnumerable<FileInfo> Files
        {
            get
            {
                if (null == Info)
                {
                    return null;
                }
                return Info.GetFiles();
            }
        }
        public string DirectoryName
        {
            get
            {
                if (null == Info)
                {
                    return string.Empty;
                }
                return Info.FullName;
            }
        }
        public IEnumerable<DirectoryRecord> Directories
        {
            get
            {
                if (null == Info)
                {
                    return null;
                }
                var dirr = new ObservableCollection<DirectoryRecord>();
                DirectoryInfo[] directoryInfos = null;
                try
                {
                    directoryInfos = Info.GetDirectories(searchPattern: "*",
                        searchOption: SearchOption.TopDirectoryOnly);
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    MainWindow.Log.Add(e.Message + "若想访问，尝试点击管理员权限重启！");
                    chongqi = true;
                }
                finally
                {
                    if (chongqi)
                    {
                        Debug.WriteLine("restart");
                    }
                    chongqi = false;
                }
                if (directoryInfos == null) return null;
                foreach (var directoryInfo in directoryInfos)
                {
                    if (directoryInfo.Attributes.ToString().IndexOf("Hidden") == -1)
                        dirr.Add(new DirectoryRecord { Info = directoryInfo });
                }
                return dirr;
            }
        }
    }
}
