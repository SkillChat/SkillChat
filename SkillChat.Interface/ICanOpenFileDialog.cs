using System.Threading.Tasks;

namespace SkillChat.Interface
{
    public interface ICanOpenFileDialog
    {
        Task<string[]> Open();
    }
}
