namespace ZKTecoApi.DTOs.Request
{
    public class UserUpdateRequest
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public long? CardNumber { get; set; }
        public int? Privilege { get; set; }
        public bool? Enabled { get; set; }
    }
}
