using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("VOLUNTEER_SKILLS")]
[Index("Vid", "SkillId", Name = "UQ_VOLUNTEER_SKILL", IsUnique = true)]
public partial class VolunteerSkill
{
    [Key]
    [Column("VSID")]
    public int Vsid { get; set; }

    [Column("SKILL_ID")]
    public int SkillId { get; set; }

    [Column("VID")]
    public int Vid { get; set; }

    [ForeignKey("SkillId")]
    [InverseProperty("VolunteerSkills")]
    public virtual Skill Skill { get; set; } = null!;

    [ForeignKey("Vid")]
    [InverseProperty("VolunteerSkills")]
    public virtual Volunteer VidNavigation { get; set; } = null!;
}
