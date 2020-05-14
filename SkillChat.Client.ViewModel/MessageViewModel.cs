using System;
using System.Globalization;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SkillChat.Client.ViewModel
{
    public class MessageViewModel: ReactiveObject
    {
        public MessageViewModel()
        {
            this.WhenAnyValue(x => x.PostTime).Subscribe(t => Time = "[" + t.ToLocalTime().ToString("t") + "] ");
        }

        public string Id { get; set; }

        public string UserLogin { get;set; }

        [Reactive]
        public string Text { get;set; }

        [Reactive]
        public DateTimeOffset PostTime { get;set; }
        
        [Reactive]
        public string Time { get;set; }
    }
}