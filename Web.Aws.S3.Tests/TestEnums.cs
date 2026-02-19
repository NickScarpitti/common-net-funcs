namespace Web.Aws.S3.Tests;

public enum EExceptionScenario
{
	NotFound,
	InternalServerError,
	ServiceUnavailable,
	BadRequest,
	GeneralException
}

public enum EFileSize
{
	Small,    // 1KB
	Medium,   // 2KB
	Large,    // 15MB
	VeryLarge // 20MB
}

public enum EHttpStatusResult
{
	OK,
	NotFound,
	BadRequest
}

public enum EApiMethod
{
	UploadStream,
	UploadFilePath,
	GetStream,
	GetFilePath,
	Delete,
	Exists,
	GetUrl
}
