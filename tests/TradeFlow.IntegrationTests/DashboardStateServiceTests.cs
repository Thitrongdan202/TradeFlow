namespace TradeFlow.IntegrationTests;

using FluentAssertions;
using TradeFlow.Application.Common.Models.Notifications;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Web.Services;
using Xunit;

public class DashboardStateServiceTests
{
    [Fact]
    public void InitialState_ShouldNotBeLoaded_AndHaveEmptyCollections()
    {
        // Arrange & Act
        var service = new DashboardStateService();

        // Assert
        service.HasLoaded.Should().BeFalse();
        service.Stats.Should().BeEmpty();
        service.RecentActivities.Should().BeEmpty();
        service.Notifications.Should().BeEmpty();
        service.LastUpdatedUtc.Should().BeNull();
    }

    [Fact]
    public void SetState_ShouldPopulateData_AndUpdateFlags()
    {
        // Arrange
        var service = new DashboardStateService();
        var stats = new List<DashboardStatItem>
        {
            new("Đơn bán hàng hôm nay", "12", "12 đơn phát sinh", "primary"),
            new("Doanh thu tháng này", "50,000,000 ₫", "Hóa đơn trong tháng", "success")
        };
        var activities = new List<AuditLog>
        {
            new(TradeFlow.Domain.Enums.AuditEventType.Login, "admin", null, null, "User logged in", null, null)
        };
        var notifications = new List<SystemNotificationDto>
        {
            new() { Id = "1", Title = "Thông báo đơn hàng mới", Message = "Có đơn hàng cần xác nhận" }
        };

        // Act
        service.SetState(stats, activities, notifications);

        // Assert
        service.HasLoaded.Should().BeTrue();
        service.LastUpdatedUtc.Should().NotBeNull();
        service.Stats.Should().HaveCount(2);
        service.Stats[0].Label.Should().Be("Đơn bán hàng hôm nay");
        service.RecentActivities.Should().HaveCount(1);
        service.Notifications.Should().HaveCount(1);
    }

    [Fact]
    public void Invalidate_ShouldResetLoadedState_AndClearData()
    {
        // Arrange
        var service = new DashboardStateService();
        service.SetState(
            [new("Test", "100", "Meta", "primary")],
            [],
            []
        );
        service.HasLoaded.Should().BeTrue();

        // Act
        service.Invalidate();

        // Assert
        service.HasLoaded.Should().BeFalse();
        service.Stats.Should().BeEmpty();
        service.RecentActivities.Should().BeEmpty();
        service.Notifications.Should().BeEmpty();
    }
}
