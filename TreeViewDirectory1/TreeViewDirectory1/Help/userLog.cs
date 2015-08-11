using System.Collections.ObjectModel;

namespace MrDebugger
{
    public class UserLog
    {
        public ObservableCollection<string> logData;

        public UserLog()
        {
            logData = new ObservableCollection<string>();
        }

        public void Add(string tmp)
        {
            logData.Add(tmp);
        }
    }


}
