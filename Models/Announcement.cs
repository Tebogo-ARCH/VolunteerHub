using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("ANNOUNCEMENTS")]
public partial class Announcement
{
    [Key]
    [Column("ANN_ID")]
    public int AnnId { get; set; }

    [Column("OID")]
    public int Oid { get; set; }

    [Column("TITLE")]
    [StringLength(200)]
    [Unicode(false)]
    public string Title { get; set; } = null!;

    [Column("CONTENT")]
    [Unicode(false)]
    public string Content { get; set; } = null!;

    [Column("MID")]
    public int Mid { get; set; }

    [Column("CREATED_AT")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("Mid")]
    [InverseProperty("Announcements")]
    public virtual Manager MidNavigation { get; set; } = null!;

    [ForeignKey("Oid")]
    [InverseProperty("Announcements")]
    public virtual Organization OidNavigation { get; set; } = null!;
}
