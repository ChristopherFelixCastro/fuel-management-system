using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Tickets.Sandbox.Api.Application.Contracts;

public record CachedIdempotencyResponse(int StatusCode, string ContentType, string Body, string RequestHash);

public interface IIdempotencyService
{
    bool TryGet(string key, out CachedIdempotencyResponse? cachedResponse);
    void Save(string key, string requestHash, int statusCode, string contentType, string body);
    string ComputePayloadHash(string method, string path, string body);
}

public class MemoryIdempotencyService : IIdempotencyService
{
    private readonly ConcurrentDictionary<string, CachedIdempotencyResponse> _store = new();

    public bool TryGet(string key, out CachedIdempotencyResponse? cachedResponse)
    {
        return _store.TryGetValue(key, out cachedResponse);
    }

    public void Save(string key, string requestHash, int statusCode, string contentType, string body)
    {
        _store[key] = new CachedIdempotencyResponse(statusCode, contentType, body, requestHash);
    }

    public string ComputePayloadHash(string method, string path, string body)
    {
        var raw = $"{method.ToUpperInvariant()}|{path.ToLowerInvariant()}|{body}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
