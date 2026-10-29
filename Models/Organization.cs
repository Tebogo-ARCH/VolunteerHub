using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("ORGANIZATIONS")]
public partial class Organization
{
    [Key]
    [Column("OID")]
    public int Oid { get; set; }

    [Column("ORG_NAME")]
    [StringLength(150)]
    [Unicode(false)]
    public string OrgName { get; set; } = null!;

    [Column("DESCRIPTION")]
    [StringLength(500)]
    [Unicode(false)]
    public string? Description { get; set; }

    [Column("EMAIL")]
    [StringLength(150)]
    [Unicode(false)]
    public string? Email { get; set; }

    [Column("PHONE")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Phone { get; set; }

    [Column("ADDRESS_ID")]
    public int? AddressId { get; set; }

    [Column("MID")]
    public int Mid { get; set; }

    [ForeignKey("AddressId")]
    [InverseProperty("Organizations")]
    public virtual Address? Address { get; set; }

    [InverseProperty("OidNavigation")]
    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    [ForeignKey("Mid")]
    [InverseProperty("Organizations")]
    public virtual Manager MidNavigation { get; set; } = null!;
}
