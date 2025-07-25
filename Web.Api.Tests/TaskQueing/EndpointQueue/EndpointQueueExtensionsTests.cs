using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueing.EndpointQueue;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Web.Api.Tests.TaskQueing.EndpointQueue;

public class EndpointQueueExtensionsTests
{
    [Fact]
    public async Task ExecuteQueuedAsync_Bounded_Should_Invoke_Service()
    {
        Mock<ControllerBase> controllerMock = new();
        Mock<IEndpointQueueService> serviceMock = new();
        BoundedChannelOptions options = new(1);

        serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(123);

        int result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(123), options);

        result.ShouldBe(123);
    }

    [Fact]
    public async Task ExecuteQueuedAsync_Unbounded_Should_Invoke_Service()
    {
        Mock<ControllerBase> controllerMock = new();
        Mock<IEndpointQueueService> serviceMock = new();
        UnboundedChannelOptions options = new();

        serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(456);

        int result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(456), options);

        result.ShouldBe(456);
    }
}
