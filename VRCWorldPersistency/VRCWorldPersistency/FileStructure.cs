using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VRCWorldPersistency
{
    /// <summary>
    /// Describes the JSON file structure in which the settings to all worlds are stored
    /// </summary>
    [JsonObject]
    internal class FileStructure
    {
        /// <summary>
        /// The VRChat world ID
        /// </summary>
        internal string WorldID { set; get; }
        /// <summary>
        /// A world can have a list of values that are stored
        /// </summary>
        internal List<PersistentWorldValue> PersistentWorldValues { set; get; }
    }
    /// <summary>
    /// Each setting is stored with an individual name and value.
    /// A world can request to store and load those values as well as 
    /// deleting or changing them
    /// </summary>
    internal class PersistentWorldValue
    {
        /// <summary>
        /// To identify a certain setting
        /// </summary>
        internal string Key { set; get; }
        /// <summary>
        /// The data that is being stored
        /// </summary>
        internal string Value { set; get; }
    }
}
