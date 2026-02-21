using PatentsViewAPI.Models.Request;

namespace PatentsViewAPI.Services.Interfaces
{
    public interface IPatentsViewService
    {
        /// <summary>
        /// Searches patents based on keywords and optional application
        /// </summary>
        /// <param name="request">The analize request containing keywords</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Raw JSON response from PatentsView API</returns>
        Task<string> AnalizePatentsAsync(AnalizeRequest request, CancellationToken cancellationToken = default);


        /// <summary>
        /// Direct search with full control over query parameters
        /// </summary>
        /// <param name="request">The direct search request with q, f, s, o parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Raw JSON response from PatentsView API</returns>
        Task<string> SearchDirectAsync(DirectSearchRequest request, CancellationToken cancellationToken = default);
    }
}
