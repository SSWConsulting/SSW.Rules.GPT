using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application;

public interface IRulesContext
{
    DbSet<Rule> Rules { get; set; }
    DbSet<MatchRulesResult> MatchRulesResults { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    public DatabaseFacade Database { get; }
}
