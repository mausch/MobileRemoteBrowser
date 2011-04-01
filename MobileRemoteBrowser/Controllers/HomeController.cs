using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using MobileRemoteBrowser.Models;
using MvcContrib.Pagination;
using Winista.Mime;

namespace MobileRemoteBrowser.Controllers {
    [HandleError]
    public class HomeController : Controller {
        private static Process vlcProc;

        private string[] VideoExtensions {
            get { return ConfigurationManager.AppSettings["videoExtensions"].Split(';'); }
        }

        private int PageSize {
            get { return Convert.ToInt32(ConfigurationManager.AppSettings["pageSize"]); }
        }

        private int VideoWidth {
            get { return Convert.ToInt32(ConfigurationManager.AppSettings["width"]); }
        }

        private int VideoHeight {
            get { return Convert.ToInt32(ConfigurationManager.AppSettings["height"]); }
        }

        private string TranscoderSettings {
            get {
                return ConfigurationManager.AppSettings["transcoderSettings"]
                    .Replace("$w", VideoWidth.ToString())
                    .Replace("$h", VideoHeight.ToString());
            }
        }

        public ActionResult Index() {
            DriveInfo[] drives = DriveInfo.GetDrives();
            return View("Folder", new FilesModel {
                CurrentDirectory = null,
                Files = drives.Select(d => new FileModel {
                    Type = FileType.Dir,
                    Name = d.RootDirectory.Name
                }).AsPagination(1, drives.Length),
            });
        }

        public ActionResult Folder(string path, int? page) {
            if (path == null)
                return Index();
            page = page ?? 1;
            IEnumerable<FileModel> dirs = Dir.GetDirectories(path)
                .Select(x => new FileModel {Name = Path.GetFileName(x), Type = FileType.Dir});
            IEnumerable<FileModel> files = Dir.GetFiles(path)
                .Select(x => new FileModel {Name = Path.GetFileName(x), Type = FileType.File});
            var m = new FilesModel {
                CurrentDirectory = path,
                Files = dirs.Concat(files).AsPagination(page.Value, PageSize),
            };
            return View(m);
        }

        public ActionResult KillVLC() {
            KillVLCProc();
            return Redirect(Request.UrlReferrer.ToString());
        }

        public static void KillVLCProc() {
            if (vlcProc != null && !vlcProc.HasExited) {
                vlcProc.Kill();
                vlcProc = null;
            }
        }

        private bool IsVideo(string filename) {
            string ext = Path.GetExtension(filename).ToLowerInvariant();
            return VideoExtensions.Any(e => ext == "." + e.ToLowerInvariant());
        }

        private void RunVLC() {
            if (vlcProc != null)
                return;
            string exe = Server.MapPath("~/vlc/vlc.exe");
            vlcProc = Process.Start(exe, "-I telnet --rtsp-host 0.0.0.0:554");
        }

        public ActionResult Video(string path, string file) {
            KillVLCProc();
            RunVLC();
            const int width = 640; // 752
            const int height = 360; // 423
            Guid vodId = Guid.NewGuid();
            using (var telnet = new Telnet()) {
                telnet.Connect("127.0.0.1", 4212);
                telnet.Send("admin"); // password
                telnet.Receive();
                telnet.Send(string.Format("new {0} broadcast enabled", vodId));
                telnet.Receive();
                telnet.Send(string.Format("setup {0} input \"{1}\"", vodId, Path.Combine(path, file)));
                telnet.Receive();
                //telnet.Send(string.Format("setup {0} mux mov", vodId)); // mp4 mp2t mp2p ts ps mp2v mp4v avi asf
                telnet.Send("setup {0} output #transcode{$t}:gather:rtp{mp4a-latm,sdp=rtsp://0.0.0.0/{0}.sdp}"
                                .Replace("{0}", vodId.ToString())
                                .Replace("$t", TranscoderSettings));
                telnet.Receive();
                telnet.Send(string.Format("control {0} play", vodId));
                telnet.Receive();
            }

            return Redirect(string.Format("rtsp://{0}/{1}.sdp", Request.Url.Host, vodId));
        }

        public ActionResult Run(string path, string file) {
            if (IsVideo(file))
                return Video(path, file);
            string f = Path.Combine(path, file);
            var mime = GetMimeType(file);
            return File(new FileStream(f, FileMode.Open), mime);
        }

        private string GetMimeType(string filename) {
            var mt = new MimeTypes(Server.MapPath("~/mime-types.xml"));
            MimeType mime = mt.GetMimeType(filename.ToLowerInvariant());
            if (mime != null)
                return mime.Name;
            return "application/octet-stream";
        }
    }
}