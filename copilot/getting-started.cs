#:package GitHub.Copilot.SDK@1.0.13

using GitHub.Copilot;

await using var client = new CopilotClient();
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "auto",
    OnPermissionRequest = PermissionHandler.ApproveAll
});

var response = await session.SendAndWaitAsync(new MessageOptions
{
    Prompt = "What is 2 + 2?"
});

System.Console.WriteLine(response?.Data.Content);