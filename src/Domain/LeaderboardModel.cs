namespace Domain;

public class LeaderboardModel
{
    public int Id { get; set; } 
    public DateTimeOffset Date { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
}