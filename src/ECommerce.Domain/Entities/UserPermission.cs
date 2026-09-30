namespace ECommerce.Domain.Entities;

public class UserPermission
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }

    // true = izin ver, false = engelle (rol iznini geçersiz kılar)
    public bool IsGranted { get; set; }

    public User User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}