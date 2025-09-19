namespace DotRun.Runtime.Factories;

public interface IShellFactory
{
    IShell CreateShell(string shellType);
    IShell GetDefaultShell();
}