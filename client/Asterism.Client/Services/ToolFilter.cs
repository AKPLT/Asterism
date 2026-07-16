using Asterism.Shared.Models;

namespace Asterism.Client.Services;

/// <summary>一覧画面(MainViewModel)と管理者画面(AdminViewModel)で共通の検索・カテゴリ絞り込みロジック。</summary>
public static class ToolFilter
{
    public const string AllCategoriesLabel = "すべて";
    public const string FavoritesLabel = "★ お気に入り";

    public static bool Matches(ToolEntry tool, string? searchText, string selectedCategory)
    {
        var categoryOk = selectedCategory == AllCategoriesLabel || tool.Category == selectedCategory;

        var text = searchText?.Trim() ?? "";
        var textOk = text.Length == 0
            || tool.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
            || tool.Description.Contains(text, StringComparison.OrdinalIgnoreCase)
            || tool.Tags.Any(t => t.Contains(text, StringComparison.OrdinalIgnoreCase));

        return categoryOk && textOk;
    }

    public static List<string> BuildCategoryList(IEnumerable<string> categories)
    {
        var list = new List<string> { AllCategoriesLabel };
        list.AddRange(categories.Distinct().OrderBy(c => c, StringComparer.CurrentCulture));
        return list;
    }
}
