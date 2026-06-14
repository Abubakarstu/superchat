using Domain.Entities.WebWidget;

namespace Domain.Interfaces;

public interface IWebWidgetRepository
{
    Task<WebWidget?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<WebWidget>> GetAllAsync(CancellationToken ct = default);
    void Add(WebWidget widget);
    void Update(WebWidget widget);
    void Delete(WebWidget widget);
}
