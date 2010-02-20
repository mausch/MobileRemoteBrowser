using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using De.Mud.Telnet;
using MvcContrib.Pagination;
using Net.Graphite.Telnet;
using ServerFileBrowser.Models;
using Winista.Mime;

namespace ServerFileBrowser.Controllers {
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
            if (vlcProc != null && !vlcProc.HasExited)
                vlcProc.Kill();
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
            RunVLC();
            const int width = 640; // 752
            const int height = 360; // 423
            Guid vodId = Guid.NewGuid();
            var telnet = new TelnetWrapper();
            telnet.DataAvailable += telnet_DataAvailable;
            var mre = new ManualResetEvent(false);
            HttpContext.Items["telnet"] = mre;
            telnet.Connect("localhost", 4212);
            if (!telnet.Connected)
                throw new Exception("Telnet connection to VLC failed");
            telnet.Receive();
            mre.WaitOne();
            mre.Reset();
            telnet.Send("admin" + telnet.CRLF); // password
            telnet.Receive();
            mre.WaitOne();
            mre.Reset();
            telnet.Send(string.Format("new {0} vod enabled", vodId) + telnet.CRLF);
            telnet.Receive();
            mre.WaitOne();
            mre.Reset();
            telnet.Send(string.Format("setup {0} input \"{1}\"", vodId, Path.Combine(path, file)) + telnet.CRLF);
            telnet.Receive();
            mre.WaitOne();
            mre.Reset();
            telnet.Send("setup {0} output #transcode{$t}"
                            .Replace("{0}", vodId.ToString())
                            .Replace("$t", ConfigurationManager.AppSettings["transcoderSettings"])
                            .Replace("$w", width.ToString())
                            .Replace("$h", height.ToString()) + telnet.CRLF);
            telnet.Receive();
            mre.WaitOne();
            mre.Reset();
            telnet.Send("quit" + telnet.CRLF);

            return Redirect(string.Format("rtsp://{0}/{1}", Request.Url.Host, vodId));
        }

        private void telnet_DataAvailable(object sender, DataAvailableEventArgs e) {
            var mre = (ManualResetEvent) HttpContext.Items["telnet"];
            var data = e.Data;
            Console.WriteLine(data);
            mre.Set();
        }

        public ActionResult Run(string path, string file) {
            if (IsVideo(file))
                return Video(path, file);
            string f = Path.Combine(path, file);
            return File(new FileStream(f, FileMode.Open), GetMimeType(file));
        }

        private string GetMimeType(string filename) {
            var mt = new MimeTypes(Server.MapPath("~/mime-types.xml"));
            MimeType mime = mt.GetMimeType(filename);
            if (mime != null)
                return mime.Name;
            return "application/octet-stream";
        }
    }
}