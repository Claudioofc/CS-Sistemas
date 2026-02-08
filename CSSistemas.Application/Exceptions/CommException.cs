namespace CSSistemas.Application.Exceptions;

/// <summary>Exceção de regra de negócio/API. Tratada no pipeline global e convertida em resposta HTTP com status e mensagem padronizados.</summary>
public class CommException : Exception
{
    /// <summary>Status HTTP a retornar (400, 404, 409, etc.).</summary>
    public int StatusCode { get; }

    public CommException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }

    public static CommException BadRequest(string message) => new(message, 400);
    public static CommException NotFound(string message = "Recurso não encontrado.") => new(message, 404);
    public static CommException Conflict(string message) => new(message, 409);
    public static CommException BadGateway(string message) => new(message, 502);
    public static CommException ServiceUnavailable(string message) => new(message, 503);
    public static CommException InternalServerError(string message = "Ocorreu um erro. Tente novamente.") => new(message, 500);
}
