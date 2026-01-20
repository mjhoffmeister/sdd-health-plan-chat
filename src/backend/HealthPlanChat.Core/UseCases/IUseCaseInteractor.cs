namespace HealthPlanChat.Core.UseCases;

/// <summary>
/// Use case interactor interface.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TOutput">Output type.</typeparam>
public interface IUseCaseInteractor<TRequest, TOutput>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">Request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Output.</returns>
    Task<TOutput> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
