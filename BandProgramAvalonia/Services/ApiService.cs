using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace BandProgramAvalonia.Services;

public class ApiService
{
    private static readonly HttpClient httpClient = new();
    private readonly Util util = Util.GetInstance();

    public string GetAuthorization(string clientId, string clientSecret)
    {
        var str = $"{clientId}:{clientSecret}";
        return util.Base64Encoding(str);
    }

    public async Task<string?> GetRequestResultAsync(string requestUrl)
    {
        try
        {
            return await httpClient.GetStringAsync(requestUrl);
        }
        catch
        {
            return null;
        }
    }

    public string? GetRequestResult(string requestUrl)
    {
        try
        {
            return httpClient.GetStringAsync(requestUrl).Result;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetTokenAsync(string authorizationCode, string authorization)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://auth.band.us/oauth2/token?grant_type=authorization_code&code={authorizationCode}");
            request.Headers.Add("Authorization", authorization);

            var response = await httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }
}
