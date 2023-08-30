namespace Domain;

public class LeaderboardUser
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    
    public int LastMonth { get; set; }
    public int LastYear { get; set; }
    public int AllTime { get; set; }
}