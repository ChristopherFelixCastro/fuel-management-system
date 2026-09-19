namespace Inventario.Api.Services
{
    public static class MockRoleHelper
    {
        public static string GetRole(HttpRequest request)
        {
            if (request.Headers.TryGetValue("X-Mock-Role", out var value))
                return value.ToString();
            return "SIN_ROL";
        }

        public static bool HasRole(HttpRequest request, params string[] allowedRoles)
        {
            var role = GetRole(request);
            return allowedRoles.Contains(role);
        }
    }
}