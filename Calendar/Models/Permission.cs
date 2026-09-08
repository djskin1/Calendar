namespace Calendar.Models
{
    public class Permission
    {
        public int Id { get; set; }

        public string Code { get; set; } = "";

        // Deze verwijst naar een language-resource.
        public string NameResourceKey { get; set; } = "";

        public ICollection<RolePermission> RolePermissions { get; set; }
            = new List<RolePermission>();
    }
}