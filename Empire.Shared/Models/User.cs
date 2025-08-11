namespace Empire.Shared.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastLoginDate { get; set; }
    }
}
