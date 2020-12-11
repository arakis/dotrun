namespace DotRun.Runtime
{
    public class Step
    {
        public Job Job { get; set; }
        public string Name { get; set; }
        public string Run { get; set; }
        public string WorkDirectory { get; set; }
        public string Environment { get; set; }
        public string Shell { get; set; }
    }
}
