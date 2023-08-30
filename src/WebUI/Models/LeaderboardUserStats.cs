namespace WebUI.Models;

public class LeaderboardUserStats
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    
    public int Position { get; set; }
    
    public int LastMonth { get; set; }
    public int LastYear { get; set; }
    public int AllTime { get; set; }
}