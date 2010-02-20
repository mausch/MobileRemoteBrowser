using System.IO;
using System.Web.Mvc;
using ServerFileBrowser.Models;

namespace ServerFileBrowser {
    public static class UrlHelperExtensions {
        public static string FileAction(this UrlHelper helper, string currentDirectory, FileModel f) {
            if (f.Type == FileType.Dir)
                return helper.Action("Folder", new {path = Path.Combine(currentDirectory ?? "", f.Name)});
            return helper.Action("Run", new {path = currentDirectory, file = f.Name});
        }
    }
}