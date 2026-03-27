using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Client.Infrastructure.Auth;

internal static class JwtClaimsParser
{
    public static IReadOnlyList<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return [];
        }

        try
        {
            var jsonBytes = ParseBase64Url(parts[1]);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes) ?? new();
            var claims = new List<Claim>();

            foreach (var entry in payload)
            {
                if (entry.Key is "role" or "roles")
                {
                    AppendRoleClaims(claims, entry.Value);
                    continue;
                }

                if (entry.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in entry.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(entry.Key, item.ToString()));
                    }

                    continue;
                }

                var claimType = entry.Key switch
                {
                    "name" or "unique_name" => ClaimTypes.Name,
                    "sub" or "nameid" => ClaimTypes.NameIdentifier,
                    _ => entry.Key
                };

                claims.Add(new Claim(claimType, entry.Value.ToString()));
            }

            return claims;
        }
        catch
        {
            return [];
        }
    }

    public static string? GetDisplayName(IEnumerable<Claim> claims)
    {
        var claimList = claims as IList<Claim> ?? claims.ToList();

        return claimList.FirstOrDefault(claim => claim.Type == ClaimTypes.Name)?.Value
            ?? claimList.FirstOrDefault(claim => claim.Type == "name")?.Value
            ?? claimList.FirstOrDefault(claim => claim.Type == "unique_name")?.Value
            ?? claimList.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value;
    }

    public static bool IsExpired(IEnumerable<Claim> claims)
    {
        var expClaim = claims.FirstOrDefault(claim => claim.Type == "exp")?.Value;
        if (!long.TryParse(expClaim, out var expSeconds))
        {
            return false;
        }

        var expiration = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
        return expiration <= DateTimeOffset.UtcNow;
    }

    private static void AppendRoleClaims(ICollection<Claim> claims, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                claims.Add(new Claim(ClaimTypes.Role, item.ToString()));
            }

            return;
        }

        claims.Add(new Claim(ClaimTypes.Role, value.ToString()));
    }

    private static byte[] ParseBase64Url(string base64Url)
    {
        var padded = (base64Url.Length % 4) switch
        {
            2 => base64Url + "==",
            3 => base64Url + "=",
            _ => base64Url
        };

        padded = padded.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(padded);
    }

    public static string Encode(object value)
    {
        var json = JsonSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
