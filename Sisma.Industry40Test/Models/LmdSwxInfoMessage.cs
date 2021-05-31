using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sisma.Industry40Test.Models
{
    /// <summary>
    /// Info message model for LMD or SWX machines
    /// </summary>
    public class LmdSwxInfoMessage
    {
        
        #region properties

        [JsonProperty("cmd")]
        public int Cmd { get => 64; }

        [JsonProperty("message1")]
        public string Message1 { get; set; }

        [JsonProperty("message2")]
        public string Message2 { get; set; }

        #endregion

    }
}
