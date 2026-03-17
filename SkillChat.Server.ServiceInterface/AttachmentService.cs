using AutoMapper;
using Microsoft.Extensions.Configuration;
using Raven.Client.Documents.Session;
using ServiceStack;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SkillChat.Server.ServiceInterface
{
    public class AttachmentService : Service
    {
        private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB
        private const int BufferSize = 4 * 1024 * 1024; // 4 MB

        public IAsyncDocumentSession RavenSession { get; set; }
        public IMapper Mapper { get; set; }
        private string pref { get; set; }
        private string dirPatch { get; set; }

        public AttachmentService(IConfiguration configuration) 
        {
            dirPatch = configuration["FilesPath"];
            pref = "attachment/";
        }

        [Authenticate]
        public async Task<Stream> Get(GetAttachment request)
        {
            ValidateFileId(request.Id);
            var filePath = Path.Combine(dirPatch, request.Id);

            if (!File.Exists(filePath))
            {
                throw HttpError.BadRequest("Error: file does not exist");
            }
           
            return File.OpenRead(filePath);
        }

        private static void ValidateFileId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || 
                id.Contains("..") || 
                id.Contains('/') || 
                id.Contains('\\') ||
                Path.GetFileName(id) != id)
            {
                throw HttpError.BadRequest("Invalid file identifier");
            }
        }

        [Authenticate]
        public async Task<AttachmentMold> Post(SetAttachment request)
        {
            var session = Request.ThrowIfUnauthorized();
            var file = Request.Files.FirstOrDefault();

            if (file == null) throw HttpError.BadRequest("Error: No file to download");

            var stream = file?.InputStream;

            if (stream.Length > MaxFileSizeBytes)
            {
                throw HttpError.BadRequest("File size exceeds the maximum allowed size");
            }

            var fileId = $"{pref}{Guid.NewGuid()}";

            var fileUpload = new Attachment()
            {
                Id = fileId,
                FileName = file?.FileName,
                SenderId = session.UserAuthId,
                Hash = SaveFile(fileId.Replace(pref, string.Empty), stream),
                UploadDateTime = DateTimeOffset.Now,
                Size = stream.Length
            };

            await RavenSession.StoreAsync(fileUpload);
            await RavenSession.SaveChangesAsync();

            var mapped = Mapper.Map<AttachmentMold>(fileUpload);
            return mapped;
        }

        private string SaveFile(string fileId, Stream stream)
        {
            var filePath = Path.Combine(dirPatch, fileId);

            try
            {
                if (!Directory.Exists(dirPatch))
                {
                    Directory.CreateDirectory(dirPatch);
                }
            }
            catch
            {
                throw HttpError.BadRequest("Error on save file");
            }

            using (var fileStream = File.Create(filePath, (int)stream.Length))
            {
                var data = new byte[BufferSize];

                stream.Seek(0, SeekOrigin.Begin);
                var hashString = string.Empty;

                while (stream.Position < stream.Length)
                {
                    var read = stream.Read(data, 0, BufferSize);
                    fileStream.Write(data, 0, read);

                    hashString += data.ToMd5Hash();
                }

                fileStream.Flush();
                return hashString;
            }
        }
    }
}
