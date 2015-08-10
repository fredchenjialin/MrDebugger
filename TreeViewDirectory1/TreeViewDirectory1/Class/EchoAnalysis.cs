using System;
using System.Diagnostics;
using System.IO;

namespace TreeViewDirectory1
{
    internal class EchoAnalysis : IAnalysis
    {
        public ModifyRecord Analyze(UserInfo info)
        {
            if (!Directory.Exists(info.SourcePath) || !Directory.Exists(info.TargetPath))
            {
                MainWindow.Log.Add("请检查同步文件夹目录是否存在！！");
                return null;
            }
            ModifyRecord modifyRecord = Echo(info.SourcePath, info.TargetPath);
            modifyRecord.SyncType = info.SyncType;
            return modifyRecord;
        }
        /// <summary>
        /// 使用Echo方式分析文件夹
        /// </summary>
        /// <param name="dirSource">源文件夹</param>
        /// <param name="dirTarget">目标文件夹</param>
        /// <returns>ModifyRecord</returns>
        private ModifyRecord Echo(string dirSource, string dirTarget)
        {
            var modify = new ModifyRecord();
            var infoSource = new DirectoryInfo(dirSource);
            var infoTarget = new DirectoryInfo(dirTarget);
            FileInfo[] sourceFileInfos = null;
            FileInfo[] targetFileInfos = null;
            try
            {
                sourceFileInfos = infoSource.GetFiles("*", SearchOption.AllDirectories);
                targetFileInfos = infoTarget.GetFiles("*", SearchOption.AllDirectories);
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.WriteLine("{0}", e.Message);
            }

            
            // TarDeals_bool数组中全为true，处理后设为false，遍历时找到true的删除
            var TarDeals_bool = new bool[targetFileInfos.Length];

            /// chongmingming数组中全为true，被确定成为不删除的文件时设为false，用于控制tar端的处理
            /// 1----同源文件一模一样的文件
            /// 2----是仅仅重命名的文件
            var chongmingming = new bool[targetFileInfos.Length];

            for (int i = 0; i < targetFileInfos.Length; i++)
            {
                TarDeals_bool[i] = true;
                chongmingming[i] = true;
            }
            for (int sou = 0; sou < sourceFileInfos.Length; sou++)
            {
                // 重命名标记
                FileInfo chongmm = null;
                FileInfo directorychongmm = null;
                // a    同名标记
                // b    同MD5标记
                int a = 0;
                int b = 0;
                int x = 0; // 控制a的值，全部一样标签,只要找到一个fullname一致的，就一定不删除
                int xx = -1; // 用于控制只会分配一个tar给sou
                for (int tar = 0; tar < targetFileInfos.Length; tar++)
                {
                    if (
                        chongmingming[tar] &&
                        (targetFileInfos[tar].Name
                        .Equals(sourceFileInfos[sou].Name)) &&
                        (MainWindow.GetMD5HashFromFile(targetFileInfos[tar].FullName)
                        .Equals(MainWindow.GetMD5HashFromFile(sourceFileInfos[sou].FullName)))
                        )
                    {
                        if (getRelativePath(dirTarget, targetFileInfos[tar].FullName)
                            .Equals(getRelativePath(dirSource, sourceFileInfos[sou].FullName)))
                        {
                            chongmingming[tar] = false;
                            TarDeals_bool[tar] = false;
                            x = 1;
                            a = 1;
                            if (xx >= 0)
                            {
                                chongmingming[xx] = true;
                                TarDeals_bool[xx] = true;
                            }
                            break;
                        }

                        if (xx < 0)
                        {
                            chongmingming[tar] = false;
                            TarDeals_bool[tar] = false;
                            directorychongmm = new FileInfo(targetFileInfos[tar].FullName);
                            a = 2; xx = tar;
                        }
                    }
                }
                if (x == 1) a = 1;
                
                for (int tar = 0; tar < targetFileInfos.Length; tar++)
                {
                    if (MainWindow.GetMD5HashFromFile(targetFileInfos[tar].FullName).
                        Equals(MainWindow.GetMD5HashFromFile(sourceFileInfos[sou].FullName)) && chongmingming[tar])
                    {
                        // 重命名
                        chongmingming[tar] = false;
                        b = 1;
                        chongmm = new FileInfo(targetFileInfos[tar].FullName);
                        if (a == 0) TarDeals_bool[tar] = false;
                        break;
                    }
                }
                
                if (b == 0)
                {
                    if (0 == a)
                    {
                        modify.CopyCollection1.Add(sourceFileInfos[sou].FullName);
                        modify.CopyCollection2.Add(getNewPath(dirSource, sourceFileInfos[sou].DirectoryName, dirTarget,
                            sourceFileInfos[sou].Name));
                    }
                    else if (1 == a)
                    {
                    }
                    else if (2 == a)
                    {
                        modify.MoveCollection1.Add(directorychongmm.FullName);
                        modify.MoveCollection2.Add(getNewPath(dirSource, sourceFileInfos[sou].DirectoryName,
                            dirTarget, directorychongmm.Name));
                    }
                }
                else
                {
                    if (0 == a)
                    {
                        modify.MoveCollection1.Add(chongmm.FullName);
                        modify.MoveCollection2.Add(getNewPath(dirSource, sourceFileInfos[sou].DirectoryName, dirTarget,
                            sourceFileInfos[sou].Name));
                    }
                    else if (1 == a)
                    {
                    }
                    else if (2 == a)
                    {
                        modify.MoveCollection1.Add(directorychongmm.FullName);
                        modify.MoveCollection2.Add(getNewPath(dirSource, sourceFileInfos[sou].DirectoryName,
                            dirTarget, directorychongmm.Name));
                    }
                }
            }
            for (int i = 0; i < targetFileInfos.Length; i++)
            {
                if (TarDeals_bool[i])
                    modify.DeleteCollection.Add(targetFileInfos[i].FullName);
            }
            

            // 清理文件夹
            string[] sous = Directory.GetDirectories(dirSource, "*", SearchOption.AllDirectories);
            string[] tars = Directory.GetDirectories(dirTarget, "*", SearchOption.AllDirectories);
            foreach (string sou in sous)
            {
                if (!Directory.Exists(dirTarget + sou.Substring(dirSource.Length)))
                {
                    string[] ll = Directory.GetFiles(sou);
                    modify.createDirectories.Add(dirTarget + sou.Substring(dirSource.Length));
                }
            }

            foreach (string tar in tars)
            {
                if (!Directory.Exists(dirSource + tar.Substring(dirTarget.Length)))
                {
                    string[] ll = Directory.GetFiles(tar);
                    modify.deleteDirectories.Add(tar);
                }
            }

            modify.source = dirSource;
            modify.target = dirTarget;
            return modify;
        }

        /// <summary>
        /// 拼凑新路径
        /// </summary>
        /// <param name="sourceRoot">源文件夹</param>
        /// <param name="sourcePath">原文件路径</param>
        /// <param name="targetRoot">目标文件夹</param>
        /// <param name="name">指定文件名</param>
        /// <returns>string</returns>
        private string getNewPath(string sourceRoot, string sourcePath, string targetRoot, string name)
        {
            string newpath = targetRoot + sourcePath.Substring(sourceRoot.Length);
            if (!Directory.Exists(newpath)) Directory.CreateDirectory(newpath);
            return newpath + "\\" + name;
        }
        /// <summary>
        /// 获取相对路径
        /// </summary>
        /// <param name="sourceRoot">全路径</param>
        /// <param name="sourcePath">根文件目录</param>
        /// <returns></returns>
        private string getRelativePath(string sourceRoot, String sourcePath)
        {
            return sourcePath.Substring(sourceRoot.Length);
        }
    }
}