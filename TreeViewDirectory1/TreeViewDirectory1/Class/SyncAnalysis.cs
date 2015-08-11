using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace MrDebugger
{
    class SyncAnalysis : IAnalysis
    {
        public ModifyRecord Analyze(UserInfo info)
        {
            if (!Directory.Exists(info.SourcePath) || !Directory.Exists(info.TargetPath))
            {
                MainWindow.Log.Add("请检查同步文件夹目录是否存在！！");
                return null;
            }
            ModifyRecord modifyRecord = Sync(info.SourcePath, info.TargetPath,info.SyncType);
            modifyRecord.SyncType = info.SyncType;
            return modifyRecord;
        }
        /// <summary>
        /// 使用Sync方式分析文件夹
        /// </summary>
        /// <param name="dirSource">源文件夹</param>
        /// <param name="dirTarget">目标文件夹</param>
        /// <returns>ModifyRecord</returns>
        private ModifyRecord Sync(string dirSource, string dirTarget, int type)
        {
            string sousoutarspecificText = MainWindow.CreateBackupFileName(MainWindow.FormatPath(dirSource), dirSource, dirTarget, type.ToString());
            string soutarsouspecificText = MainWindow.CreateBackupFileName(MainWindow.FormatPath(dirSource), dirTarget, dirSource, type.ToString());
            ModifyRecord record;
            if (!File.Exists(sousoutarspecificText) && !File.Exists(soutarsouspecificText))
            {
                record = InitializeDirectory(dirSource, dirTarget);
            }
            else
            {
                record = ContrastDirectory(dirSource, dirTarget, type);
            }
            return record;
        }

        /// Sync同步方式需要一个中间文件来实现，这个中间文件在开始后被删除，
        /// 避免中间文件对同步的扰动，在同步完成后，再次填充中间文件

        #region Sync同步方式----第一次执行同步
        /// <summary>
        /// 当前文件夹第一次执行同步
        /// </summary>
        /// <param name="dirSource">左文件夹</param>
        /// <param name="dirTarget">右文件夹</param>
        /// <returns>ModifyRecord</returns>
        private ModifyRecord InitializeDirectory(string dirSource, string dirTarget)
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

            // 此处不优化成一层循环是因为需要获取sou与tar两端的被锁定的文件的位置
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                for (int j = 0; j < targetFileInfos.Length; j++)
                {
                    if (sourceFileInfos[i].FullName.Substring(dirSource.Length)
                        .Equals(targetFileInfos[j].FullName.Substring(dirTarget.Length)))
                    {
                        if (sourceFileInfos[i].LastWriteTime.CompareTo(targetFileInfos[j].LastWriteTime) > 0)
                        {
                            modifyRecord.DeleteCollection.Add(targetFileInfos[j].FullName);
                            modifyRecord.CopyCollection1.Add(sourceFileInfos[i].FullName);
                            modifyRecord.CopyCollection2.Add(targetFileInfos[j].FullName);
                        }
                        else if (sourceFileInfos[i].LastWriteTime.CompareTo(targetFileInfos[j].LastWriteTime) < 0)
                        {
                            modifyRecord.DeleteCollection.Add(sourceFileInfos[i].FullName);
                            modifyRecord.CopyCollection1.Add(targetFileInfos[j].FullName);
                            modifyRecord.CopyCollection2.Add(sourceFileInfos[i].FullName);
                        }
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
                        if (sourceFileInfos[i].CreationTime.CompareTo(targetFileInfos[j].CreationTime) >= 0)
                        {
                            if (!Directory.Exists(dirTarget + sourceFileInfos[i].DirectoryName.Substring(dirSource.Length)))
                                Directory.CreateDirectory(dirTarget + sourceFileInfos[i].DirectoryName.Substring(dirSource.Length));
                            modifyRecord.MoveCollection1.Add(targetFileInfos[j].FullName);
                            modifyRecord.MoveCollection2.Add(dirTarget+sourceFileInfos[i].FullName.Substring(dirSource.Length));
                        }
                        else
                        {
                            if (!Directory.Exists(dirSource + targetFileInfos[j].DirectoryName.Substring(dirTarget.Length)))
                                Directory.CreateDirectory(dirSource + targetFileInfos[j].DirectoryName.Substring(dirTarget.Length));
                            modifyRecord.MoveCollection1.Add(sourceFileInfos[i].FullName);
                            modifyRecord.MoveCollection2.Add(dirSource + targetFileInfos[j].FullName.Substring(dirTarget.Length));
                        }
                        tarLock[j] = false;
                        souLock[i] = false;
                        break;
                    }
                }
            }

            // 双向复制
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                if (souLock[i])
                {
                    FileInfo souInfo = sourceFileInfos[i];
                    if (!Directory.Exists(dirTarget + souInfo.DirectoryName.Substring(dirSource.Length)))
                        Directory.CreateDirectory(dirTarget + souInfo.DirectoryName.Substring(dirSource.Length));
                    modifyRecord.CopyCollection1.Add(souInfo.FullName);
                    modifyRecord.CopyCollection2.Add(dirTarget + souInfo.FullName.Substring(dirSource.Length));
                }
                
            }

            for (int j = 0; j < targetFileInfos.Length; j++)
            {
                if (tarLock[j])
                {
                    FileInfo tarInfo = targetFileInfos[j];
                    if (!Directory.Exists(dirSource + tarInfo.DirectoryName.Substring(dirTarget.Length)))
                        Directory.CreateDirectory(dirSource + tarInfo.DirectoryName.Substring(dirTarget.Length));
                    modifyRecord.CopyCollection1.Add(tarInfo.FullName);
                    modifyRecord.CopyCollection2.Add(dirSource + tarInfo.FullName.Substring(dirTarget.Length));
                }
            }
            modifyRecord.source = dirSource;
            modifyRecord.target = dirTarget;
            return modifyRecord;
        }
        #endregion

        #region Sync同步方式----读取中间文件执行同步

        /// <summary>
        /// 当前文件夹再次执行同步
        /// </summary>
        /// <param name="dirSource">左文件夹</param>
        /// <param name="dirTarget">右文件夹</param>
        /// <returns>ModifyRecord</returns>
        private ModifyRecord ContrastDirectory(string dirSource, string dirTarget, int type)
        {
            //先读数据，然后把文件删了

            Hashtable souhashtable = new Hashtable();
            Hashtable tarhashtable = new Hashtable();
            string sousoutarspecificText = MainWindow.CreateBackupFileName(MainWindow.FormatPath(dirSource), dirSource, dirTarget, type.ToString());
            string soutarsouspecificText = MainWindow.CreateBackupFileName(MainWindow.FormatPath(dirSource), dirTarget, dirSource, type.ToString());
            string tarsoutarspecificText = MainWindow.CreateBackupFileName(MainWindow.FormatPath(dirTarget), dirSource, dirTarget, type.ToString());
            string tartarsouspecificText = MainWindow.CreateBackupFileName(MainWindow.FormatPath(dirTarget), dirTarget, dirSource, type.ToString());

            string[] tmpcontrastFiles;
            if (File.Exists(sousoutarspecificText))
            {
                tmpcontrastFiles = File.ReadAllLines(sousoutarspecificText);
                File.Delete(sousoutarspecificText);
                File.Delete(tarsoutarspecificText);
            }
            else
            {
                tmpcontrastFiles = File.ReadAllLines(soutarsouspecificText);
                File.Delete(soutarsouspecificText);
                File.Delete(tartarsouspecificText);
            }
            
            string[,] contrastFiles = new string[tmpcontrastFiles.Length, 2];
            for (int i = 0; i < tmpcontrastFiles.Length; i++)
            {
                string contrastFile = tmpcontrastFiles[i];
                string[] tmp = contrastFile.Split(new char[] { '#' });
                if (tmp.Length != 2)
                    MessageBox.Show("真是出了大问题了！！！！！");
                contrastFiles[i, 0] = tmp[0];
                contrastFiles[i, 1] = tmp[1];
            }

            int contrastFilesIndexLength = contrastFiles.Length/contrastFiles.Rank;

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

            // 存入hashtable
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                souhashtable.Add(sourceFileInfos[i].FullName, i);
            }
            for (int i = 0; i < targetFileInfos.Length; i++)
            {
                tarhashtable.Add(targetFileInfos[i].FullName, i);
            }

            // 建立MD5缓存
            string[] souMD5 = new string[sourceFileInfos.Length];
            string[] tarMD5 = new string[targetFileInfos.Length];
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                souMD5[i] = "";
            }
            for (int j = 0; j < targetFileInfos.Length; j++)
            {
                tarMD5[j] = "";
            }

            var tarLock = new bool[targetFileInfos.Length];
            var souLock = new bool[sourceFileInfos.Length];
            var conLock = new bool[contrastFilesIndexLength];
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                souLock[i] = true;
            }
            for (int j = 0; j < targetFileInfos.Length; j++)
            {
                tarLock[j] = true;
            }
            for (int z = 0; z < contrastFilesIndexLength; z++)
            {
                conLock[z] = true;
            }
            //正式开始
            for (int i = 0; i < contrastFilesIndexLength; i++)
            {
                conLock[i] = false;
                if (File.Exists(dirSource + contrastFiles[i,0])&&File.Exists(dirTarget+contrastFiles[i,0]))
                {
                    #region 左右同时存在 (到目前为止可以判断的)
                    FileInfo souInfotmp = new FileInfo(dirSource + contrastFiles[i,0]);
                    FileInfo tarInfotmp = new FileInfo(dirTarget + contrastFiles[i,0]);
                    tarLock[(int)tarhashtable[dirTarget + contrastFiles[i, 0]]] = false;
                    souLock[(int)souhashtable[dirSource + contrastFiles[i, 0]]] = false;
                    if (tarInfotmp.LastWriteTime.CompareTo(souInfotmp.LastWriteTime) > 0)
                    {
                        modifyRecord.CopyCollection1.Add(tarInfotmp.FullName);
                        modifyRecord.CopyCollection2.Add(souInfotmp.FullName);
                        modifyRecord.DeleteCollection.Add(souInfotmp.FullName);
                    }
                    else if (tarInfotmp.LastWriteTime.CompareTo(souInfotmp.LastWriteTime) < 0)
                    {
                        modifyRecord.CopyCollection1.Add(souInfotmp.FullName);
                        modifyRecord.CopyCollection2.Add(tarInfotmp.FullName);
                        modifyRecord.DeleteCollection.Add(tarInfotmp.FullName);
                    }
                    #endregion
                }
                else if (!File.Exists(dirSource + contrastFiles[i, 0]) && !File.Exists(dirTarget + contrastFiles[i, 0]))
                {
                    
                    #region 左右同时不存在
                    FileInfo tarInfotmp = null;
                    FileInfo souInfotmp = null;
                    bool tarxxx = true;
                    for (int tarsearch = 0; tarsearch < targetFileInfos.Length; tarsearch++)
                    {
                        #region 在tar去找那个重命名的文件
                        if (tarLock[tarsearch] &&
                            getMD5(ref tarMD5, targetFileInfos[tarsearch].FullName, ref tarhashtable)
                                .Equals(contrastFiles[i, 1]))
                        {
                            tarLock[tarsearch] = false;
                            // 找到了重命名的
                            tarxxx = false;
                            tarInfotmp = targetFileInfos[tarsearch];
                            break;
                        }
                        #endregion
                    }
                    bool souxxx = true;
                    for (int sousearch = 0; sousearch < sourceFileInfos.Length; sousearch++)
                    {
                        #region 在sou去找那个重命名的文件
                        if (souLock[sousearch] &&
                            getMD5(ref souMD5, sourceFileInfos[sousearch].FullName, ref souhashtable)
                                .Equals(contrastFiles[i, 1]))
                        {
                            souLock[sousearch] = false;
                            souxxx = false;
                            souInfotmp = sourceFileInfos[sousearch];
                            break;
                        }
                        #endregion
                    }

                    if (souxxx && tarxxx)
                    {
                        
                    }
                    else if (!souxxx && !tarxxx)
                    {
                        if (tarInfotmp.CreationTime.CompareTo(souInfotmp.CreationTime) > 0)
                        {
                            if (!Directory.Exists(dirSource +
                                                             tarInfotmp.DirectoryName.Substring(
                                                                 dirTarget.Length)))
                                Directory.CreateDirectory(dirSource +
                                                             tarInfotmp.DirectoryName.Substring(
                                                                 dirTarget.Length));
                            modifyRecord.MoveCollection1.Add(souInfotmp.FullName);
                            modifyRecord.MoveCollection2.Add(dirSource +
                                                             tarInfotmp.FullName.Substring(
                                                                 dirTarget.Length));
                        }
                        else
                        {
                            if (!Directory.Exists(dirTarget +
                                                             souInfotmp.DirectoryName.Substring(
                                                                 dirSource.Length)))
                                Directory.CreateDirectory(dirTarget +
                                                             souInfotmp.DirectoryName.Substring(
                                                                 dirSource.Length));
                            modifyRecord.MoveCollection1.Add(tarInfotmp.FullName);
                            modifyRecord.MoveCollection2.Add(dirTarget +
                                                             souInfotmp.FullName.Substring(
                                                                 dirSource.Length));
                        }
                    }
                    else
                    {
                        if (souxxx)
                        {
                            modifyRecord.DeleteCollection.Add(tarInfotmp.FullName);
                        }
                        else
                        {
                            modifyRecord.DeleteCollection.Add(souInfotmp.FullName);
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 左右文件只有一边存在
                    if (File.Exists(dirSource + contrastFiles[i, 0]))
                    {
                        souLock[(int) souhashtable[dirSource + contrastFiles[i, 0]]] = false;
                        #region sou端存在
                        if (
                            contrastFiles[i, 1].Equals(getMD5(ref souMD5, dirSource + contrastFiles[i, 0],
                                ref souhashtable)))
                        {
                            #region 存在的文件内容没变
                            bool xxx = true;
                            
                            for (int tarsearch = 0; tarsearch < targetFileInfos.Length; tarsearch++)
                            {
                                #region 去找那个重命名的文件
                                if (tarLock[tarsearch] &&
                                    getMD5(ref tarMD5, targetFileInfos[tarsearch].FullName, ref tarhashtable)
                                        .Equals(contrastFiles[i, 1]))
                                {
                                    tarLock[tarsearch] = false;
                                    // 找到了重命名的
                                    xxx = false;
                                    if (!Directory.Exists(dirSource +
                                                                     targetFileInfos[tarsearch].DirectoryName.Substring(
                                                                         dirTarget.Length)))
                                        Directory.CreateDirectory(dirSource +
                                                                     targetFileInfos[tarsearch].DirectoryName.Substring(
                                                                         dirTarget.Length));
                                    modifyRecord.MoveCollection1.Add(dirSource + contrastFiles[i, 0]);
                                    modifyRecord.MoveCollection2.Add(dirSource +
                                                                     targetFileInfos[tarsearch].FullName.Substring(
                                                                         dirTarget.Length));
                                    break;
                                }
                                #endregion
                            }
                            if (xxx)
                            {
                                modifyRecord.DeleteCollection.Add(dirSource + contrastFiles[i, 0]);
                            }
                            #endregion
                        }
                        else
                        {
                            #region 存在的文件内容变化
                            bool xxx = true;

                            for (int tarsearch = 0; tarsearch < targetFileInfos.Length; tarsearch++)
                            {
                                #region 去找那个重命名的文件
                                if (tarLock[tarsearch] &&
                                    getMD5(ref tarMD5, targetFileInfos[tarsearch].FullName, ref tarhashtable)
                                        .Equals(contrastFiles[i, 1]))
                                {
                                    tarLock[tarsearch] = false;
                                    // 找到了重命名的
                                    xxx = false;
                                    if (!Directory.Exists(dirSource +
                                                                     targetFileInfos[tarsearch].DirectoryName.Substring(
                                                                         dirTarget.Length)))
                                        Directory.CreateDirectory(dirSource +
                                                                     targetFileInfos[tarsearch].DirectoryName.Substring(
                                                                         dirTarget.Length));
                                    modifyRecord.MoveCollection1.Add(dirSource + contrastFiles[i, 0]);
                                    modifyRecord.MoveCollection2.Add(dirSource +
                                                                     targetFileInfos[tarsearch].FullName.Substring(
                                                                         dirTarget.Length));


                                    modifyRecord.CopyCollection1.Add(dirSource +
                                                                     targetFileInfos[tarsearch].FullName.Substring(
                                                                         dirTarget.Length));
                                    modifyRecord.CopyCollection2.Add(targetFileInfos[tarsearch].FullName);
                                    modifyRecord.DeleteCollection.Add(targetFileInfos[tarsearch].FullName);
                                    break;
                                }
                                #endregion
                            }
                            if (xxx)
                            {
                                modifyRecord.DeleteCollection.Add(dirSource + contrastFiles[i, 0]);
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                        tarLock[(int) tarhashtable[dirTarget + contrastFiles[i, 0]]] = false;
                        #region tar端存在
                        if (
                            contrastFiles[i, 1].Equals(getMD5(ref tarMD5, dirTarget + contrastFiles[i, 0],
                                ref tarhashtable)))
                        {
                            #region 存在的文件内容没变
                            bool xxx = true;

                            for (int sousearch = 0; sousearch < sourceFileInfos.Length; sousearch++)
                            {
                                #region 去找那个重命名的文件
                                if (souLock[sousearch] &&
                                    getMD5(ref souMD5, sourceFileInfos[sousearch].FullName, ref souhashtable)
                                        .Equals(contrastFiles[i, 1]))
                                {
                                    souLock[sousearch] = false;
                                    // 找到了重命名的
                                    xxx = false;
                                    if (!Directory.Exists(dirTarget +
                                                                     sourceFileInfos[sousearch].DirectoryName.Substring(
                                                                         dirSource.Length)))
                                        Directory.CreateDirectory(dirTarget +
                                                                     sourceFileInfos[sousearch].DirectoryName.Substring(
                                                                         dirSource.Length));
                                    modifyRecord.MoveCollection1.Add(dirTarget + contrastFiles[i, 0]);
                                    modifyRecord.MoveCollection2.Add(dirTarget +
                                                                     sourceFileInfos[sousearch].FullName.Substring(
                                                                         dirSource.Length));
                                    break;
                                }
                                #endregion
                            }
                            if (xxx)
                            {
                                modifyRecord.DeleteCollection.Add(dirTarget + contrastFiles[i, 0]);
                            }
                            #endregion
                        }
                        else
                        {
                            #region 存在的文件内容变化
                            bool xxx = true;

                            for (int sousearch = 0; sousearch < sourceFileInfos.Length; sousearch++)
                            {
                                #region 去找那个重命名的文件
                                if (souLock[sousearch] &&
                                    getMD5(ref souMD5, sourceFileInfos[sousearch].FullName, ref souhashtable)
                                        .Equals(contrastFiles[i, 1]))
                                {
                                    souLock[sousearch] = false;
                                    // 找到了重命名的
                                    xxx = false;
                                    if (!Directory.Exists(dirTarget +
                                                                     sourceFileInfos[sousearch].DirectoryName.Substring(
                                                                         dirSource.Length)))
                                        Directory.CreateDirectory(dirTarget +
                                                                     sourceFileInfos[sousearch].DirectoryName.Substring(
                                                                         dirSource.Length));
                                    modifyRecord.MoveCollection1.Add(dirTarget + contrastFiles[i, 0]);
                                    modifyRecord.MoveCollection2.Add(dirTarget +
                                                                     sourceFileInfos[sousearch].FullName.Substring(
                                                                         dirSource.Length));
                                    modifyRecord.CopyCollection1.Add(dirTarget +
                                                                     sourceFileInfos[sousearch].FullName.Substring(
                                                                         dirSource.Length));
                                    modifyRecord.CopyCollection2.Add(sourceFileInfos[sousearch].FullName);
                                    modifyRecord.DeleteCollection.Add(sourceFileInfos[sousearch].FullName);
                                    break;
                                }
                                #endregion
                            }
                            if (xxx)
                            {
                                modifyRecord.DeleteCollection.Add(dirTarget + contrastFiles[i, 0]);
                            }
                            #endregion
                        }
                        #endregion
                    }
                    #endregion
                }
            }
            // 在新的里面找相同
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                if (!souLock[i])
                {
                    continue;
                }
                for (int j = 0; j < targetFileInfos.Length; j++)
                {
                    if (tarLock[j]
                        && sourceFileInfos[i].FullName.Substring(dirSource.Length)
                        .Equals(targetFileInfos[j].FullName.Substring(dirTarget.Length)))
                    {
                        souLock[i] = false; tarLock[j] = false;
                        if (targetFileInfos[j].LastWriteTime.CompareTo(sourceFileInfos[i].LastWriteTime) > 0)
                        {
                            modifyRecord.CopyCollection1.Add(targetFileInfos[j].FullName);
                            modifyRecord.CopyCollection2.Add(sourceFileInfos[i].FullName);
                            modifyRecord.DeleteCollection.Add(sourceFileInfos[i].FullName);
                        }
                        else if (targetFileInfos[j].LastWriteTime.CompareTo(sourceFileInfos[i].LastWriteTime) < 0)
                        {
                            modifyRecord.CopyCollection1.Add(sourceFileInfos[i].FullName);
                            modifyRecord.CopyCollection2.Add(targetFileInfos[j].FullName);
                            modifyRecord.DeleteCollection.Add(targetFileInfos[j].FullName);
                        }
                        break;
                    }
                }
            }

            // 双向复制
            for (int i = 0; i < sourceFileInfos.Length; i++)
            {
                if (souLock[i])
                {
                    FileInfo souInfo = sourceFileInfos[i];
                    if (!Directory.Exists(dirTarget + souInfo.DirectoryName.Substring(dirSource.Length)))
                        Directory.CreateDirectory(dirTarget + souInfo.DirectoryName.Substring(dirSource.Length));
                    modifyRecord.CopyCollection1.Add(souInfo.FullName);
                    modifyRecord.CopyCollection2.Add(dirTarget + souInfo.FullName.Substring(dirSource.Length));
                }

            }

            for (int j = 0; j < targetFileInfos.Length; j++)
            {
                if (tarLock[j])
                {
                    FileInfo tarInfo = targetFileInfos[j];
                    if (!Directory.Exists(dirSource + tarInfo.DirectoryName.Substring(dirTarget.Length)))
                        Directory.CreateDirectory(dirSource + tarInfo.DirectoryName.Substring(dirTarget.Length));
                    modifyRecord.CopyCollection1.Add(tarInfo.FullName);
                    modifyRecord.CopyCollection2.Add(dirSource + tarInfo.FullName.Substring(dirTarget.Length));
                }
            }

            modifyRecord.source = dirSource;
            modifyRecord.target = dirTarget;
            return modifyRecord;
        }
        #endregion
        /// <summary>
        /// 若MD5值已经保存，直接获取，否则计算后保存
        /// </summary>
        /// <param name="tmp">保存MD5值的数组</param>
        /// <param name="tmpstring">需要查询MD5值的文件路径</param>
        /// <param name="tmphashtable">用于控制MD5在tmp数组中的下标</param>
        /// <returns>string</returns>
        private string getMD5(ref string[] tmp,string tmpstring,ref Hashtable tmphashtable)
        {
            int tmpll = (int) tmphashtable[tmpstring];
            if (tmp[tmpll] == "")
            {
                tmp[tmpll] = MainWindow.GetMD5HashFromFile(tmpstring);
            }
            return tmp[tmpll];
        }
    }
}