using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BarberBook.Web.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly AdminOptions _admin;
    private readonly ApiOptions _api;

    public LoginModel(IOptions<AdminOptions> admin, IOptions<ApiOptions> api)
    {
        _admin = admin.Value;
        _api = api.Value;
    }

    [BindProperty]
    [Required]
    public string Username { get; set; } = string.Empty;
    [BindProperty]
    [Required]
    public string Password { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;
    public string ApiBaseUrl => _api.BaseUrl;
    public string TenantId => _admin.TenantId;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Error = "Informe usuário e senha.";
            return Page();
        }

        if (Username != _admin.Username || Password != _admin.Password)
        {
            Error = "Credenciais inválidas.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Username),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return RedirectToPage("/Admin/Index");
    }
}

