namespace RealTimeChat.Domain.Entities
{
    public sealed class Room
    {
        public const int MaxNameLength = 100;
        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;

        private Room()
        {
        }

        public Room(string name)
        {
            Rename(name);
        }


        private void Rename(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            name = name.Trim();

            if (name.Length > MaxNameLength) {
                throw new ArgumentException($"Room name cannot exceed {MaxNameLength} characters.", nameof(name));
            }

            Name = name;
        }
    }
}
