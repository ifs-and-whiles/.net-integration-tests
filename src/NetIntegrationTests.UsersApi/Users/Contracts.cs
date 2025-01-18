namespace NetIntegrationTests.UsersApi.Users;

public static class Contracts
{
    public static class Users
    {
        public static class V1
        {
            public static class IncrementExpensesCount
            {
                public class Request
                {
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
                    public string Name { get; set; }
                    public Guid Id { get; set; }
                    public int ExpensesCount { get; set; }
                    public int MaxExpenseCount { get; set; }
                }
            }
            
            public static class Create
            {
                public class Request
                {
                    public string Name { get; set; }
                }
                
                public class Response
                {
                    public Guid Id { get; set; }
                }
            }
        }
    }
}