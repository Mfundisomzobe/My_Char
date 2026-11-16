namespace My_Char.Models.Entities
{
    public class BlockedSlot
    {
        public int Id { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public string? Reason { get; set; }

    }
}
