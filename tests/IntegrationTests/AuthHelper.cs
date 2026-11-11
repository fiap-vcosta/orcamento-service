using System.Net.Http.Headers;

namespace IntegrationTests;

public static class AuthHelper
{
    public static void AuthenticateAsStaff(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.CreateStaffToken());
    }
}
