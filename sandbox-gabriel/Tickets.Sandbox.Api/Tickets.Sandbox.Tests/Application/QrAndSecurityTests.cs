using FluentAssertions;
using Microsoft.Extensions.Options;
using Tickets.Sandbox.Api.Infrastructure.Configuration;
using Tickets.Sandbox.Api.Infrastructure.Services;
using Xunit;

namespace Tickets.Sandbox.Tests.Application;

public class QrAndSecurityTests
{
    private readonly QrCodeService _service;

    public QrAndSecurityTests()
    {
        var options = Options.Create(new QrSecurityOptions
        {
            SecretKey = "SuperSecretKeyForTestingHMACSHA256Signature123!"
        });
        _service = new QrCodeService(options);
    }

    [Fact]
    public void GenerateQrSecurityData_ShouldCreateConsistentHashAndSignature()
    {
        var ticketId = Guid.NewGuid();

        var result = _service.GenerateQrSecurityData(ticketId);

        result.RawToken.Should().NotBeNullOrWhiteSpace();
        result.TokenHash.Should().NotBeNullOrWhiteSpace();
        result.Signature.Should().NotBeNullOrWhiteSpace();
        result.QrPayload.Should().NotBeNullOrWhiteSpace();

        // Verificar firma
        var isValid = _service.VerifySignature(ticketId, result.RawToken, result.Signature);
        isValid.Should().BeTrue();

        // Verificar parseo
        var parsed = _service.ParsePayload(result.QrPayload);
        parsed.Should().NotBeNull();
        parsed!.Value.ticketId.Should().Be(ticketId);
        parsed.Value.rawToken.Should().Be(result.RawToken);
        parsed.Value.signature.Should().Be(result.Signature);
    }

    [Fact]
    public void VerifySignature_TamperedToken_ShouldReturnFalse()
    {
        var ticketId = Guid.NewGuid();
        var result = _service.GenerateQrSecurityData(ticketId);

        var isValid = _service.VerifySignature(ticketId, "tampered_token_string", result.Signature);
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_TamperedTicketId_ShouldReturnFalse()
    {
        var ticketId = Guid.NewGuid();
        var otherTicketId = Guid.NewGuid();
        var result = _service.GenerateQrSecurityData(ticketId);

        var isValid = _service.VerifySignature(otherTicketId, result.RawToken, result.Signature);
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GenerateQrImagePng_ShouldReturnValidPngHeader()
    {
        var ticketId = Guid.NewGuid();
        var result = _service.GenerateQrSecurityData(ticketId);

        var pngBytes = _service.GenerateQrImagePng(result.QrPayload);

        pngBytes.Should().NotBeNull();
        pngBytes.Length.Should().BeGreaterThan(100);

        // Validar PNG magic header bytes: 137 80 78 71 (0x89 'P' 'N' 'G')
        pngBytes[0].Should().Be(0x89);
        pngBytes[1].Should().Be(0x50);
        pngBytes[2].Should().Be(0x4E);
        pngBytes[3].Should().Be(0x47);
    }
}
