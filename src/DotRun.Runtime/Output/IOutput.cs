namespace DotRun.Runtime
{
    public interface IOutput
    {
        void Write(string text);
        void WriteLine(string text);
        void ErrorLine(string text);
    }

}
