namespace CustomerOperations.Functions;

public interface IServiceRequestStatusTool
{
    ServiceRequestStatusResult GetStatus(string requestId);
}
