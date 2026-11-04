import asyncio
import json
import os
import time
from datetime import datetime, timezone
from typing import Any

from agent_framework import Agent, FunctionInvocationContext, function_middleware, tool
from agent_framework.openai import OpenAIChatClient


def get_setting(*names: str) -> str | None:
    """Retrieve setting from environment variables or .NET user secrets fallback."""
    for name in names:
        value = os.getenv(name)
        if value:
            return value

    user_secrets_id = "1a482776-e7c1-4dfa-9179-40743b66d232"
    appdata = os.getenv("APPDATA")
    if appdata:
        secrets_path = os.path.join(
            appdata, "Microsoft", "UserSecrets", user_secrets_id, "secrets.json"
        )
        if os.path.exists(secrets_path):
            try:
                with open(secrets_path, "r", encoding="utf-8-sig") as f:
                    content = f.read().strip()
                    if content:
                        data = json.loads(content)
                        for name in names:
                            if name in data:
                                return data[name]
            except Exception:
                pass
    return None


@tool(name="get_current_datetime", description="Get the current date and time in UTC.")
def get_current_datetime() -> str:
    """Gets the current date and time in UTC format."""
    return datetime.now(timezone.utc).isoformat()


@function_middleware
async def middleware(
    context: FunctionInvocationContext,
    call_next: Any,
) -> Any:
    """Function middleware to intercept and log tool calls and arguments."""
    tool_details = f"- Tool Call: '{context.function.name}'"
    items: list[tuple[str, Any]] = []

    if context.arguments:
        if isinstance(context.arguments, dict):
            items = list(context.arguments.items())
        elif hasattr(context.arguments, "model_dump"):
            items = list(context.arguments.model_dump().items())

    if items:
        args_str = ", ".join(f"[{k} = {v}]" for k, v in items)
        tool_details += f" (Args: {args_str})"

    print(tool_details)
    return await call_next()


async def main() -> None:
    endpoint = get_setting("endpoint", "AZURE_OPENAI_ENDPOINT")
    api_key = get_setting("apikey", "AZURE_OPENAI_API_KEY")

    if not endpoint or not api_key:
        raise RuntimeError(
            "Set endpoint and apikey (or AZURE_OPENAI_ENDPOINT and "
            "AZURE_OPENAI_API_KEY) before running this program."
        )

    chat_client = OpenAIChatClient(
        model="gpt-5",
        api_key=api_key,
        azure_endpoint=endpoint,
    )

    football_agent = Agent(
        client=chat_client,
        instructions=(
            "You are an expert in Football statistics. Use the date and time tool "
            "whenever current date/time context is required. Use the web search tool "
            "for any information that is not available in your training data. Always "
            "provide sources for your information."
        ),
        name="Football Bot",
        description="Football Bot that provides current football statistics",
        tools=[
            get_current_datetime,
            OpenAIChatClient.get_web_search_tool(),
        ],
    )

    agent = Agent(
        client=chat_client,
        instructions="You are a helpful assistant. Refer all requests about football statistics to the Football Bot.",
        name="Main Agent",
        description="An assistant that provides weather information and football statistics.",
        tools=[football_agent.as_tool()],
        middleware=[middleware],
    )

    start = time.perf_counter()
    response = await agent.run(
        "How many games per season are played in the premier league by all teams?"
    )
    elapsed_ms = int((time.perf_counter() - start) * 1000)

    print(response)

    usage = getattr(response, "usage", None) or getattr(response, "usage_details", None)
    if usage:
        if isinstance(usage, dict):
            input_tokens = usage.get("input_token_count", 0)
            cached_tokens = (
                usage.get("cache_read_input_token_count")
                or usage.get("openai.cached_input_tokens", 0)
            )
            output_tokens = usage.get("output_token_count", 0)
            reasoning_tokens = (
                usage.get("reasoning_output_token_count")
                or usage.get("openai.reasoning_tokens", 0)
            )
            total_tokens = usage.get("total_token_count", 0)
        else:
            input_tokens = getattr(usage, "input_tokens", 0)
            cached_tokens = (
                getattr(getattr(usage, "input_tokens_details", None), "cached_tokens", 0) or 0
            )
            output_tokens = getattr(usage, "output_tokens", 0)
            reasoning_tokens = (
                getattr(getattr(usage, "output_tokens_details", None), "reasoning_tokens", 0) or 0
            )
            total_tokens = getattr(usage, "total_tokens", 0)

        print()
        print(f"Input Tokens: {input_tokens}")
        print(f"Cached Tokens: {cached_tokens}")
        print(f"Output Tokens: {output_tokens}")
        print(f"Reasoning Tokens: {reasoning_tokens}")
        print(f"Total Tokens: {total_tokens}")

    print(f"Time spent: {elapsed_ms} ms")


if __name__ == "__main__":
    asyncio.run(main())
