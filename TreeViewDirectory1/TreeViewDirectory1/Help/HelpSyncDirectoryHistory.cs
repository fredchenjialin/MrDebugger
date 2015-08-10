using System.Xml.Serialization;

namespace TreeViewDirectory1
{
    public class HelpSyncDirectoryHistory
    {
        [XmlAttribute("Id")]
        public int Id { get; set; }
        [XmlAttribute("PairName")]
        public string PairName { get; set; }

        [XmlAttribute("LeftDirectoryName")]
        public string LeftDirectoryName { get; set; }
        [XmlAttribute("RightDirectoryName")]
        public string RightDirectoryName { get; set; }
        [XmlIgnore]
        public string LR { get; set; }

        [XmlAttribute("SyncType")]
        public string SyncType { get; set; }
        [XmlAttribute("SyncTypeId")]
        public int SyncTypeId { get; set; }
    }
}
