//using System.Text.Json;

//namespace DeepClone.Tests;

//public sealed class SerializeTests
//{
//	private readonly Fixture _fixture;

//	public SerializeTests()
//	{
//		_fixture = new Fixture();
//	}

//	public sealed class TestClass
//	{
//		public int Number { get; set; }

//		public string? Text { get; set; }

//		public List<int>? Numbers { get; set; }

//		public TestClass? Child { get; set; }

//		public DateTime TimeStamp { get; set; }
//	}

//	public sealed class NonSerializableClass
//	{
//		public Action? Callback { get; set; }

//		public Stream? DataStream { get; set; }
//	}

//	[Fact]
//	public void DeepClone_WhenInputIsNull_ShouldReturnNull()
//	{
//		// Arrange
//		TestClass? source = null;

//		// Act
//#pragma warning disable CS0618 // Type or member is obsolete
//		TestClass? result = source.DeepCloneS();
//#pragma warning restore CS0618 // Type or member is obsolete

//		// Assert
//		result.ShouldBeNull();
//	}

//	[Theory]
//	[InlineData("Simple string")]
//	[InlineData(null)]
//	public void DeepClone_WhenInputIsString_ShouldCreateNewInstance(string? input)
//	{
//		// Act
//#pragma warning disable CS0618 // Type or member is obsolete
//		string? result = input.DeepCloneS();
//#pragma warning restore CS0618 // Type or member is obsolete

//		// Assert
//		result.ShouldBe(input);
//		if (input is not null)
//		{
//			ReferenceEquals(result, input).ShouldBeFalse();
//		}
//	}

//	[Fact]
//	public void DeepClone_WhenInputIsSimpleClass_ShouldCreateDeepCopy()
//	{
//		// Arrange
//		TestClass source = _fixture.Build<TestClass>()
//				.Without(x => x.Child)
//				.Without(x => x.Numbers)
//				.Create();

//		// Act
//#pragma warning disable CS0618 // Type or member is obsolete
//		TestClass result = source.DeepCloneS();
//#pragma warning restore CS0618 // Type or member is obsolete

//		// Assert
//		result.ShouldNotBeSameAs(source);
//		result.Number.ShouldBe(source.Number);
//		result.Text.ShouldBe(source.Text);
//		result.TimeStamp.ShouldBe(source.TimeStamp);
//	}

//	[Fact]
//	public void DeepClone_WhenInputHasCollection_ShouldCreateDeepCopy()
//	{
//		// Arrange
//		TestClass source = new()
//		{
//			Numbers = [1, 2, 3, 4, 5]
//		};

//		// Act
//#pragma warning disable CS0618 // Type or member is obsolete
//		TestClass result = source.DeepCloneS();
//#pragma warning restore CS0618 // Type or member is obsolete

//		// Assert
//		result.Numbers.ShouldNotBeSameAs(source.Numbers);
//		result.Numbers!.Count.ShouldBe(source.Numbers.Count);
//		result.Numbers.SequenceEqual(source.Numbers).ShouldBeTrue();
//	}

//	[Fact]
//	public void DeepClone_WhenInputHasCircularReference_ShouldThrowJsonException()
//	{
//		// Arrange
//		TestClass source = new()
//		{
//			Number = 42,
//			Text = "parent"
//		};
//		source.Child = new()
//		{
//			Number = 24,
//			Text = "child",
//			Child = source // Create circular reference
//		};

//		// Act & Assert
//#pragma warning disable CS0618 // Type or member is obsolete
//		Should.Throw<JsonException>(() => source.DeepCloneS());
//#pragma warning restore CS0618 // Type or member is obsolete
//	}

//	[Fact]
//	public void DeepClone_WhenInputHasNonSerializableMembers_ShouldThrowJsonException()
//	{
//		// Arrange
//		NonSerializableClass source = new()
//		{
//			Callback = () => Console.WriteLine("Hello"),
//			DataStream = new MemoryStream()
//		};

//		// Act & Assert
//#pragma warning disable CS0618 // Type or member is obsolete
//		Should.Throw<NotSupportedException>(() => source.DeepCloneS());
//#pragma warning restore CS0618 // Type or member is obsolete
//	}

//	[Theory]
//	[MemberData(nameof(GetComplexObjects))]
//	public void DeepClone_WhenInputIsComplexObject_ShouldCreateDeepCopy(TestClass source)
//	{
//		// Act
//#pragma warning disable CS0618 // Type or member is obsolete
//		TestClass result = source.DeepCloneS();
//#pragma warning restore CS0618 // Type or member is obsolete

//		// Assert
//		result.ShouldNotBeSameAs(source);
//		ValidateDeepClone(result, source);
//	}

//	private static void ValidateDeepClone(TestClass? result, TestClass? source)
//	{
//		if (source is null)
//		{
//			result.ShouldBeNull();
//			return;
//		}

//		result.ShouldNotBeNull();
//		result.Number.ShouldBe(source.Number);
//		result.Text.ShouldBe(source.Text);
//		result.TimeStamp.ShouldBe(source.TimeStamp);

//		if (source.Numbers is not null)
//		{
//			result.Numbers.ShouldNotBeNull();
//			result.Numbers.ShouldNotBeSameAs(source.Numbers);
//			result.Numbers.SequenceEqual(source.Numbers).ShouldBeTrue();
//		}
//		else
//		{
//			result.Numbers.ShouldBeNull();
//		}

//		if (source.Child is not null)
//		{
//			result.Child.ShouldNotBeNull();
//			result.Child.ShouldNotBeSameAs(source.Child);
//			ValidateDeepClone(result.Child, source.Child);
//		}
//		else
//		{
//			result.Child.ShouldBeNull();
//		}
//	}

//	public static IEnumerable<object[]> GetComplexObjects()
//	{
//		yield return
//		[
//				new TestClass
//				{
//						Number = 1,
//						Text = "Parent",
//						Numbers = [1, 2, 3],
//						TimeStamp = DateTime.UtcNow,
//						Child = new TestClass
//								{
//										Number = 2,
//										Text = "Child",
//										Numbers = [4, 5, 6],
//										TimeStamp = DateTime.UtcNow.AddDays(-1)
//								}
//				}];

//		yield return [new TestClass { Number = 42, Text = null, Numbers = [], TimeStamp = DateTime.UtcNow, Child = null }];
//	}
//}
