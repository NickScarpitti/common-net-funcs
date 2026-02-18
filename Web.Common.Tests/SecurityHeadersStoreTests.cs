using CommonNetFuncs.Web.Common;

namespace Web.Common.Tests;

public class SecurityHeadersStoreTests
{
	[Fact]
	public void XrfHeader_HasExpectedValue()
	{
		// Assert
		SecurityHeadersStore.XrfHeader.ShouldBe("X-XSRF-TOKEN");
	}

	[Fact]
	public void SecurityHeaders_IsNotNull()
	{
		// Assert
		SecurityHeadersStore.SecurityHeaders.ShouldNotBeNull();
	}

	[Fact]
	public void SecurityHeaders_ContainsExpectedHeaders()
	{
		// Assert
		SecurityHeadersStore.SecurityHeaders.Count.ShouldBe(6);

		SecurityHeadersStore.SecurityHeaders.ShouldContainKey("X-Xss-Protection");
		SecurityHeadersStore.SecurityHeaders.ShouldContainKey("X-Frame-Options");
		SecurityHeadersStore.SecurityHeaders.ShouldContainKey("Referrer-Policy");
		SecurityHeadersStore.SecurityHeaders.ShouldContainKey("X-Content-Type-Options");
		SecurityHeadersStore.SecurityHeaders.ShouldContainKey("X-Permitted-Cross-Domain-Policies");
		SecurityHeadersStore.SecurityHeaders.ShouldContainKey("Content-Security-Policy");
	}

	[Fact]
	public void SecurityHeaders_XssProtection_HasCorrectValue()
	{
		// Assert
		SecurityHeadersStore.SecurityHeaders["X-Xss-Protection"].ShouldBe("1; mode=block");
	}

	[Fact]
	public void SecurityHeaders_XFrameOptions_HasCorrectValue()
	{
		// Assert
		SecurityHeadersStore.SecurityHeaders["X-Frame-Options"].ShouldBe("DENY");
	}

	[Fact]
	public void SecurityHeaders_ReferrerPolicy_HasCorrectValue()
	{
		// Assert
		SecurityHeadersStore.SecurityHeaders["Referrer-Policy"].ShouldBe("no-referrer");
	}

	[Fact]
	public void SecurityHeaders_XContentTypeOptions_HasCorrectValue()
	{
		// Assert
		SecurityHeadersStore.SecurityHeaders["X-Content-Type-Options"].ShouldBe("nosniff");
	}

	[Fact]
	public void SecurityHeaders_XPermittedCrossDomainPolicies_HasCorrectValue()
	{
		// Assert
		SecurityHeadersStore.SecurityHeaders["X-Permitted-Cross-Domain-Policies"].ShouldBe("none");
	}

	[Fact]
	public void SecurityHeaders_ContentSecurityPolicy_HasCorrectValue()
	{
		// Arrange
		string expectedValue = "block-all-mixed-content; upgrade-insecure-requests; script-src 'self' http://cdnjs.cloudflare.com/ http://cdn.jsdelivr.net/; object-src 'self';";

		// Assert
		SecurityHeadersStore.SecurityHeaders["Content-Security-Policy"].ShouldBe(expectedValue);
	}

	[Fact]
	public void SecurityHeaders_IsFrozenDictionary()
	{
		// Assert
		SecurityHeadersStore.SecurityHeaders.GetType().Name.ShouldContain("FrozenDictionary");
	}

	[Fact]
	public void SecurityHeaders_IsImmutable()
	{
		// Assert - FrozenDictionary is immutable by nature
		// Verify it implements IReadOnlyDictionary
		SecurityHeadersStore.SecurityHeaders.ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
	}

	[Fact]
	public void HeadersToRemove_IsNotNull()
	{
		// Assert
		SecurityHeadersStore.HeadersToRemove.ShouldNotBeNull();
	}

	[Fact]
	public void HeadersToRemove_ContainsExpectedHeaders()
	{
		// Assert
		SecurityHeadersStore.HeadersToRemove.Count.ShouldBe(2);
		SecurityHeadersStore.HeadersToRemove.ShouldContain("Server");
		SecurityHeadersStore.HeadersToRemove.ShouldContain("X-Powered-By");
	}

	[Fact]
	public void HeadersToRemove_Server_IsPresent()
	{
		// Assert
		SecurityHeadersStore.HeadersToRemove.ShouldContain("Server");
	}

	[Fact]
	public void HeadersToRemove_XPoweredBy_IsPresent()
	{
		// Assert
		SecurityHeadersStore.HeadersToRemove.ShouldContain("X-Powered-By");
	}

	[Theory]
	[InlineData("X-Xss-Protection", "1; mode=block")]
	[InlineData("X-Frame-Options", "DENY")]
	[InlineData("Referrer-Policy", "no-referrer")]
	[InlineData("X-Content-Type-Options", "nosniff")]
	[InlineData("X-Permitted-Cross-Domain-Policies", "none")]
	public void SecurityHeaders_AllHeaders_CanBeAccessedByKey(string headerName, string expectedValue)
	{
		// Assert
		SecurityHeadersStore.SecurityHeaders.TryGetValue(headerName, out var value).ShouldBeTrue();
		value.ShouldBe(expectedValue);
	}

	[Fact]
	public void SecurityHeaders_NonExistentKey_ReturnsNull()
	{
		// Act
		var exists = SecurityHeadersStore.SecurityHeaders.TryGetValue("NonExistent-Header", out var value);

		// Assert
		exists.ShouldBeFalse();
		value.ShouldBeNull();
	}

	[Fact]
	public void HeadersToRemove_CanBeEnumerated()
	{
		// Arrange
		var headersList = new List<string>();

		// Act
		foreach (var header in SecurityHeadersStore.HeadersToRemove)
		{
			headersList.Add(header);
		}

		// Assert
		headersList.Count.ShouldBe(2);
		headersList.ShouldContain("Server");
		headersList.ShouldContain("X-Powered-By");
	}
}
