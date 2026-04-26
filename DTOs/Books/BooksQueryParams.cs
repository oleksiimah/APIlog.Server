namespace APIlog.Server.DTOs.Books;

public class BooksQueryParams
{
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string SortOrder { get; set; } = "asc";

    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public short? YearMin { get; set; }
    public short? YearMax { get; set; }
    public short? PagesMin { get; set; }
    public short? PagesMax { get; set; }
    public decimal? HeightMin { get; set; }
    public decimal? HeightMax { get; set; }
    public decimal? WidthMin { get; set; }
    public decimal? WidthMax { get; set; }
    public decimal? DepthMin { get; set; }
    public decimal? DepthMax { get; set; }

    public List<int>? AuthorIds { get; set; }
    public List<int>? BookStoreIds { get; set; }
    public List<int>? PublisherIds { get; set; }
    public List<int>? LanguageIds { get; set; }
    public List<int>? CoverTypeIds { get; set; }
    public List<int>? SubjectIds { get; set; }
    public List<int>? BookTypeIds { get; set; }
    public List<int>? GenreIds { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
