using MvcContrib.Pagination;

namespace MobileRemoteBrowser.Models {
    public class FilesModel {
        public string CurrentDirectory { get; set; }
        public IPagination<FileModel> Files { get; set; }
    }
}