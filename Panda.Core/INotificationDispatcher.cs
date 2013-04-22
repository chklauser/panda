using System;
using System.Threading.Tasks;

namespace Panda
{
    public interface INotificationDispatcher
    {
        bool CheckAccess();
        Task BeginInvoke(Delegate handler, params object[] args);
    }
}