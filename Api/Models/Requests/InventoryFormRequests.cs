using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace API.Models.Requests;

public class CreateInventoryTransactionFormRequest
{
    [Required]
    public string RequestJson { get; set; } = string.Empty;

    public List<IFormFile>? EvidenceFiles { get; set; }
}
