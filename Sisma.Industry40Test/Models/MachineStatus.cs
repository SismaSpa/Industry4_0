using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sisma.Industry40Test.Models
{
    /// <summary>
    /// Generic machine status message model
    /// </summary>
    public class MachineStatus
    {
        #region properties

        [JsonProperty("status")]
        public int Status { get; set; }
        
        [JsonProperty("ts")]
        public string TimeStamp { get; set; }

        #endregion

    }
}
