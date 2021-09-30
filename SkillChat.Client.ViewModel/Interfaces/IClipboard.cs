using System.Threading.Tasks;

namespace SkillChat.Client.ViewModel.Interfaces
{
    public interface IClipboard
    {
        Task SetTextAsync(string text);
    }
}
