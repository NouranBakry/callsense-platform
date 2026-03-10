using CallSense.Intake.Domain.Entities;
using CallSense.SharedKernel;

namespace CallSense.Intake.Application.Services;

public interface ICallService
{
    Task<Result<Guid>> UploadAsync(UploadCallRequest request, CancellationToken cancellationToken = default);
    Task<CallRecord?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
}
