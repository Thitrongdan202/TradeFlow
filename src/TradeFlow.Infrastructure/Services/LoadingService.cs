using System;
using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Interfaces;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Triển khai dịch vụ quản lý trạng thái màn hình tải toàn cục (Scoped per Blazor circuit/connection).
/// Có bộ đếm tác vụ lồng nhau và cơ chế timeout an toàn (tối đa 3 giây) chống treo giao diện.
/// </summary>
public class LoadingService : ILoadingService, IDisposable
{
    private readonly int _safetyTimeoutMs;
    private int _loadingCount;
    private string _loadingText = "Đang tải...";
    private CancellationTokenSource? _safetyCts;

    public LoadingService(int safetyTimeoutMs = 3000)
    {
        _safetyTimeoutMs = safetyTimeoutMs;
    }

    public bool IsLoading => _loadingCount > 0;
    public string LoadingText => _loadingText;

    public event Action? OnChange;

    public void Show(string? text = null)
    {
        _loadingCount++;
        _loadingText = string.IsNullOrWhiteSpace(text) ? "Đang tải..." : text.Trim();

        // Safety fallback timer: auto-dismiss after maximum 3 seconds
        ResetSafetyTimer();

        NotifyStateChanged();
    }

    public void Hide()
    {
        CancelSafetyTimer();
        if (_loadingCount > 0)
        {
            _loadingCount--;
        }
        if (_loadingCount == 0)
        {
            _loadingText = "Đang tải...";
        }
        NotifyStateChanged();
    }

    public Task ShowAsync(string? text = null)
    {
        Show(text);
        return Task.CompletedTask;
    }

    public Task HideAsync()
    {
        Hide();
        return Task.CompletedTask;
    }

    private void ResetSafetyTimer()
    {
        CancelSafetyTimer();
        _safetyCts = new CancellationTokenSource();
        var token = _safetyCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_safetyTimeoutMs, token);
                if (!token.IsCancellationRequested && _loadingCount > 0)
                {
                    _loadingCount = 0;
                    _loadingText = "Đang tải...";
                    NotifyStateChanged();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when Hide() is called normally
            }
        }, token);
    }

    private void CancelSafetyTimer()
    {
        if (_safetyCts != null)
        {
            try
            {
                _safetyCts.Cancel();
                _safetyCts.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
            _safetyCts = null;
        }
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }

    public void Dispose()
    {
        CancelSafetyTimer();
    }
}
