namespace DotRun.Runtime
{
    public class NullOutput : IOutput
    {
        public void Write(string text)
        {
        }

        public void WriteLine(string text)
        {
        }

        public void ErrorLine(string text)
        {
        }
    }

}
