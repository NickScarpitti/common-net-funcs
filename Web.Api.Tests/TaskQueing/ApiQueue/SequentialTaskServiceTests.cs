using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueing.ApiQueue;
using Moq;

namespace Web.Api.Tests.TaskQueing.ApiQueue;

public class SequentialTaskServiceTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Invoke_Processor()
    {
        Mock<SequentialTaskProcessor> processorMock = new(MockBehavior.Strict, new BoundedChannelOptions(1), 1000);
        processorMock.Setup(x => x.EnqueueAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(99);

        SequentialTaskService service = new(processorMock.Object);

        int result = await service.ExecuteAsync(_ => Task.FromResult(99));

        result.ShouldBe(99);
        processorMock.Verify(x => x.EnqueueAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()), Moq.Times.Once);
    }
}
