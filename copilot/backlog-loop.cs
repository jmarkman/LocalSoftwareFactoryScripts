#:package GitHub.Copilot.SDK@1.0.13

using System.Diagnostics;
using GitHub.Copilot;

const int CONTEXT_WINDOW_THRESHOLD = 40000;
const int TIMEOUT_DURATION_IN_MINUTES = 8;

const string INITIAL_PROMPT = @"
    Find the next available sequential task in backlog.md, 
    mark it in progress, review the associated files,
    and draft an implementation plan into the task. 
    Present the plan here for my review. Do not write code until I approve.
";

await using var client = new CopilotClient();
var session = await CreateAgentAsync();
var agentPeakInput = 0L;

RegisterSessionEvents(session);

try
{
    do
    {
        // Planning
        var revising = false;
        var revisionText = string.Empty;
        while (true)
        {
            await session.SendAndWaitAsync(new MessageOptions
            {
                Prompt = revising ? revisionText : INITIAL_PROMPT
            }, TimeSpan.FromMinutes(TIMEOUT_DURATION_IN_MINUTES));
    
            Console.WriteLine();
            Console.Write("Approve plan? 'y' to approve, 'n' to cancel, or enter revision feedback: ");
    
            var input = Console.ReadLine()?.Trim() ?? string.Empty;
    
            if (input.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
    
            if (input.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
    
            revisionText = input;
            revising = true;
        }

        // Working
        await session.SendAndWaitAsync(new MessageOptions
        {
            Prompt = "Plan approved. Close the task once the work is completed."
        }, TimeSpan.FromMinutes(TIMEOUT_DURATION_IN_MINUTES));

        // Rotation
        if (agentPeakInput >= CONTEXT_WINDOW_THRESHOLD)
        {
            Console.WriteLine($"[Context Window]: {agentPeakInput} >= {CONTEXT_WINDOW_THRESHOLD}, rotating agent");
            await session.DisposeAsync();
            session = await CreateAgentAsync();
            RegisterSessionEvents(session);
            UpdateContextWindow(0);
        }

    } while (TasksAvailable());
}
finally
{
    await session.DisposeAsync();
}

/// <summary>
/// Subscribes to the event emitter for the provided session,
/// wiring up output and context window calculation
/// </summary>
void RegisterSessionEvents(CopilotSession currentSession)
{
    currentSession.On<SessionEvent>(sessionEvent =>
    {
        switch (sessionEvent)
        {
            case AssistantMessageDeltaEvent deltaEvent:
                Console.Write(deltaEvent.Data.DeltaContent);
                break;
            case AssistantUsageEvent usageEvent:
                Console.WriteLine($"Input tokens: {usageEvent.Data.InputTokens}");
                UpdateContextWindow(usageEvent.Data.InputTokens);
                break;
            case SessionStartEvent startEvent:
                Console.WriteLine(startEvent.Data.ToString());
                break;
            case SessionErrorEvent errorEvent:
                Console.WriteLine(errorEvent.ToString());
                break;
        }
    });
}

/// <summary>
/// Determines if any tasks are still in "To Do" state in backlog.md
/// </summary>
bool TasksAvailable()
{
    var backlogStartInfo = new ProcessStartInfo
    {
        FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
        WorkingDirectory = Directory.GetCurrentDirectory(),
        Arguments = "/d /c backlog task list --status \"To Do\" --plain"
    };

    using var backlog = Process.Start(backlogStartInfo)!;

    string output = backlog.StandardOutput.ReadToEnd();

    if (backlog.ExitCode != 0)
    {
        throw new InvalidOperationException($"backlog task list failed with exit code {backlog.ExitCode}.");
    }

    return !output.Trim().Contains("No tasks found.", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Updates the context window token count
/// </summary>
void UpdateContextWindow(long? tokens)
{
    agentPeakInput = Math.Max(agentPeakInput, tokens ?? 0);
}

/// <summary>
/// Creates a new agent
/// </summary>
async Task<CopilotSession> CreateAgentAsync()
{
    return await client.CreateSessionAsync(new SessionConfig
    {
        Model = "auto",
        SkillDirectories = new List<string>(),
        OnPermissionRequest = PermissionHandler.ApproveAll,
        Streaming = true
    });
}