#:package GitHub.Copilot.SDK@1.0.13

using GitHub.Copilot;

await using var client = new CopilotClient();
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "auto",
    OnPermissionRequest = PermissionHandler.ApproveAll,
    Streaming = true
});

session.On<SessionEvent>(sessionEvent =>
{
    switch (sessionEvent)
    {
        case AssistantMessageDeltaEvent deltaEvent:
            System.Console.WriteLine(deltaEvent.Data.DeltaContent);
            break;
        case SessionIdleEvent idleEvent:
            System.Console.WriteLine();
            break;
    }
});

await session.SendAndWaitAsync(new MessageOptions
{
    Prompt = "Tell me a short joke"
});