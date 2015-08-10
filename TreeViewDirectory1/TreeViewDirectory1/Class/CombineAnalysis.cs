using System;
using System.Diagnostics;
using System.IO;

namespace TreeViewDirectory1
{
    /// <summary>
    /// 这个类的重点在于复制一些新的文件去目标文件夹，但是需要找到可以直接剪切的可以省下复制的时间
    /// </summary>
    class CombineAnalysis : IAnalysis
    {
        public ModifyRecord Analyze(UserInfo info)
        {
            if (!Directory.Exists(info.SourcePath) || !Directory.Exists(info.TargetPath))
            {
                MainWindow.Log.Add("请检查同步文件夹目录是否存在！！");
                return null;
            }
            ModifyRecord modifyRecord = Combine(info.SourcePath, info.TargetPath);
            modifyRecord.SyncType = info.SyncType;
            return modifyRecord;
        }
        /// <summary>
        /// 使用combine方式分析文件夹
        /// </summary>
        /// <param name="dirSource">源文件夹</param>
        /// <param name="dirTarget">目标文件夹</param>
        /// <returns>ModifyRecord</returns>
        private ModifyRecord Combine(string dirSource, string dirTarget)
        {
            ModifyRecord modifyRecord = new ModifyRecord();
            DirectoryInfo infoSource = new DirectoryInfo(dirSource);
            DirectoryInfo infoTarget = new DirectoryInfo(dirTarget);
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

            // 从两端找到（rpath+文件名）相等的文件，对这些文件进行锁定
            var tarLock = new bool[targetFileInfos.Length];
            var souLock = new bool[sourceFileInfos.Length];
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                souLock[i] = true;
            }
            for (int j = 0; j < targetFileInfos.Length; j++)
            {
                tarLock[j] = true;
            }
            
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                for (int j = 0; j < targetFileInfos.Length; j++)
                {
                    if (sourceFileInfos[i].FullName.Substring(dirSource.Length)
                        .Equals(targetFileInfos[j].FullName.Substring(dirTarget.Length)))
                    {
                        souLock[i] = false; tarLock[j] = false; break;
                    }
                }
            }
            

            
            // sou端对应在tar的可以用来的剪切的副本，即内容相同
            string[] saveCache = new string[sourceFileInfos.Length];
            for (int i = 0; i < sourceFileInfos.Length; i++)
                saveCache[i] = "";

            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                if (!souLock[i])
                {
                    continue;
                }
                for (int j = 0; j < targetFileInfos.Length; j++)
                {
                    if (tarLock[j]
                        &&
                        MainWindow.GetMD5HashFromFile(sourceFileInfos[i].FullName)
                            .Equals(MainWindow.GetMD5HashFromFile(targetFileInfos[j].FullName)))
                    {
                        saveCache[i] = targetFileInfos[j].FullName;
                        tarLock[j] = false;
                    }
                }
            }
            
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                FileInfo souInfo = sourceFileInfos[i];
                if (!File.Exists(dirTarget + souInfo.FullName.Substring(dirSource.Length)))
                {
                    if (!Directory.Exists(dirTarget + souInfo.DirectoryName.Substring(dirSource.Length)))
                        Directory.CreateDirectory(dirTarget + souInfo.DirectoryName.Substring(dirSource.Length));
                    if (saveCache[i].Equals(""))
                    {
                        modifyRecord.CopyCollection1.Add(souInfo.FullName);
                        modifyRecord.CopyCollection2.Add(dirTarget + souInfo.FullName.Substring(dirSource.Length));
                    }
                    else
                    {
                        modifyRecord.MoveCollection1.Add(saveCache[i]);
                        modifyRecord.MoveCollection2.Add(dirTarget + souInfo.FullName.Substring(dirSource.Length));
                    }
                }
            }

            modifyRecord.source = dirSource;
            modifyRecord.target = dirTarget;
            return modifyRecord;
        }

    }
}
