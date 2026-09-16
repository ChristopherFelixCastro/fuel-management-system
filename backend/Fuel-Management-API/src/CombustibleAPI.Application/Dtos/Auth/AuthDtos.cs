using System.ComponentModel.DataAnnotations;

namespace CombustibleAPI.Application.Dtos.Auth;

public class LoginRequestDto
{
    [Required] public string Username { get; set; } = default!;
    [Required] public string Password { get; set; } = default!;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = default!;
    public DateTime AccessTokenExpiraEn { get; set; }
    public DateTime Expiracion => AccessTokenExpiraEn;

    /// <summary>
    /// Valor real del refresh token, entregado una única vez al cliente.
    /// Solo su hash se persiste en BD (SDP Iván sección 4).
    /// </summary>
    public string RefreshToken { get; set; } = default!;
    public DateTime RefreshTokenExpiraEn { get; set; }

    public string Usuario { get; set; } = default!;
    public string Rol { get; set; } = default!;
    public Guid UsuarioId { get; set; }
    public Guid? EstacionId { get; set; }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Usuario { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Rol { get; set; } = default!;
    public Guid? StationId { get; set; }
    public Guid? EstacionId => StationId;
    public string? EstacionNombre { get; set; }
    public Guid? EmpleadoId { get; set; }
    public string? EmpleadoNombre { get; set; }
}

public class RefreshRequestDto
{
    [Required] public string RefreshToken { get; set; } = default!;
}

public class LogoutRequestDto
{
    [Required] public string RefreshToken { get; set; } = default!;
}
