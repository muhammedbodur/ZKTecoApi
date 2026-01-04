namespace ZKTecoApi.DTOs.Request
{
    public class UserCreateRequest
    {
        public string EnrollNumber { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public long? CardNumber { get; set; }
        public int Privilege { get; set; } = 0;
        public bool Enabled { get; set; } = true;
    }
}
