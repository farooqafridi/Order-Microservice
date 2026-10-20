namespace CartService.Dto
{
    public class CartDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        //public Guid CustomerId { get; set; }

        public string CustomerId { get; set; } = string.Empty;
    }
}
