using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetIntegrationTests.UsersApi.Database;

namespace NetIntegrationTests.UsersApi.Users;

[Route("users")]
public class UsersController : ControllerBase
{
    private readonly UsersRepository _usersRepository;

    public UsersController(UsersRepository usersRepository)
    {
        _usersRepository = usersRepository;
    }

    [HttpPost("create-user")]
    [ProducesResponseType(typeof(Contracts.Users.V1.Create.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateUser([FromBody] Contracts.Users.V1.Create.Request request)
    {
        var userId = Guid.NewGuid();
        
        if(string.IsNullOrEmpty(request.Name))
            return BadRequest("Name is required");

        var doesUserExist = _usersRepository.DoesUserExist(request.Name);
        
        if(doesUserExist)
            return BadRequest($"User already exists for name: {request.Name}");
        
        await _usersRepository.CreateUser(
            name: request.Name, 
            id: userId,
            expensesCount: 0,
            maxExpenseCount: 5);

        return Ok(new Contracts.Users.V1.Create.Response()
        {
            Id = userId
        });
    }

    [HttpPost("get-user")]
    [ProducesResponseType(typeof(Contracts.Users.V1.Get.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser([FromBody] Contracts.Users.V1.Get.Request request)
    {
        var user = await _usersRepository.GetUser(request.Id);

        if (user == null)
            return NotFound("User not found");

        return Ok(user);
    }

    [HttpPost("increment-expenses-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IncrementExpensesCount([FromBody] Contracts.Users.V1.IncrementExpensesCount.Request request)
    {
        var user = await _usersRepository.GetUser(request.UserId);

        if (user == null)
            return NotFound("User not found");

        await _usersRepository.IncrementExpensesCount(request.UserId);

        return Ok();
    }
}