using System;

namespace DotRun.Runtime
{
    public class ConsoleOutput : IOutput
    {
        public void Write(string text)
        {
            Console.Write(text);
        }

        public void WriteLine(string text)
        {
            Console.Write("> ");
            Console.WriteLine(text);
        }

        public void ErrorLine(string text)
        {
            Console.Write("! ");
            Console.WriteLine(text);
        }
    }

}
