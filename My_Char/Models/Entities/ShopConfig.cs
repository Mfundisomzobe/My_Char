namespace My_Char.Models.Entities
{
    public class ShopConfig
    {
        public int Id { get; set; }
        public string Name { get; set; } = "My Salon";
        public string OwnerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Slug { get; set; } = "my-salon";
        public int OpenHour { get; set; } = 8;
        public int CloseHour { get; set; } = 18;
        public int SlotStepMinutes { get; set; } = 30;
        public int NoShowLimit { get; set; } = 3;
        public string? WhatsAppBroadcastList { get; set; }
        public string? GalleryJson { get; set; }

    }
}
