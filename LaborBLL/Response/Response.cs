namespace LaborBLL.Response
{
    public record Response<T>(T Result, bool Success, string? ErrorMessage);
}
