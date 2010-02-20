using System.Collections.Generic;

namespace ServerFileBrowser.Models {
    public class FilesModel {
        public string CurrentDirectory { get; set; }
        public ICollection<string> Files { get; set; }
        public ICollection<string> Directories { get; set; }
    }
}