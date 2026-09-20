using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace modular_mlm.Web.Services;

public sealed class ProtectedCookieService(IDataProtectionProvider dataProtectionProvider)
{
    public void Write<T>(
        HttpResponse response,
        string name,
        T payload,
        TimeSpan lifetime,
        bool essential = false
    )
    {
        var protector = CreateProtector(name);
        var json = JsonSerializer.Serialize(payload);
        var protectedValue = protector.Protect(json);

        response.Cookies.Append(
            name,
            protectedValue,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = lifetime,
                Path = "/",
                IsEssential = essential,
            }
        );
    }

    public T? Read<T>(HttpRequest request, string name)
    {
        if (!request.Cookies.TryGetValue(name, out var value))
            return default;

        try
        {
            var json = CreateProtector(name).Unprotect(value);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (CryptographicException)
        {
            return default;
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public void Delete(HttpResponse response, string name)
    {
        // __Host- cookies are accepted by browsers only when Secure and Path=/ are
        // present. Deleting one without the same prefix requirements can leave the
        // revoked session cookie in the browser and invalidate the next login.
        response.Cookies.Delete(
            name,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
            }
        );
    }

    private IDataProtector CreateProtector(string name) =>
        dataProtectionProvider.CreateProtector($"modular_mlm.cookies.{name}.v1");
}
