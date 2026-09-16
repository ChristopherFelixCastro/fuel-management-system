using System.Net;
using CombustibleAPI.Application.Exceptions;
using FluentAssertions;
using Xunit;

namespace CombustibleAPI.Tests.Exceptions;

public class ApiExceptionTests
{
    [Theory]
    [InlineData(nameof(ApiException.TicketInexistente), "TICKET_INEXISTENTE", HttpStatusCode.UnprocessableEntity)]
    [InlineData(nameof(ApiException.TicketVencido), "TICKET_EXPIRED", HttpStatusCode.UnprocessableEntity)]
    [InlineData(nameof(ApiException.TicketConsumido), "TICKET_CONSUMED", HttpStatusCode.UnprocessableEntity)]
    [InlineData(nameof(ApiException.InventarioInsuficiente), "INSUFFICIENT_STOCK", HttpStatusCode.UnprocessableEntity)]
    public void FabricasDeErroresDeNegocio_DevuelvenCodigoYStatusEsperados(string factoryName, string expectedCode, HttpStatusCode expectedStatus)
    {
        var method = typeof(ApiException).GetMethod(factoryName, Array.Empty<Type>())!;
        var exception = (ApiException)method.Invoke(null, null)!;

        exception.Code.Should().Be(expectedCode);
        exception.StatusCode.Should().Be(expectedStatus);
    }

    [Fact]
    public void Unauthorized_DevuelveStatus401()
    {
        var exception = ApiException.Unauthorized();

        exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        exception.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public void DespachoDuplicado_DevuelveStatus409Conflict()
    {
        // Regla de negocio: un solo despacho por ticket (constraint UNIQUE a nivel BD).
        var exception = ApiException.DespachoDuplicado();

        exception.StatusCode.Should().Be(HttpStatusCode.Conflict);
        exception.Code.Should().Be("DISPATCH_ALREADY_DONE");
    }
}
