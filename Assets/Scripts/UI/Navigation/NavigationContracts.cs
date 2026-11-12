using System.Threading;
using Cysharp.Threading.Tasks;

namespace UI
{
  public enum NavigationStatus
  {
    Succeeded,
    AlreadyCurrent,
    Empty,
    InvalidPage,
    LifecycleFailed,
    ShuttingDown
  }

  public sealed record NavigationResult(NavigationStatus Status, string Error = null)
  {
    public bool IsSuccess => Status is NavigationStatus.Succeeded or NavigationStatus.AlreadyCurrent;

    public static NavigationResult Success()
    {
      return new NavigationResult(NavigationStatus.Succeeded);
    }

    public static NavigationResult Failure(NavigationStatus status, string error)
    {
      return new NavigationResult(status, error);
    }
  }

  public interface IPageNavigator
  {
    UniTask<NavigationResult> PushAsync<TPage>(CancellationToken cancellationToken = default)
      where TPage : BasePage;

    UniTask<NavigationResult> ReplaceAsync<TPage>(CancellationToken cancellationToken = default)
      where TPage : BasePage;

    UniTask<NavigationResult> PopAsync(CancellationToken cancellationToken = default);
    UniTask<NavigationResult> ClearAsync(CancellationToken cancellationToken = default);
  }
}