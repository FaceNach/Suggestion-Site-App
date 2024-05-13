using Microsoft.Extensions.Caching.Memory;

namespace SuggestionAppLibrary.DataAccess;

public class CategoryData : ICategoryData
{
    private readonly IMongoCollection<CategoryModel> _categories;
    private IMemoryCache _cache;
    private const string cacheName = "CategoryData";

    public CategoryData(IDbConnection db, IMemoryCache cache)
    {
        _categories = db.CategoryCollection;
        _cache = cache;
    }

    public async Task<List<CategoryModel>> GetCategoriesAsync()
    {
        var output = _cache.Get<List<CategoryModel>>(cacheName);

        if (output is null)
        {
            var results = await _categories.FindAsync(_ => true);
            output = results.ToList();

            _cache.Set(cacheName, output, TimeSpan.FromDays(1));
        }
        
        return output;
    }

    public async Task<CategoryModel> GetCategory(string id)
    {
        var result = await _categories.FindAsync(c => c.Id == id);

        return result.FirstOrDefault();
    }

    public Task CreateCategory(CategoryModel category)
    {
        return _categories.InsertOneAsync(category);
    }
}