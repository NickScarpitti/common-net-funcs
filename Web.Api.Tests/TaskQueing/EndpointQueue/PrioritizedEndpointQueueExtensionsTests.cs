using CommonNetFuncs.Web.Api.TaskQueing.EndpointQueue;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Web.Api.Tests.TaskQueing.EndpointQueue;

public sealed class PrioritizedEndpointQueueExtensionsTests
{
    [Theory]
    [InlineData(TaskPriority.Low, null)]
    [InlineData(TaskPriority.Normal, "customKey")]
    public async Task ExecutePrioritizedAsync_Priority_Should_Invoke_Service(TaskPriority priority, string? customKey)
    {
        Mock<ControllerBase> controllerMock = new();
        Mock<IPrioritizedEndpointQueueService> serviceMock = new();

        serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), priority, It.IsAny<CancellationToken>()))
            .ReturnsAsync(321);

        int result = await controllerMock.Object.ExecutePrioritizedAsync(serviceMock.Object, _ => Task.FromResult(321), priority, customKey);

        result.ShouldBe(321);
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(2, "customKey")]
    public async Task ExecutePrioritizedAsync_CustomPriority_Should_Invoke_Service(int customPriority, string? customKey)
    {
        Mock<ControllerBase> controllerMock = new();
        Mock<IPrioritizedEndpointQueueService> serviceMock = new();

        serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), customPriority, It.IsAny<CancellationToken>()))
            .ReturnsAsync(654);

        int result = await controllerMock.Object.ExecutePrioritizedAsync(serviceMock.Object, _ => Task.FromResult(654), customPriority, customKey);

        result.ShouldBe(654);
    }
}
