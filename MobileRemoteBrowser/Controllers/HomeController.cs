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

        public ActionResult Index() {
            var drives = DriveInfo.GetDrives();
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
            var dirs = Dir.GetDirectories(path)
                .Select(x => new FileModel {Name = Path.GetFileName(x), Type = FileType.Dir});
            var files = Dir.GetFiles(path)
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
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            return VideoExtensions.Any(e => ext == "." + e.ToLowerInvariant());
        }

        private void RunVLC() {
            if (vlcProc != null)
                return;
            var exe = Server.MapPath("~/vlc/vlc.exe");
            vlcProc = Process.Start(exe, "-I telnet --rtsp-host 0.0.0.0:554");
        }

        public ActionResult Video(string path, string file) {
            KillVLCProc();
            RunVLC();
            const int width = 640; // 752
            const int height = 360; // 423
            var vodId = Guid.NewGuid();
            using (var telnet = new Telnet()) {
                telnet.Connect("localhost", 4212);
                telnet.Send("admin"); // password
                telnet.Send(string.Format("new {0} broadcast enabled", vodId));
                telnet.Send(string.Format("setup {0} input \"{1}\"", vodId, Path.Combine(path, file)));
                //telnet.Send(string.Format("setup {0} mux mov", vodId)); // mp4 mp2t mp2p ts ps mp2v mp4v avi asf
                telnet.Send("setup {0} output #transcode{$t}:gather:rtp{mp4a-latm,sdp=rtsp://0.0.0.0/{0}.sdp}"
                                .Replace("{0}", vodId.ToString())
                                .Replace("$t", ConfigurationManager.AppSettings["transcoderSettings"])
                                .Replace("$w", width.ToString())
                                .Replace("$h", height.ToString()));
                telnet.Send(string.Format("control {0} play", vodId));
            }

            return Redirect(string.Format("rtsp://{0}/{1}.sdp", Request.Url.Host, vodId));
        }

        public ActionResult Run(string path, string file) {
            if (IsVideo(file))
                return Video(path, file);
            var f = Path.Combine(path, file);
            return File(new FileStream(f, FileMode.Open), GetMimeType(file));
        }

        private string GetMimeType(string filename) {
            var mt = new MimeTypes(Server.MapPath("~/mime-types.xml"));
            var mime = mt.GetMimeType(filename);
            if (mime != null)
                return mime.Name;
            return "application/octet-stream";
        }
    }
}