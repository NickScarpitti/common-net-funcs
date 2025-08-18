using System.ComponentModel.DataAnnotations;

namespace Web.Common.Tests.ValidationAttributes;

public abstract class ValidationTestBase
{
    protected readonly IFixture Fixture;
    protected readonly ValidationContext DummyValidationContext;

    protected ValidationTestBase()
    {
        Fixture = new Fixture();
        DummyValidationContext = new ValidationContext(new object());
    }

    protected static ValidationContext CreateValidationContext(string memberName)
    {
        return new ValidationContext(new object())
        {
            MemberName = memberName
        };
    }
}
