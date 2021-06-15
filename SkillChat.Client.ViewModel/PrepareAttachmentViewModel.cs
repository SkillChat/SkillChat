using System;
using System.IO;
using PropertyChanged;
using SkillChat.Interface.Extensions;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class PrepareAttachmentViewModel
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public long Size { get; set; }

        public string SizeName => Size.SizeCalculating();

        public string Extensions => Path.GetExtension(FileName).Replace(".", "").ToUpper();
    }
}