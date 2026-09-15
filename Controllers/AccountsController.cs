using FinanceApi.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AccountsController : ControllerBase
{
    [HttpGet("error")]
    public IActionResult GetError()
    {
        throw new NotImplementedException("This endpoint is not yet implemented");
    }

    [HttpGet("notfound")]
    public IActionResult GetNotFound()
    {
        throw new KeyNotFoundException("The requested account was not found");
    }

    [HttpGet("invalid")]
    public IActionResult GetInvalid()
    {
        throw new ArgumentException("Invalid account parameters provided");
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetAccountById(int id)
    {
        throw new NotFoundException($"Account with id {id} was not found");
    }

    [HttpGet("validate")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult GetValidate()
    {
        throw new ValidationException("One or more fields are invalid", new Dictionary<string, string[]>
        {
            ["amount"] = ["Amount must be greater than zero."]
        });
    }

    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetAccounts()
    {
        return Ok(new { Message = "This endpoint works correctly" });
    }
}