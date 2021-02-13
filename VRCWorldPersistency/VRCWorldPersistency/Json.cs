using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRCWorldPersistency
{
    internal class Json
    {
        private const string JSON_FILE_NAME = "PersistentWorldData.json";
        internal FileStructure ReadJsonFile()
        {
            if (!JsonFileExists())
            {
                return new FileStructure();
            }
            else
            {
                string path = GetJsonFilePath();
                string fileContent = System.IO.File.ReadAllText(path);
                FileStructure fileStructure = Newtonsoft.Json.JsonConvert.DeserializeObject<FileStructure>(fileContent);
                return fileStructure;
            }
        }
        internal void SaveJsonFile(FileStructure fileStructure)
        {
            string path = GetJsonFilePath();
            string fileContent = Newtonsoft.Json.JsonConvert.SerializeObject(fileStructure);
            System.IO.File.WriteAllText(path, fileContent);
        }
        /// <summary>
        /// Checks if the json file exists
        /// </summary>
        private bool JsonFileExists()
        {
            string path = GetJsonFilePath();
            return System.IO.File.Exists(path);
        }
        /// <summary>
        /// Returns the path to the json file
        /// </summary>
        private string GetJsonFilePath()
        {
            return System.IO.Path.Combine(Environment.CurrentDirectory, JSON_FILE_NAME);
        }
    }
}
