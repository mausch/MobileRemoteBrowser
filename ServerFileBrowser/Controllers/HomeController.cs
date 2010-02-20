using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using MvcContrib.Pagination;
using ServerFileBrowser.Models;

namespace ServerFileBrowser.Controllers {
    [HandleError]
    public class HomeController : Controller {
        private const int pageSize = 50;

        public ActionResult Index(string path, int? page) {
            path = path ?? "c:\\";
            page = page ?? 1;
            var dirs = Dir.GetDirectories(path)
                .Select(x => new FileModel { Name = Path.GetFileName(x), Type = FileType.Dir });
            var files = Dir.GetFiles(path)
                .Select(x => new FileModel { Name = Path.GetFileName(x), Type = FileType.File });
            var m = new FilesModel {
                CurrentDirectory = path,
                Files = dirs.Concat(files).AsPagination(page.Value, pageSize),
            };
            return View(m);
        }

        public static Process proc;

        public ActionResult Kill() {
            KillProc();
            return Redirect(Request.UrlReferrer.ToString());
        }

        public static void KillProc() {
            if (proc != null && !proc.HasExited)
                proc.Kill();
        }

        public ActionResult Run(string path, string file) {
            var exe = Server.MapPath("/vlc/vlc.exe");
            const int width = 640; // 752
            const int height = 360; // 423
            string output = ":sout=#transcode{audio-sync,soverlay,ab=64,samplerate=44100,channels=1,acodec=mp4a,vcodec=h264,width=$w,height=$h,vfilter=\"canvas{width=$w,height=$h,aspect=16:9}\",vb=500,venc=x264{profile=baseline}}:gather:rtp{mp4a-latm,sdp=rtsp://0.0.0.0/vlc.sdp}"
                .Replace("$w", width.ToString())
                .Replace("$h", height.ToString());
            KillProc();
            proc = Process.Start(exe, string.Format("-I http \"{0}\" {1}", Path.Combine(path, file), output));
            return Redirect(string.Format("rtsp://{0}/vlc.sdp", Request.Url.Host));
        }
    }
}