using Core.DTO.Email;

namespace Core.Interface.Service.Email
{
    public interface IDeadLetterSink
    {
        Task WriteAsync(DeadLetterEmail item, CancellationToken ct = default);
    }
}
