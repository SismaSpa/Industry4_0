using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sisma.Industry40Test.Utils
{
    public class JSONUtils
    {

        #region methods

        public static string SerializeJSON(object objToSerialize, out EventArgs errorEventArgs, bool indented = false)
        {
            errorEventArgs = EventArgs.Empty;
            EventArgs eea = EventArgs.Empty;

            string str = JsonConvert.SerializeObject(objToSerialize, indented ? Formatting.Indented : Formatting.None, 
                new JsonSerializerSettings
                {
                    Error = delegate (object sender, ErrorEventArgs args)
                    {
                        eea = args;
                        args.ErrorContext.Handled = true;
                    },
                });

            if (eea != EventArgs.Empty)
                errorEventArgs = eea;

            return str;
        }

        public static object DeserializeJSON(Type type, string jsonString, out EventArgs errorEventArgs)
        {
            errorEventArgs = EventArgs.Empty;
            EventArgs eea = EventArgs.Empty;

            object obj = JsonConvert.DeserializeObject(jsonString, type,
                new JsonSerializerSettings
                {
                    Error = delegate (object sender, ErrorEventArgs args)
                    {
                        eea = args;
                        args.ErrorContext.Handled = true;
                    },
                });

            if (eea != EventArgs.Empty)
                errorEventArgs = eea;

            return obj;
        } 

        #endregion

    }
}
