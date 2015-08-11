namespace MrDebugger
{
    class AnalyzerFactory
    {
        /// <summary>
        /// 产生一个分析类的对象
        /// </summary>
        /// <param name="syncType">分析类的类型Id</param>
        /// <returns></returns>
        public IAnalysis CreatAnalyser(int syncType)
        {
            IAnalysis analysis;
            if (syncType == (int)MainWindow.SYNCType.Sync)
            {
                analysis = new SyncAnalysis();
            }
            else if (syncType == (int)MainWindow.SYNCType.Echo)
            {
                analysis = new EchoAnalysis();
            }
            else if (syncType == (int)MainWindow.SYNCType.Combine)
            {
                analysis = new CombineAnalysis();
            }
            else
            {
                MainWindow.Log.Add("该种同步方法未实现！！");
                return null;
            }
            return analysis;
        }
    }
}
