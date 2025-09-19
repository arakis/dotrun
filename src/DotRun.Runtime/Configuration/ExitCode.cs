namespace DotRun.Runtime.Configuration;

public enum ExitCode
{
    Success = 0,
    GeneralError = 1,
    InvalidArguments = 2,
    ConfigurationError = 3,
    WorkflowExecutionFailed = 4,
    ProjectNotFound = 5,
    WorkflowNotFound = 6,
    Timeout = 7,
    Cancelled = 8,
    UnhandledException = 99
}