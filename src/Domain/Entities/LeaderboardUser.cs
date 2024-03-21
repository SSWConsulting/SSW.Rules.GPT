namespace Domain.Entities;

public class LeaderboardUser
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    
    public int LastMonth { get; set; }
    public int LastYear { get; set; }
    public int AllTime { get; set; }
}