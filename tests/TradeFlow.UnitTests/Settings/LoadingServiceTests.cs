using System.Threading.Tasks;
using FluentAssertions;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Settings;

public class LoadingServiceTests
{
    private readonly LoadingService _sut = new();

    [Fact]
    public void InitialState_ShouldNotBeLoading_AndHaveDefaultText()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.LoadingText.Should().Be("Đang tải...");
    }

    [Fact]
    public void Show_ShouldSetIsLoadingTrue_AndCustomText_AndRaiseEvent()
    {
        bool eventRaised = false;
        _sut.OnChange += () => eventRaised = true;

        _sut.Show("Đang tải danh sách hàng hóa...");

        _sut.IsLoading.Should().BeTrue();
        _sut.LoadingText.Should().Be("Đang tải danh sách hàng hóa...");
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void Show_WithNullOrEmptyText_UsesDefaultText()
    {
        _sut.Show(null);

        _sut.IsLoading.Should().BeTrue();
        _sut.LoadingText.Should().Be("Đang tải...");
    }

    [Fact]
    public void Hide_WhenLoadingCountReachesZero_ResetsIsLoadingAndText()
    {
        bool eventRaised = false;
        _sut.Show("Xử lý...");
        _sut.OnChange += () => eventRaised = true;

        _sut.Hide();

        _sut.IsLoading.Should().BeFalse();
        _sut.LoadingText.Should().Be("Đang tải...");
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void NestedShowAndHide_MaintainsIsLoadingUntilAllHidesCalled()
    {
        _sut.Show("Tác vụ 1");
        _sut.Show("Tác vụ 2");

        _sut.IsLoading.Should().BeTrue();
        _sut.LoadingText.Should().Be("Tác vụ 2");

        _sut.Hide(); // Still 1 active task
        _sut.IsLoading.Should().BeTrue();

        _sut.Hide(); // 0 active tasks
        _sut.IsLoading.Should().BeFalse();
        _sut.LoadingText.Should().Be("Đang tải...");
    }

    [Fact]
    public async Task ShowAsync_And_HideAsync_WorkAsynchronously()
    {
        await _sut.ShowAsync("Đang xử lý nền...");
        _sut.IsLoading.Should().BeTrue();
        _sut.LoadingText.Should().Be("Đang xử lý nền...");

        await _sut.HideAsync();
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SafetyTimeout_AutoDismissesLoadingState_WhenHideNeverCalled()
    {
        // Arrange: Service configured with short 100ms safety timeout
        var fastService = new LoadingService(safetyTimeoutMs: 100);
        fastService.Show("Tác vụ bị treo không gọi Hide...");

        fastService.IsLoading.Should().BeTrue();

        // Act: Wait slightly longer than safety timeout
        await Task.Delay(180);

        // Assert: System must have auto-dismissed to prevent permanent blocking
        fastService.IsLoading.Should().BeFalse();
        fastService.LoadingText.Should().Be("Đang tải...");
    }
}
