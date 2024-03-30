namespace webapi.Errors;

public static class Errors
{
	/// <summary> Produces a <see cref="StatusCodes.Status400BadRequest"/> response. </summary>
	public static IResult BadRequest(string errorCode, string message, IEnumerable<object>? errors = null)
	{
		return Problem(StatusCodes.Status400BadRequest, errorCode, message, errors);
	}

	/// <summary> Produces a <see cref="StatusCodes.Status409Conflict"/> response. </summary>
	public static IResult Conflict(string errorCode, string message, IEnumerable<object>? errors = null)
	{
		return Problem(StatusCodes.Status409Conflict, errorCode, message, errors);
	}

	/// <summary> Produces a <see cref="StatusCodes.Status500InternalServerError"/> response. </summary>
	public static IResult ServerError(string errorCode, string message, IEnumerable<object>? errors = null)
	{
		return Problem(StatusCodes.Status500InternalServerError, errorCode, message, errors);
	}



	private static IResult Problem(int statusCode, string errorCode, string message, IEnumerable<object>? errors)
	{
		object data = errors is null
			? new { message, code = errorCode }
			: new { message, code = errorCode, errors };

		return Results.Json(statusCode: statusCode, data: data);
	}
}
