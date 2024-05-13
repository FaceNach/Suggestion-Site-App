namespace SuggestionAppLibrary.DataAccess;

public interface ICategoryData
{
    Task<List<CategoryModel>> GetCategoriesAsync();
    Task<CategoryModel> GetCategory(string id);
    Task CreateCategory(CategoryModel category);
}