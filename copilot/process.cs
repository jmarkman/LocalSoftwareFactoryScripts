using System.Diagnostics;

var backlogStartInfo = new ProcessStartInfo
{
    FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true,
    WorkingDirectory = "", // This should be the directory you initialized backlog in
    Arguments = "/d /c backlog task list --status \"To Do\" --plain"
};

using var backlog = Process.Start(backlogStartInfo)!;

string output = backlog.StandardOutput.ReadToEnd();
string error = backlog.StandardError.ReadToEnd();

if (backlog.ExitCode != 0)
{
    throw new InvalidOperationException($"backlog task list failed with exit code {backlog.ExitCode}.");
}

Console.WriteLine(output);