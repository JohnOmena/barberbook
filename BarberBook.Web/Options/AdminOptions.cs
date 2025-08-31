namespace BarberBook.Web;

public sealed class AdminOptions
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public string TenantId { get; set; } = ""; // required for booking creation
}

