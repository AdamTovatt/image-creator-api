namespace ImageCreatorApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public List<string>? Permissions { get; set; }

        public User(int id, string email, List<string> permissions)
        {
            Id = id;
            Email = email;
            Permissions = permissions;
        }

        public static User FromTableRow(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Input value cannot be null or empty.", nameof(value));

            string[] parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new FormatException("Input value must contain at least an ID and an email.");

            if (!int.TryParse(parts[0], out int id))
                throw new FormatException("The first part of the input must be a valid integer ID.");

            string email = parts[1];
            List<string> permissions = parts.Length > 2 ? parts.Skip(2).ToList() : new List<string>();

            return new User(id, email, permissions);
        }
    }
}
