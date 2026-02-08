namespace CSSistemas.Application.DTOs.Auth;

/// <summary>Resultado do login: sucesso, senha errada (com tentativas restantes) ou conta bloqueada.</summary>
public abstract record LoginResult;

public record LoginSuccessResult(LoginResponse Response) : LoginResult;

public record LoginFailureResult(int AttemptsRemaining) : LoginResult;

public record LoginLockedResult(DateTime LockoutEnd) : LoginResult;
