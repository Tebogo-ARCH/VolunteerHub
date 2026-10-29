using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("ADDRESS")]
public partial class Address
{
    [Key]
    [Column("ADDRESS_ID")]
    public int AddressId { get; set; }

    [Column("STREET_ADDRESS")]
    [StringLength(200)]
    [Unicode(false)]
    public string StreetAddress { get; set; } = null!;

    [Column("SUBURB")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Suburb { get; set; }

    [Column("CITY")]
    [StringLength(100)]
    [Unicode(false)]
    public string City { get; set; } = null!;

    [Column("PROVINCE")]
    [StringLength(100)]
    [Unicode(false)]
    public string Province { get; set; } = null!;

    [Column("POSTAL_CODE")]
    [StringLength(10)]
    [Unicode(false)]
    public string? PostalCode { get; set; }

    [InverseProperty("Address")]
    public virtual ICollection<Organization> Organizations { get; set; } = new List<Organization>();

    [InverseProperty("Address")]
    public virtual ICollection<Person> People { get; set; } = new List<Person>();
}
