using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Xunit.TestContext;

namespace EFCore.Tests;

public sealed partial class BaseDbContextActionsTests
{
	private readonly IServiceProvider serviceProvider;
	private readonly Fixture fixture;
	private readonly TestDbContext context;

	public BaseDbContextActionsTests()
	{
		fixture = new Fixture();
		fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(x => fixture.Behaviors.Remove(x));
		fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		ServiceCollection services = new();
		services.AddDbContextPool<TestDbContext>(options => options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
		serviceProvider = services.BuildServiceProvider();
		context = serviceProvider.GetRequiredService<TestDbContext>();
	}
}

