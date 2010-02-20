namespace ServerFileBrowser.Models {
    public class FileModel {
        public string Type { get; set; }
        public string Name { get; set; }
    }

    public static class FileType {
        public static readonly string Dir = "dir";
        public static readonly string File = "file";
    }
}