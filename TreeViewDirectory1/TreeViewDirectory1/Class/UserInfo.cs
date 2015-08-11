namespace MrDebugger
{
    /// <summary>
    /// 用户输入
    /// </summary>
    public class UserInfo
    {
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }

        /// <summary>
        /// 0---sync
        /// 1---echo
        /// 2---combine
        /// </summary>
        public int SyncType { get; set; }
    }
}
