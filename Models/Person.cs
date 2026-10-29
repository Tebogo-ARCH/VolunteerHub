using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("PERSONS")]
[Index("Email", Name = "UQ__PERSONS__161CF724B73E2EB6", IsUnique = true)]
[Index("UserName", Name = "UQ__PERSONS__E0B75F6FFE9F948E", IsUnique = true)]
public partial class Person
{
    [Key]
    [Column("PID")]
    public int Pid { get; set; }

    [Column("FIRST_NAME")]
    [StringLength(100)]
    [Unicode(false)]
    public string FirstName { get; set; } = null!;

    [Column("LAST_NAME")]
    [StringLength(100)]
    [Unicode(false)]
    public string LastName { get; set; } = null!;

    [Column("USER_NAME")]
    [StringLength(100)]
    [Unicode(false)]
    public string UserName { get; set; } = null!;

    [Column("EMAIL")]
    [StringLength(150)]
    [Unicode(false)]
    public string Email { get; set; } = null!;

    [Column("PHONE")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Phone { get; set; }

    [Column("ADDRESS_ID")]
    public int? AddressId { get; set; }

    [Column("JOINED_AT")]
    public DateTime? JoinedAt { get; set; }

    [Column("STATUS")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Status { get; set; }

    [Column("MUST_CHANGE_PASSWORD")]
    public bool MustChangePassword { get; set; }

    [Column("PASSWORD_HASH")]
    [StringLength(255)]
    public string? PasswordHash { get; set; }

    [ForeignKey("AddressId")]
    [InverseProperty("People")]
    public virtual Address? Address { get; set; }

    [InverseProperty("PidNavigation")]
    public virtual Manager? Manager { get; set; }

    [InverseProperty("PidNavigation")]
    public virtual Staff? Staff { get; set; }

    [InverseProperty("PidNavigation")]
    public virtual Volunteer? Volunteer { get; set; }
}
