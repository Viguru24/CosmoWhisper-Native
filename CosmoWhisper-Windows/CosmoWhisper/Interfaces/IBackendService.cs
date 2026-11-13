using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace CosmoWhisper.Interfaces
{
    public interface IBackendService
    {
        void UpdateBaseAddress();
        Task<(bool success, string message)> Login();
        Task<(bool success, string message)> RequestMagicCode(string email);
        Task<(bool success, string message)> VerifyMagicCode(string email, string code);
        Task<Managers.SyncResult> SyncStatus();
        Task<bool> ReportUsage(long durationMs);
        Task<Managers.SupportActivityResponse?> GetSupportTicketActivity();
        Task<Stream?> GetStatusStream(CancellationToken cancellationToken = default);
        Task<bool> SignOut();
    }
}
