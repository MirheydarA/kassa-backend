namespace KassaApi.DTOs;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class CreateClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class UpdateClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}