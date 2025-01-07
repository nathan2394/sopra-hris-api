
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using sopra_hris_api.Helpers;

namespace sopra_hris_api.Entities
{
    public class Entity
    {
        public DateTime? DateIn { get; set; }
        [JsonIgnore]
        public DateTime? DateUp { get; set; }
        [JsonIgnore]
        public long UserIn { get; set; }
        [JsonIgnore]
        public long UserUp { get; set; }
        [JsonIgnore]
        public bool? IsDeleted { get; set; }
        public Entity()
        {
            UserIn = 0;
            DateIn = Utility.getCurrentTimestamps();
            IsDeleted = false;
        }
    }
}