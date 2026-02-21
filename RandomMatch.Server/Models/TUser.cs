namespace RandomMatch.Server.Models
{
    public enum GenderUser
    {
        Man,
        Woman
    }
    public enum StateUser
    {
        New0,
        New1,
        New2,
        New3,
        New4,
        New5,
        New6,
        New7,
        New8,
        Search,
        Stop
    }
    public class TUser
    {
        public long ChatId { get; set; }
        public int Age { get; set; }
        public int SearchAge { get; set; }
        public string?  City { get; set; }
        public string? AboutMe { get; set; }
        public string? PhotoId{ get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsBanned { get; set; } = false;
        public StateUser State { get; set; } = StateUser.New0;
        public GenderUser Gender { get; set; }
        public GenderUser SearchGender { get; set; }
    }
}
