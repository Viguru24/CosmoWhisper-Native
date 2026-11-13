using System;
using System.Threading.Tasks;

namespace CosmoWhisper.Interfaces
{
    public interface ICommandController
    {
        event Action? CommandExecuted;
        Task<bool> Handle(string text);
    }
}
