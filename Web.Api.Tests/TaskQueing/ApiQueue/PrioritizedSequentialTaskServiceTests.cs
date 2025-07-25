using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueing.ApiQueue;
using CommonNetFuncs.Web.Api.TaskQueing.EndpointQueue;
using Moq;

namespace Web.Api.Tests.TaskQueing.ApiQueue;

public class PrioritizedSequentialTaskServiceTests
{
    [Theory]
    [InlineData(1, TaskPriority.Normal)]
    [InlineData(2, TaskPriority.High)]
    public async Task ExecuteAsync_Should_Invoke_Processor(int priority, TaskPriority priorityLevel)
    {
        Mock<PrioritizedSequentialTaskProcessor> processorMock = new(MockBehavior.Strict, new BoundedChannelOptions(1), 1000);
        processorMock.Setup(x => x.EnqueueWithPriorityAsync(It.IsAny<Func<CancellationToken, Task<int?>>>(), priority, priorityLevel, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priority);

        PrioritizedSequentialTaskService service = new(processorMock.Object);

        object result = await service.ExecuteAsync(_ => Task.FromResult<int?>(priority), priority, priorityLevel);

        result.ShouldBe(priority);
        processorMock.Verify(x => x.EnqueueWithPriorityAsync(It.IsAny<Func<CancellationToken, Task<int?>>>(), priority, priorityLevel, null, It.IsAny<CancellationToken>()), Moq.Times.Once);
    }
}
