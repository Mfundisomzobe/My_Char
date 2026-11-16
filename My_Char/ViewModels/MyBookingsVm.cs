namespace My_Char.ViewModels
{
    public class MyBookingsVm
    {
        public string ShopName { get; set; } = "";
        public string ShopPhone { get; set; } = "";
        public List<BookingVm> Upcoming { get; set; } = new();
        public List<BookingVm> Past { get; set; } = new();

    }
}
