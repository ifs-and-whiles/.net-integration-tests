namespace NetIntegrationTests.Expenses;

public static class Contracts
{
    public static class Expenses
    {
        public static class V1
        {
            public static class Events
            {
                public class ExpenseCreatedEvent
                {
                    public static string ExchangeName { get; } = "NetIntegrationTests.Expenses:Contracts-Expenses-V1-Events-ExpenseCreatedEvent";
                    public Guid Id { get; set; }
                    public Guid UserId { get; set; }
                }

            }
            public static class Get
            {
                public class Request
                {
                    public Guid Id { get; set; }
                }

                public class Response
                {
                    public Guid Id { get; set; }
                    public string Name { get; set; }
                    public decimal Amount { get; set; }
                    public Guid UserId { get; set; }
                }
            }

            public static class Create
            {
                public class Request
                {
                    public string Name { get; set; }
                    public decimal Amount { get; set; }
                    public Guid UserId { get; set; }
                }
                public class Response
                {
                    public Guid Id { get; set; }
                }
            }

            public static class Delete
            {
                public class Request
                {
                    public Guid Id { get; set; }
                }
            }
        }
    }
}