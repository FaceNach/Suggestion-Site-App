using Microsoft.Extensions.Caching.Memory;

namespace SuggestionAppLibrary.DataAccess;

public class MongoSuggestionData : ISuggestionData
{
    private readonly IDbConnection _db;
    private readonly IMongoCollection<SuggestionModel> _suggestion;
    private IUserData _userData;
    private IMemoryCache _cache;
    private const string CacheName = "SuggestionData";

    public MongoSuggestionData(IDbConnection db, IUserData userData, IMemoryCache cache)
    {
        _db = db;
        _suggestion = db.SuggestionCollection;
        _userData = userData;
        _cache = cache;
    }

    public async Task<List<SuggestionModel>> GetSuggestionsAsync()
    {
        var output = _cache.Get<List<SuggestionModel>>(CacheName);

        if (output is null)
        {
            var result = await _suggestion.FindAsync(s => s.Archived == false);

            output = result.ToList();
            _cache.Set(CacheName, output, TimeSpan.FromMinutes(1));
        }
        
        return output;
    }

    public async Task<List<SuggestionModel>> GetApprovedSuggestionsAsync()
    {
        var output = await GetSuggestionsAsync();
        
        return output.Where(x => x.ApprovedForRelease).ToList();
    }

    public async Task<SuggestionModel> GetSuggestion(string id)
    {
        var result = await _suggestion.FindAsync(s => s.Id == id);

        return result.FirstOrDefault();
    }

    public async Task<List<SuggestionModel>> GetSuggestionsWaitingForAprrovalAsync()
    {
        var output = await GetSuggestionsAsync();
        return output.Where(x => x.ApprovedForRelease == false && x.Rejected == false).ToList();
    }

    public async Task UpdateSuggestionAsync(SuggestionModel suggestion)
    {
        await _suggestion.ReplaceOneAsync(s => s.Id == suggestion.Id, suggestion);
        _cache.Remove(CacheName);
    }

    public async Task UpvoteSuggestion(string suggestionId, string userId)
    {
        var client = _db.Client;

        using var session = await client.StartSessionAsync();
        
        session.StartTransaction();

        try
        {
            var db = client.GetDatabase(_db.DbName);
            var suggestionsInTransactions = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
            var suggestion = (await suggestionsInTransactions.FindAsync(s => s.Id == suggestionId)).First();

            bool isUpvote = suggestion.UserVotes.Add(userId);
            if (isUpvote == false)
            {
                suggestion.UserVotes.Remove(userId);
            }

            await suggestionsInTransactions.ReplaceOneAsync(s => s.Id == suggestionId, suggestion);

            var usersInTransaction = db.GetCollection<UserModel>(_db.UserCollectionName);
            var user = await _userData.GetUserAsync(suggestion.Author.Id);

            if (isUpvote)
            {
                user.VotedOnSuggestions.Add(new BasicSuggestionModel(suggestion));
            }
            else
            {
                var suggestionToRemove = user.VotedOnSuggestions.Where(s => s.Id == suggestionId).First();
                user.VotedOnSuggestions.Remove(suggestionToRemove);
            }

            await usersInTransaction.ReplaceOneAsync(u => u.Id == userId, user);

            await session.CommitTransactionAsync();
            
            _cache.Remove(CacheName);

        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    public async Task CreateSuggestions(SuggestionModel suggestion)
    {
        var client = _db.Client;

        using var session = await client.StartSessionAsync();
        
        session.StartTransaction();

        try
        {
            var db = client.GetDatabase(_db.DbName);
            var suggestionsInTransactions = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
            await suggestionsInTransactions.InsertOneAsync(suggestion);

            var usersInTransaction = db.GetCollection<UserModel>(_db.UserCollectionName);
            var user = await _userData.GetUserAsync(suggestion.Author.Id);
            user.AuthoredSuggestions.Add(new BasicSuggestionModel(suggestion));
            await usersInTransaction.ReplaceOneAsync(u => u.Id == user.Id, user);
            await session.AbortTransactionAsync();
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }

}