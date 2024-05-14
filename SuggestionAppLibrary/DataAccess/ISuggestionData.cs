namespace SuggestionAppLibrary.DataAccess;

public interface ISuggestionData
{
    Task<List<SuggestionModel>> GetSuggestionsAsync();
    Task<List<SuggestionModel>> GetApprovedSuggestionsAsync();
    Task<SuggestionModel> GetSuggestion(string id);
    Task<List<SuggestionModel>> GetSuggestionsWaitingForAprrovalAsync();
    Task UpdateSuggestionAsync(SuggestionModel suggestion);
    Task UpvoteSuggestion(string suggestionId, string userId);
    Task CreateSuggestions(SuggestionModel suggestion);
}