

namespace LaborDAL.Entities
{
    public class Coversation
    {
        public int Id { get; set; }
        public string UserId1 { get; set; }
        public string UserId2 { get; set; }
            public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public List<ChatUsers> ChatUsers { get; set; }= new List<ChatUsers>();
        public bool IsOnline { get; set; }

    }
}
