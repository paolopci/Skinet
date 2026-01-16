using System;

namespace Core.Specification;

public class ProductSpecParams
{
    private List<string> _brands = [];

    public List<string> Brands
    {
        get => _brands;
        set
        {
            _brands =
            value.SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    private List<string> _types = [];

    public List<string> Types
    {
        get => _types;
        set
        {
            _types =
            value.SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    public string? Sort { get; set; }

    public int PageIndex { get; set; } = 1;

    public const int MaxPageSize = 50;

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value <= 0)
            {
                _pageSize = 10;
                return;
            }

            _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }
    }
}
