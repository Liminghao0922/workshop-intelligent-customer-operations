"""Sample backend API placeholder.

Replace this file with your preferred framework implementation.
"""

from typing import Dict


def health() -> Dict[str, str]:
    return {"status": "ok", "service": "customer-operations-api"}


def chat(message: str) -> Dict[str, str]:
    return {
        "request": message,
        "response": "This is a placeholder response. Connect this function to Azure AI Foundry Agent.",
    }
