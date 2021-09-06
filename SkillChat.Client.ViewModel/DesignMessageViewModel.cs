using System;
using System.Collections.Generic;
using SkillChat.Server.ServiceModel.Molds.Attachment;

namespace SkillChat.Client.ViewModel
{
    public class DesignMessageViewModel : MessageViewModel
    {
        public DesignMessageViewModel()
        {

            UserNickname = "kibnet";
            Text = "Привет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\n";
            PostTime = DateTimeOffset.Now.AddDays(-1);
            Id = "1";
            ShowNickname = true;
            IsMyMessage = false;
            LastEditTime = DateTimeOffset.Now;
            Attachments = new List<AttachmentMessageViewModel>
            {
                new AttachmentMessageViewModel(new AttachmentMold
                {
                    FileName = "file.txt",
                    Size = 12409814809,
                    Id = "123",
                    UploadDateTime = DateTimeOffset.Now
                }, new AttachmentManager("", null))
            };

        }
    }
}