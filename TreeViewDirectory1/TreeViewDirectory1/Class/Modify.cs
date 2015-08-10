using System.Collections.ObjectModel;

namespace TreeViewDirectory1
{
    /// <summary>
    /// 用于记录将要处理的文件集合
    /// </summary>
    public class ModifyRecord
    {
        public ObservableCollection<string> DeleteCollection;
        public ObservableCollection<string> MoveCollection1;
        public ObservableCollection<string> MoveCollection2;
        public ObservableCollection<string> CopyCollection1;
        public ObservableCollection<string> CopyCollection2;
        
        public ObservableCollection<string> createDirectories;
        public ObservableCollection<string> deleteDirectories;

        public string source { get; set; }
        public string target { get; set; }
        public int SyncType { get; set; }

        public ModifyRecord()
        {
            DeleteCollection = new ObservableCollection<string>();
            MoveCollection1 = new ObservableCollection<string>();
            CopyCollection1 = new ObservableCollection<string>();
            MoveCollection2 = new ObservableCollection<string>();
            CopyCollection2 = new ObservableCollection<string>();
            createDirectories = new ObservableCollection<string>();
            deleteDirectories = new ObservableCollection<string>();
            source = "";
            target = "";
        }

        private int GetRecordCount()
        {
            int count = DeleteCollection.Count + MoveCollection1.Count + CopyCollection1.Count + createDirectories.Count +
                        deleteDirectories.Count;
            return count;
        }
    }
}