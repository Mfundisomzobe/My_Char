namespace My_Char.ViewModels
{
    public class PublicBookingViewModel
    {
        public string ShopName { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Slug { get; set; } = "";
        public List<ServiceDto> Services { get; set; } = new();

    }
}
