using System.Reflection;
using CommonNetFuncs.Web.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Web.Requests.Tests;

public enum SerializerSetting
{
	ContractResolver,
	ReferenceLoopHandling,
	NullValueHandling
}

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

	[Theory]
	[InlineData(SerializerSetting.ContractResolver)]
	[InlineData(SerializerSetting.ReferenceLoopHandling)]
	[InlineData(SerializerSetting.NullValueHandling)]
	public void JsonPatchInputFormatter_ShouldHaveCorrectSerializerSettings(SerializerSetting setting)
	{
		// Act
		NewtonsoftJsonPatchInputFormatter formatter = JsonPatchFormatter.JsonPatchInputFormatter();

		PropertyInfo? serializerSettingsProperty = formatter.GetType().GetProperty("SerializerSettings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		serializerSettingsProperty.ShouldNotBeNull();

		JsonSerializerSettings? serializerSettings = serializerSettingsProperty.GetValue(formatter) as JsonSerializerSettings;
		serializerSettings.ShouldNotBeNull();

		// Assert
		switch (setting)
		{
			case SerializerSetting.ContractResolver:
				serializerSettings.ContractResolver.ShouldBeOfType<DefaultContractResolver>();
				break;
			case SerializerSetting.ReferenceLoopHandling:
				serializerSettings.ReferenceLoopHandling.ShouldBe(ReferenceLoopHandling.Ignore);
				break;
			case SerializerSetting.NullValueHandling:
				serializerSettings.NullValueHandling.ShouldBe(NullValueHandling.Ignore);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(setting));
		}
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
