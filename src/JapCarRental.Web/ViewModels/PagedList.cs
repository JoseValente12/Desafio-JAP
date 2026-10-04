namespace JapCarRental.Web.ViewModels;

// One page of a larger list, plus what the view needs to draw the pager.
// Generic so customers and vehicles can share it.
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalItems { get; }
    public int TotalPages { get; }

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    // 1-based range shown to the user ("5 to 8 of 8").
    public int FirstItem => Items.Count == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int LastItem => Items.Count == 0 ? 0 : FirstItem + Items.Count - 1;

    private PagedList(IReadOnlyList<T> items, int page, int pageSize, int totalItems, int totalPages)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = totalPages;
    }

    public static PagedList<T> Create(IEnumerable<T> source, int page, int pageSize)
    {
        var all = source.ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)pageSize));

        // A hand-edited URL like ?page=999 or ?page=-3 lands on a valid page instead of an empty one.
        var safePage = Math.Clamp(page, 1, totalPages);

        var items = all.Skip((safePage - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<T>(items, safePage, pageSize, all.Count, totalPages);
    }
}