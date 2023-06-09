﻿using System.Text.Json.Serialization;

namespace webapi.Models;

public partial class CheckersUser
{
	[JsonIgnore]
	public long UserId { get; set; }

	public long PlayCount { get; set; }

	public long WinCount { get; set; }

	public virtual User User { get; set; } = null!;
}
