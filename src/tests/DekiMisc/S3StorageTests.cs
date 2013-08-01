using System;
using System.IO;
using System.Linq;
using System.Text;
using MindTouch.Deki.Data;
using MindTouch.Deki.Storage;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Dream.Test.Mock;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;
using MindTouch.IO;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class S3StorageTests {
        private const string PREFIX = "prefix";
        private const string BUCKET = "bucket";
        private S3Storage _s3Storage;
        private string _tempFilename;
        private int _cacheSeconds = 60;
        private FileStream _filestream;
        private string _tempDirectory;

        [SetUp]
        public void Setup() {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "s3_cache_" + XUri.EncodeSegment(PREFIX));
        }

        private S3Storage Storage {
            get {
                if(_s3Storage != null) {
                    return _s3Storage;
                }
                _s3Storage = new S3Storage(
                    new XDoc("config")
                        .Elem("publickey", "publickey")
                        .Elem("privatekey", "privatekey")
                        .Elem("bucket", BUCKET)
                        .Elem("prefix", PREFIX)
                        .Elem("cachetimeout", _cacheSeconds),
                    LogUtils.CreateLog<S3Storage>()
                 );
                return _s3Storage;
            }
        }

        [TearDown]
        public void Teardown() {
            MockPlug.DeregisterAll();
            if(!string.IsNullOrEmpty(_tempFilename)) {
                _filestream.Dispose();
                File.Delete(_tempFilename);
            }
            if(_s3Storage != null) {
                _s3Storage.Dispose();
            }
            _s3Storage = null;
        }

        [Test]
        public void Storage_wipes_cache_directory_on_start() {
            Directory.CreateDirectory(_tempDirectory);
            var cacheFile = Path.Combine(_tempDirectory, "foo");
            File.WriteAllText(cacheFile, "bar");
            Assert.IsTrue(File.Exists(cacheFile), "manually created cache file disappeared");
            var storage = Storage;
            Assert.IsFalse(File.Exists(cacheFile), "manually created cache file still there after firing up storage");
        }

        [Test]
        public void Storage_wipes_cache_directory_on_shutdown() {
            Assert.IsFalse(Directory.Exists(_tempDirectory), "temp dir already exists");
            var storage = Storage;
            Assert.IsTrue(Directory.Exists(_tempDirectory), "temp dir did not get created when storage was fired up");
            var cacheFile = Path.Combine(_tempDirectory, "foo");
            File.WriteAllText(cacheFile, "bar");
            Assert.IsTrue(File.Exists(cacheFile), "manually created cache file is missing");
            _s3Storage = null;
            storage.Dispose();
            Assert.IsFalse(Directory.Exists(_tempDirectory), "temp dir is still there after dispose");
        }

        [Test]
        public void Cached_items_expire() {
            _cacheSeconds = 2;
            MockPlug.Setup(new XUri("http://s3.amazonaws.com").At(BUCKET));
            var attachment = CreateAttachmentForUpload();
            var storage = Storage;
            Assert.IsFalse(Directory.GetFiles(_tempDirectory).Any(), "there's already files in the temp dir");
            storage.PutFile(attachment.Item1, SizeType.ORIGINAL, attachment.Item2);
            Assert.AreEqual(1, Directory.GetFiles(_tempDirectory).Length, "Unexpected number of cache files after put");
            Assert.IsTrue(Wait.For(() => !Directory.GetFiles(_tempDirectory).Any(), 120.Seconds()), "cache didn't empty in time");
        }

        [Test]
        public void Can_get_file() {
            var attachment = CreateAttachmentForDownload();
            var s3Filename = GetS3Filename(attachment);
            MockPlug.Setup(new XUri("http://s3.amazonaws.com").At(BUCKET))
                .Verb("GET")
                .At(s3Filename)
                .Returns(DreamMessage.Ok(MimeType.TEXT,"foobar"))
                .ExpectCalls(Times.Once());
            var info = Storage.GetFile(attachment, SizeType.ORIGINAL, false);
            Assert.AreEqual("foobar",Encoding.ASCII.GetString(info.Stream.ReadBytes(info.Length)));
            MockPlug.VerifyAll(1.Seconds());
        }

        [Test]
        public void Put_file_is_uploaded() {
            var attachment = CreateAttachmentForUpload();
            var s3Filename = GetS3Filename(attachment.Item1);
            MockPlug.Setup(new XUri("http://s3.amazonaws.com").At(BUCKET)).Verb("PUT").At(s3Filename).ExpectCalls(Times.Once());
            Storage.PutFile(attachment.Item1, SizeType.ORIGINAL, attachment.Item2);
            MockPlug.VerifyAll(1.Seconds());
        }

        [Test]
        public void Put_file_is_cached() {
            var attachment = CreateAttachmentForUpload();
            var s3Filename = GetS3Filename(attachment.Item1);
            MockPlug.Setup(new XUri("http://s3.amazonaws.com").At(BUCKET)).Verb("PUT").At(s3Filename).ExpectCalls(Times.Once());
            Storage.PutFile(attachment.Item1, SizeType.ORIGINAL, attachment.Item2);
            MockPlug.VerifyAll(1.Seconds());
        }

        private Tuplet<ResourceBE, StreamInfo> CreateAttachmentForUpload() {
            _tempFilename = Path.GetTempFileName();
            using(var file = File.CreateText(_tempFilename)) {
                file.WriteLine(StringUtil.CreateAlphaNumericKey(255));
            }
            _filestream = File.OpenRead(_tempFilename);
            return new Tuplet<ResourceBE, StreamInfo>(
                new ResourceBE(ResourceBE.Type.FILE) {
                    ResourceId = 123,
                    Name = "file",
                    Content = new ResourceContentBE((uint)_filestream.Length, MimeType.TEXT) { Revision = 2 }
                },
                new StreamInfo(_filestream, _filestream.Length, MimeType.TEXT)
            );
        }

        private ResourceBE CreateAttachmentForDownload() {
            return new ResourceBE(ResourceBE.Type.FILE) {
                ResourceId = 123,
                Name = "file",
                Content = new ResourceContentBE(true) { Revision = 2 }
            };
        }


        private static string[] GetS3Filename(ResourceBE resource) {
            return new[] { PREFIX, "r" + resource.ResourceId.ToString(), (resource.Content.Revision - 1).ToString() };
        }
    }
}
