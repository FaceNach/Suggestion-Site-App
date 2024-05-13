namespace SuggestionAppLibrary.DataAccess;

public interface IStatusData
{
    Task<List<StatusModel>> GetStatuses();
    Task CreateStatus(StatusModel status);
}