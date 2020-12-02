using PropertyChanged;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public static class Notify
    {
        public static bool WindowIsActive { get; set; }

        public static async void NewMessage(string userLogin, string text)
        {
            if (!WindowIsActive)
                await Notification.Notification.Manager.Show(
                    $"{(userLogin.Length > 10 ? string.Concat(userLogin.Remove(10, userLogin.Length - 10), "...") : userLogin)} : ",
                    $"\"{(text.Length > 10 ? string.Concat(text.Remove(10, text.Length - 10), "...") : text)}\"");
        }
    }
}