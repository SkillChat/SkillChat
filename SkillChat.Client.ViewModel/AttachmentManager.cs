using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ServiceStack;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds.Attachment;

namespace SkillChat.Client.ViewModel
{
    public class AttachmentManager
    {
        public AttachmentManager(string attachmentPath, IJsonServiceClient serviceClient)
        {
            _attachmentPath = attachmentPath;
            _serviceClient = serviceClient;
        }

        private readonly string _attachmentPath;
        private readonly IJsonServiceClient _serviceClient;

        public void OpenAttachment(string fileName)
        {
            var path = Path.Combine(_attachmentPath, fileName);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        public bool IsExistAttachment(AttachmentMold data) 
        {        
            var fileInfo = new FileInfo(Path.Combine(_attachmentPath, data.FileName));
            return fileInfo.Exists && fileInfo.Length == data.Size;
        }

        public async Task<bool> DownloadAttachment(AttachmentMold data)
        {
            try
            {
                //Тут надо убрать префикс получаемого файла
                var pref = "attachment/";
                var attachment = await _serviceClient.GetAsync(new GetAttachment { Id = data.Id.Replace(pref, string.Empty) });
                if (attachment == null) return false;

                var savePath = Path.Combine(_attachmentPath, data.FileName);
                var saveFileInfo = new FileInfo(savePath);
                if (!saveFileInfo.Directory.Exists)
                {
                    saveFileInfo.Directory.Create();
                }

                if (!string.IsNullOrEmpty(savePath))
                {
                    await using (var fileStream = File.Create(savePath, (int)attachment.Length))
                    {
                        const int bufferSize = 4194304;
                        var buffer = new byte[bufferSize];
                        attachment.Seek(0, SeekOrigin.Begin);

                        while (attachment.Position < attachment.Length)
                        {
                            var read = await attachment.ReadAsync(buffer, 0, bufferSize);
                            await fileStream.WriteAsync(buffer, 0, read);
                        }

                        await fileStream.FlushAsync();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                //TODO вывести ошибку в будущем
                return false;
            }
        }
    }
}