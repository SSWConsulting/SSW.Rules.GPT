using Application.Contracts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class LeaderboardService
{
    private readonly IRulesContext _rulesContext;
    
    public LeaderboardService(IRulesContext rulesContext)
    {
        _rulesContext = rulesContext;
    }
    
    public async Task<List<LeaderboardUser>> GetLeaderboardStats()
    {
        //Needs to be universal time in order to work the database date values
        var oneMonth = DateTime.Now.AddMonths(-1).ToUniversalTime();
        var oneYear = DateTime.Now.AddYears(-1).ToUniversalTime();

        return await _rulesContext.UserStats
            .GroupBy(s => new { s.Name, s.Email })
            .Select(group => new LeaderboardUser()
            {
                Name = group.Key.Name,
                Email = group.Key.Email,
                LastMonth = group.Count(u => u.Date >= oneMonth),
                LastYear = group.Count(u => u.Date >= oneYear),
                AllTime = group.Count()
            }).ToListAsync();
    }
}