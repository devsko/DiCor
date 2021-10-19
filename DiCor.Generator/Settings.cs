using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DiCor.Generator
{
    public class Settings
    {
        public DateTime LastUpdateCheck { get; set; }
        [JsonIgnore]
        public bool CheckForUpdate { get; set; }
    }
}
