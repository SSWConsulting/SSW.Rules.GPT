using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Contracts;

public interface IRulesContext
{
    DbSet<Rule> Rules { get; set; }
    DbSet<LeaderboardModel> UserStats { get; set; }
    DbSet<MatchRulesResult> MatchRulesResults { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    public DatabaseFacade Database { get; }
}
