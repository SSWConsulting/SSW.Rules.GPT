using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application;

public interface IRulesContext
{
    DbSet<Rule> Rules { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
