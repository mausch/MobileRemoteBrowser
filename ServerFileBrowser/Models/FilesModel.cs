using MvcContrib.Pagination;

namespace ServerFileBrowser.Models {
    public class FilesModel {
        public string CurrentDirectory { get; set; }
        public IPagination<FileModel> Files { get; set; }
    }
}