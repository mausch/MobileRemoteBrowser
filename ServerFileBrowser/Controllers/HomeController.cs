﻿using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using MvcContrib.Pagination;
using ServerFileBrowser.Models;
using Winista.Mime;

namespace ServerFileBrowser.Controllers {
    [HandleError]
    public class HomeController : Controller {
        private const int pageSize = 50;
        public static Process proc;

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
            IEnumerable<FileModel> dirs = Dir.GetDirectories(path)
                .Select(x => new FileModel {Name = Path.GetFileName(x), Type = FileType.Dir});
            IEnumerable<FileModel> files = Dir.GetFiles(path)
                .Select(x => new FileModel {Name = Path.GetFileName(x), Type = FileType.File});
            var m = new FilesModel {
                CurrentDirectory = path,
                Files = dirs.Concat(files).AsPagination(page.Value, pageSize),
            };
            return View(m);
        }

        public ActionResult Kill() {
            KillProc();
            return Redirect(Request.UrlReferrer.ToString());
        }

        public static void KillProc() {
            if (proc != null && !proc.HasExited)
                proc.Kill();
        }

        private string[] VideoExtensions {
            get {
                return ConfigurationManager.AppSettings["videoExtensions"].Split(';');
            }
        }

        private bool IsVideo(string filename) {
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            return VideoExtensions.Any(e => ext == "." + e.ToLowerInvariant());
        }

        public ActionResult Video(string path, string file) {
            string exe = Server.MapPath("~/vlc/vlc.exe");
            const int width = 640; // 752
            const int height = 360; // 423
            string output = ":sout=#transcode{$t}:gather:rtp{mp4a-latm,sdp=rtsp://0.0.0.0/vlc.sdp}"
                .Replace("$t", ConfigurationManager.AppSettings["transcoderSettings"])
                .Replace("$w", width.ToString())
                .Replace("$h", height.ToString());
            KillProc();
            proc = Process.Start(exe, string.Format("-I http \"{0}\" {1}", Path.Combine(path, file), output));
            return Redirect(string.Format("rtsp://{0}/vlc.sdp", Request.Url.Host));
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