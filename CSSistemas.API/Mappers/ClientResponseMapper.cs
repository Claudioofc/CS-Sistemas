using CSSistemas.Application.DTOs.Client;
using CSSistemas.Domain.Entities;

namespace CSSistemas.API.Mappers;

/// <summary>Mapeamento Client → ClientResponse (DRY entre ClientsController e AdminController).</summary>
public static class ClientResponseMapper
{
    public static ClientResponse ToResponse(Client c) => new(
        c.Id, c.BusinessId, c.Name, c.Phone, c.Email, c.Notes, c.IsActive, c.CreatedAt, c.UpdatedAt);
}
