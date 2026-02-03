using System.Reflection;
using CommonNetFuncs.Web.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Web.Requests.Tests;

public class JsonPatchFormatterTests
{
	[Fact]
	public void JsonPatchInputFormatter_ShouldReturn_NewtonsoftJsonPatchInputFormatter()
	{
		// Act
		NewtonsoftJsonPatchInputFormatter formatter = JsonPatchFormatter.JsonPatchInputFormatter();

		// Assert
		formatter.ShouldNotBeNull();
		formatter.ShouldBeOfType<NewtonsoftJsonPatchInputFormatter>();
	}

	[Fact]
	public void JsonPatchInputFormatter_ShouldHave_DefaultContractResolver()
	{
		// Act
		NewtonsoftJsonPatchInputFormatter formatter = JsonPatchFormatter.JsonPatchInputFormatter();

		// Use reflection to access protected SerializerSettings
		PropertyInfo? serializerSettingsProperty = formatter.GetType().GetProperty("SerializerSettings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		serializerSettingsProperty.ShouldNotBeNull();

		JsonSerializerSettings? serializerSettings = serializerSettingsProperty.GetValue(formatter) as Newtonsoft.Json.JsonSerializerSettings;
		serializerSettings.ShouldNotBeNull();

		serializerSettings.ContractResolver.ShouldBeOfType<DefaultContractResolver>();
	}

	[Fact]
	public void JsonPatchInputFormatter_ShouldHave_ReferenceLoopHandling_Ignore()
	{
		// Act
		NewtonsoftJsonPatchInputFormatter formatter = JsonPatchFormatter.JsonPatchInputFormatter();

		PropertyInfo? serializerSettingsProperty = formatter.GetType().GetProperty("SerializerSettings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		JsonSerializerSettings? serializerSettings = serializerSettingsProperty?.GetValue(formatter) as Newtonsoft.Json.JsonSerializerSettings;
		serializerSettings.ShouldNotBeNull();

		serializerSettings.ReferenceLoopHandling.ShouldBe(ReferenceLoopHandling.Ignore);
	}

	[Fact]
	public void JsonPatchInputFormatter_ShouldHave_NullValueHandling_Ignore()
	{
		// Act
		NewtonsoftJsonPatchInputFormatter formatter = JsonPatchFormatter.JsonPatchInputFormatter();

		PropertyInfo? serializerSettingsProperty = formatter.GetType().GetProperty("SerializerSettings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

		JsonSerializerSettings? serializerSettings = serializerSettingsProperty?.GetValue(formatter) as Newtonsoft.Json.JsonSerializerSettings;
		serializerSettings.ShouldNotBeNull();

		serializerSettings.NullValueHandling.ShouldBe(NullValueHandling.Ignore);
	}

	[Theory]
	[InlineData(typeof(NewtonsoftJsonPatchInputFormatter))]
	public void JsonPatchInputFormatter_ShouldBe_FirstInputFormatterOfType(Type expectedType)
	{
		// Arrange
		_ = JsonPatchFormatter.JsonPatchInputFormatter();

		// Act
		ServiceProvider serviceProvider = new ServiceCollection()
			.AddLogging()
			.AddMvc()
			.AddNewtonsoftJson()
			.Services.BuildServiceProvider();

		MvcOptions mvcOptions = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Value;
		NewtonsoftJsonPatchInputFormatter? firstPatchFormatter = mvcOptions.InputFormatters.OfType<NewtonsoftJsonPatchInputFormatter>().FirstOrDefault();

		// Assert
		firstPatchFormatter.ShouldNotBeNull();
		firstPatchFormatter.ShouldBeOfType(expectedType);
	}
}
