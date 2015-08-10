using System;
using System.Collections;
using System.IO;
using System.Xml.Serialization;

namespace TreeViewDirectory1
{
    [XmlRoot("Pair")]
    public class HelpSDHxml
    {
        [XmlElement("DirectoryPair", typeof(HelpSyncDirectoryHistory))]
        public ArrayList HelpSyncDirectoryHistories { get; set; }


        public HelpSDHxml()
        {
            HelpSyncDirectoryHistories = new ArrayList();
        }
        /// <summary>
        /// xml文件初始化
        /// </summary>
        /// <returns></returns>
        private HelpSDHxml Init()
        {
            XmlSerializer writer =
                new XmlSerializer(typeof(HelpSDHxml));

            var path = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "//pair.xml";
            FileStream file;
            file = System.IO.File.OpenWrite(path);
            HelpSyncDirectoryHistory hsdh = new HelpSyncDirectoryHistory()
            {
                Id = 0,
                PairName = "Index",
                LeftDirectoryName = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                RightDirectoryName = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                LR =
                    AppDomain.CurrentDomain.SetupInformation.ApplicationBase + " < Sync > " +
                    AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                SyncType = "Sync",
                SyncTypeId = 0
            };
            HelpSDHxml tmp = new HelpSDHxml();
            tmp.HelpSyncDirectoryHistories.Add(hsdh);
            writer.Serialize(file, tmp);
            file.Close();
            return tmp;
        }
        /// <summary>
        /// 反序列化xml文件。读取xml到类
        /// </summary>
        /// <returns></returns>
        public HelpSDHxml Load()
        {
            XmlSerializer writer =
                new XmlSerializer(typeof(HelpSDHxml));

            var path = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "//pair.xml";
            FileStream file;
            if (!File.Exists(path))
            {
                file = System.IO.File.Create(path);
                file.Close();
                return this.Init();
            }
            else
            {
                file = System.IO.File.OpenRead(path);
                HelpSDHxml tmp = (HelpSDHxml)writer.Deserialize(file);
                file.Close();
                return tmp;
            }
        }
        /// <summary>
        /// 序列化xml文件。将新建内容加入xml
        /// </summary>
        /// <param name="hsdh">新建内容</param>
        public void Add(HelpSyncDirectoryHistory hsdh)
        {
            XmlSerializer writer = new XmlSerializer(typeof(HelpSDHxml));
            var path = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "//pair.xml";
            FileStream file;
            file = System.IO.File.OpenWrite(path);
            this.HelpSyncDirectoryHistories.Add(hsdh);
            writer.Serialize(file, this);
            file.Close();
        }

    }
}
