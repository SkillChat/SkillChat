using PropertyChanged;
using ServiceStack;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class SendAttachmentsViewModel
    {
        public bool IsOpen { get; set; }
        public List<PrepareAttachmentViewModel> Attachments { get; set; }

        private List<string> AttachmentsPath { get; set; }
        private string MessageText { get; set; }
        private readonly IJsonServiceClient _serviceClient;
        private IChatHub _hub;
        private IMapper _mapper;

        public SendAttachmentsViewModel(IJsonServiceClient serviceClient)
        {
            IsOpen = false;
            MessageText = string.Empty;

            _serviceClient = serviceClient;
            _mapper = Locator.Current.GetService<IMapper>();
        }

        public void SetChatHub(IChatHub chatHub) => _hub = chatHub;

        public async Task Open(IEnumerable<string> attachments)
        {
            AttachmentsPath = attachments.ToList();
            Attachments = await CreateAttachmentList(attachments.ToList());

            if (attachments.Any()) IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
        }

        public async Task SendMessage()
        {
            var uploadedAttachment = UploadAttachment();
            var mw = Locator.Current.GetService<MainWindowViewModel>();
            if (uploadedAttachment != null && uploadedAttachment.Count > 0)
            {
                var chatId = GetChatId();
                var attachmentDisplayMold =
                    uploadedAttachment
                        .Select(s => _mapper.Map<AttachmentHubMold>(s)).ToList();
                MessageText = MessageText.Trim(); //Удаление пробелов в начале и конце сообщения
                var IdReplyMes = mw.SelectedQuotedMessage==null? "" : mw.SelectedQuotedMessage.Id;
                await _hub.SendMessage(new HubMessage(chatId, MessageText, attachmentDisplayMold, IdReplyMes));
                mw.CancelQuoted();
                IsOpen = false;
                MessageText = string.Empty;
            }
        }

        private async Task<List<PrepareAttachmentViewModel>> CreateAttachmentList(List<string> data)
        {
            var result = new List<PrepareAttachmentViewModel>();

            foreach (var path in data)
            {
                var fileInfo = new FileInfo(path);

                result.Add(new PrepareAttachmentViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = fileInfo.Name,
                    Size = fileInfo.Length,
                });
            }

            return result;
        }

        private List<AttachmentMold> UploadAttachment()
        {
            var response = new SetAttachment();
            var result = new List<AttachmentMold>();

            Task.WaitAll(
                AttachmentsPath.Select(attachment => Task.Run(() =>
                {
                    var reqestResult =
                        _serviceClient
                            .PostFileWithRequest<AttachmentMold>(File.OpenRead(attachment), new FileInfo(attachment).Name, response);

                    result.Add(reqestResult);
                })).ToArray());

            return result;
        }

        private string GetChatId() => Locator.Current.GetService<MainWindowViewModel>()?.ChatId;
    }
}
