using PropertyChanged;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public static class Notify
    {
        public static void NewMessage(string userLogin, string text)
        {
            Notification.Manager.Show(
                    $"{(userLogin.Length > 10 ? string.Concat(userLogin.Remove(10, userLogin.Length - 10), "...") : userLogin)} : ",
                    $"\"{(text.Length > 10 ? string.Concat(text.Remove(10, text.Length - 10), "...") : text)}\"");
        }
    }
}