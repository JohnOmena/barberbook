using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BarberBook.Web.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly ApiOptions _api;
    private readonly AdminOptions _admin;

    public IndexModel(IOptions<ApiOptions> api, IOptions<AdminOptions> admin)
    {
        _api = api.Value;
        _admin = admin.Value;
    }

    public string ApiBaseUrl => _api.BaseUrl.TrimEnd('/');
    public string TenantId => _admin.TenantId;
    public string Username => User?.Identity?.Name ?? "admin";

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostLogout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Admin/Login");
    }
}
