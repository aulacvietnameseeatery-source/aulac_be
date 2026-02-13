public class AccountListDTO
{
	public long AccountId { get; set; }

	public string FullName { get; set; } = "";
	public string? Phone { get; set; }
	public string? Email { get; set; }

	public long RoleId { get; set; }
	public string RoleName { get; set; } = "";

	/// <summary>
	/// 1 = ACTIVE, 2 = INACTIVE, 3 = LOCKED
	/// </summary>
	public int AccountStatus { get; set; }

	public string AccountStatusName { get; set; } = null!;
}

public class AccountListQueryDTO
{
	public string? Search { get; set; }      // search by FullName/Email/Phone/Username
	public long? RoleId { get; set; }
	public int? AccountStatus { get; set; }  // 1/2/3

	public int PageIndex { get; set; } = 1;
	public int PageSize { get; set; } = 10;
}
