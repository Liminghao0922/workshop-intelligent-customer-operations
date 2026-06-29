"""Mock business action: get service request status."""

MOCK_REQUESTS = {
    "SR-1001": {"status": "In Progress", "nextStep": "Engineer review"},
    "SR-1002": {"status": "Pending Customer", "nextStep": "Waiting for additional information"},
    "SR-1003": {"status": "Resolved", "nextStep": "No further action required"},
}


def get_service_request_status(request_id: str):
    return MOCK_REQUESTS.get(request_id, {"status": "Not Found", "nextStep": "Confirm request ID"})
