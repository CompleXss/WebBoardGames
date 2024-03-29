﻿using System.Text.Json.Serialization;

namespace webapi.Models;

public partial class UserGameStatistic
{
	[JsonIgnore]
	public long UserID { get; set; }

	[JsonIgnore]
	public long GameID { get; set; }

	public long PlayCount { get; set; }

	public long WinCount { get; set; }

	[JsonIgnore]
	public virtual Game Game { get; set; } = null!;

	public virtual User User { get; set; } = null!;
}