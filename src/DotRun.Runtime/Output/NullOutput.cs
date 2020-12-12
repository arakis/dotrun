using System.Collections.Generic;

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

    public class MemoryOutput : IOutput
    {
        public void Write(string text)
        {
        }

        public List<string> Lines { get; } = new List<string>();

        public void WriteLine(string text)
        {
            Lines.Add(text);
        }

        public List<string> Errors { get; } = new List<string>();
        public void ErrorLine(string text)
        {
            Errors.Add(text);
        }
    }

}
