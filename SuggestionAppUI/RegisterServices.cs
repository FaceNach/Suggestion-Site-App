namespace SuggestionAppUI;

public static class RegisterServices
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddMemoryCache();
    }
}