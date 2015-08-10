using System;
using System.IO;

namespace TreeViewDirectory1
{
    class Execute
    {
        /// <summary>
        /// 执行操作
        /// </summary>
        /// <param name="modifyRecord">文件记录</param>
        public void Fix(ModifyRecord modifyRecord)
        {
            if (modifyRecord == null)
            {
                return;
            }
            DispatcherAdd("正在删除...");
            for (int i = 0; i < modifyRecord.DeleteCollection.Count; i++)
            {
                DispatcherAdd("删除 " + modifyRecord.DeleteCollection[i]);
                File.Delete(modifyRecord.DeleteCollection[i]);
            }

            DispatcherAdd("正在移动...");
            for (int i = 0; i < modifyRecord.MoveCollection1.Count; i++)
            {
                DispatcherAdd("将 " + modifyRecord.MoveCollection1[i] + "重命名到 " + modifyRecord.MoveCollection2[i]);
                File.Move(modifyRecord.MoveCollection1[i], modifyRecord.MoveCollection2[i]);
            }

            DispatcherAdd("正在复制...");
            for (int i = 0; i < modifyRecord.CopyCollection1.Count; i++)
            {
                DispatcherAdd("将 " + modifyRecord.CopyCollection1[i] + "复制到 " + modifyRecord.CopyCollection2[i]);
                File.Copy(modifyRecord.CopyCollection1[i], modifyRecord.CopyCollection2[i]);
            }

            DispatcherAdd("创建空文件夹...");
            for (int i = 0; i < modifyRecord.createDirectories.Count; i++)
            {
                DispatcherAdd("创建文件夹" + modifyRecord.createDirectories[i]);
                Directory.CreateDirectory(modifyRecord.createDirectories[i]);
            }

            DispatcherAdd("删除多余的文件夹...");
            for (int i = 0; i < modifyRecord.deleteDirectories.Count; i++)
            {
                DispatcherAdd("删除文件夹" + modifyRecord.deleteDirectories[i]);
                Directory.Delete(modifyRecord.deleteDirectories[i], true);
            }

            DispatcherAdd("正在生成backup文件...");
            if (modifyRecord.source != "" && modifyRecord.target != "")
            {
                WriteFileAndMD5toSpecificFile(modifyRecord.source,modifyRecord.target,modifyRecord.SyncType);
            }

            DispatcherAdd("完毕.");
        }

        /// <summary>
        /// 备份当前文件信息
        /// </summary>
        /// <param name="souPath">左文件夹</param>
        /// <param name="tarPath">右文件夹</param>
        private void WriteFileAndMD5toSpecificFile(string souPath,string tarPath,int type)
        {
            string souspecificFile = MainWindow.CreateBackupFileName(MainWindow.FormatPath(souPath), souPath, tarPath, type.ToString());
            string tarspecificFile = MainWindow.CreateBackupFileName(MainWindow.FormatPath(tarPath), souPath, tarPath, type.ToString());
            FileStream fs = File.Create(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\MrDebugger-backup");
            File.SetAttributes(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\MrDebugger-backup",FileAttributes.Hidden);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fs))
            {
                foreach (string info in Directory.GetFiles(souPath, "*", SearchOption.AllDirectories))
                {
                    file.WriteLine(info.Substring(souPath.Length) + "#" + MainWindow.GetMD5HashFromFile(info));
                }
            }
            fs.Close();

            File.Copy(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\MrDebugger-backup",
                souspecificFile, true);
            File.Copy(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\MrDebugger-backup",
                tarspecificFile, true);
            File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\MrDebugger-backup");
        }

        private void DispatcherAdd(string tmp)
        {
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                MainWindow.Log.Add(tmp);
            }));
        }
    }
}
