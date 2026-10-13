namespace OrderService.Models
{
    public enum OrderStatus
    {
        PendingPayment = 1,
        Paid = 2,
        ReadyForDispatch = 3,
        Dispatched = 4,
        PaymentFailed = 5,
        Cancelled = 6
    }
}
