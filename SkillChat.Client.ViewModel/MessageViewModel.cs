using System;
using PropertyChanged;
using ReactiveUI;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MessageViewModel
    {
        public MessageViewModel()
        {
            this.WhenAnyValue(x => x.PostTime).Subscribe(t =>
            {
                var local = t.ToLocalTime();
                if (local < DateTimeOffset.Now.Date)
                {
                    Time = local.ToString("g");
                }
                else
                {
                    Time = local.ToString("t");
                }
            });
        }

        public string Id { get; set; }

        public string UserLogin { get; set; }

        public string Text { get; set; }

        public DateTimeOffset PostTime { get; set; }

        public string Time { get; set; }
    }
}