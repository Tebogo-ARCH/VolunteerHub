using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("SKILLS")]
public partial class Skill
{
    [Key]
    [Column("SKILL_ID")]
    public int SkillId { get; set; }

    [Column("SKILL_NAME")]
    [StringLength(100)]
    [Unicode(false)]
    public string SkillName { get; set; } = null!;

    [Column("SKILL_DESCRIPTION")]
    [StringLength(500)]
    [Unicode(false)]
    public string? SkillDescription { get; set; }

    [InverseProperty("Skill")]
    public virtual ICollection<VolunteerSkill> VolunteerSkills { get; set; } = new List<VolunteerSkill>();
}
