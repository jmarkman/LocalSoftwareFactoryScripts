import asyncio
import subprocess
from cursor_sdk.asyncio import AsyncClient, AsyncRun
from cursor_sdk import LocalAgentOptions, SDKMessage, AsyncAgent, RunResult, CursorAgentError, SDKAssistantMessage, SDKThinkingMessage, SDKToolUseMessage, SDKStatusMessage, SDKUsageMessage, TextBlock, ToolUseBlock

class AgentResponse:
    def __init__(self, token_usage, run_result: RunResult) -> None:
        self.token_usage = token_usage
        self.run_result = run_result

agent_peak_input = 0

CONTEXT_THRESHOLD = 240000

INITIAL_PROMPT = """
    Find the next available task in backlog.md, 
    mark it in progress, review the associated files,
    and draft an implementation plan into the task. 
    Present the plan here for my review. Do not write code until I approve.
"""

NEXT_TASK_PROMPT = """
    The previous task is done. Find the next available backlog task,
    write its plan via backlog CLI, and present it for review.
    Do not implement until I approve.
"""

def available_tasks():
    result = subprocess.run(
        "backlog task list --status \"To Do\" --plain",
        capture_output=True,
        text=True,
        check=True,
        shell=True,
    )
    if "No tasks found." in result.stdout.strip():
        return False
    return True

def update_usage(peak_input):
    global agent_peak_input
    agent_peak_input = max(agent_peak_input, peak_input)



async def log_message(msg: SDKMessage):
    if isinstance(msg, SDKAssistantMessage):
        for block in msg.message.content:
            if isinstance(block, TextBlock):
                print(block.text, end="")
            if isinstance(block, ToolUseBlock):
                print(block.input, end="")
    elif isinstance(msg, SDKThinkingMessage):
        print(msg.text, end="")
    elif isinstance(msg, SDKToolUseMessage):
        print(f"[tool]: {msg.name} - {msg.status}")
    elif isinstance(msg, SDKStatusMessage):
        print(f"[status]: {msg.status}")
    elif isinstance(msg, SDKUsageMessage):
        print(f"[usage in tokens]: {msg.usage.total_tokens}")

async def process_agent_messages(run: AsyncRun):
    peak_input = 0
    async for message in run.messages():
        await log_message(message)
        if message.type == "usage":
            peak_input = max(peak_input, message.usage.input_tokens)
    response = await run.wait()
    if response.usage is not None:
        peak_input = max(peak_input, response.usage.input_tokens)
    return AgentResponse(peak_input, response)

async def rotate_agent(client: AsyncClient, old_agent: AsyncAgent) -> AsyncAgent:
    await old_agent.close()
    return await create_agent(client)

async def create_async_cursor_client():
    project_directory = ""
    return await AsyncClient.launch_bridge(workspace=project_directory)

async def create_agent(client: AsyncClient):
    project_directory = ""
    key = ""
    model_name = "composer-2.5"
    local_options = LocalAgentOptions(cwd=project_directory)
    return await client.agents.create(
        model=model_name,
        api_key=key,
        local=local_options
    )

async def main():
    async with await create_async_cursor_client() as client:
        agent = await create_agent(client)
        first_task = True
        try:
            while True:
                if available_tasks() == False:
                    print("All tasks complete.")
                    break
                prompt = INITIAL_PROMPT if first_task else NEXT_TASK_PROMPT
                first_task = False
                # Planning phase
                revising = False
                revision_text = ""
                while True:
                    planning_run = await agent.send(revision_text if revising else prompt)
                    result = await process_agent_messages(planning_run)
                    update_usage(result.token_usage)
                    user_input = await asyncio.to_thread(
                        input,
                        "Approve plan? 'y' to approve, 'n' to cancel, or type revision feedback: "
                    )
                    user_response = user_input.strip().lower()
                    if user_response == "y":
                        break
                    if user_response == "n":
                        return
                    revision_text = user_input
                    revising = True
                # Working phase
                working_run = await agent.send("Plan approved. Close the task once the work is completed.")
                work_result = await process_agent_messages(working_run)
                update_usage(work_result.token_usage)
                if work_result.run_result.status == "error":
                    print(f"Run failed: {work_result.run_result.id}")
                    return
                # Agent rotation
                if agent_peak_input >= CONTEXT_THRESHOLD:
                    print(f"[Context Window]: {agent_peak_input} >=  {CONTEXT_THRESHOLD}, rotating agent")
                    agent = await rotate_agent(client, agent)
                    update_usage(0)
                    first_task = True
        except CursorAgentError as err:
            print(f"Loop ran into an exception: '{err.message}'. Retryable = '{err.is_retryable}'")
            return
        finally:
            await agent.close()

if __name__ == "__main__":
    asyncio.run(main())
