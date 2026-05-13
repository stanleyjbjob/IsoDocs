using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

public interface ICaseRepository
{
    Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
