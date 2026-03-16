using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "Events")]
    public class Events : Entity
    {
        public long ID { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Location { get; set; }
        public string? Program { get; set; }
        public string? LocationLink { get; set; }
        public string? Type { get; set; }
    }

    [Keyless]
    public class EventsDto
    {
        public long ID { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Location { get; set; }
        public string? Program { get; set; }
        public string? LocationLink { get; set; }
        public string? Type { get; set; }
    }

    [Keyless]
    public class EventListDto
    {
        public long ID { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Location { get; set; }
        public string? Program { get; set; }
        public string? LocationLink { get; set; }
        public string? Type { get; set; }
    }
}