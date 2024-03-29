using webapi.Services;

namespace webapi.Extensions;

public static class HttpExtensions
{
	public static string? GetAccessTokenCookie(this HttpRequest request)
	{
		return request.Cookies[AuthService.ACCESS_TOKEN_COOKIE_NAME];
	}

	public static string? GetRefreshTokenCookie(this HttpRequest request)
	{
		return request.Cookies[AuthService.REFRESH_TOKEN_COOKIE_NAME];
	}

	public static string? GetDeviceIdCookie(this HttpRequest request)
	{
		return request.Cookies[AuthService.DEVICE_ID_COOKIE_NAME];
	}
}
