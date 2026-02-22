using PlatzDaemon.Models;

namespace PlatzDaemon.Services;

public interface IConfigStore
{
    BookingConfig Get();
    Task SaveAsync(BookingConfig config);
}
