namespace ZKTecoApi.DTOs.Response
{
    public class UserInfoResponse
    {
        public string EnrollNumber { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public long? CardNumber { get; set; }
        public int Privilege { get; set; }
        public bool Enabled { get; set; }
    }
}
