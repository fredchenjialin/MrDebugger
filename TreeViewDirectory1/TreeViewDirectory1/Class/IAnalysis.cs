namespace TreeViewDirectory1
{
    public interface IAnalysis
    {
        /// <summary>
        /// 使用指定方式分析文件夹
        /// </summary>
        /// <param name="info">用户输入</param>
        /// <returns>ModifyRecord</returns>
        ModifyRecord Analyze(UserInfo info);
    }
}
