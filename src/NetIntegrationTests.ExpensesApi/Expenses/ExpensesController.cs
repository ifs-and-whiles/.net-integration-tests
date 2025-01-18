using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetIntegrationTests.Database;
using NetIntegrationTests.Expenses;
using NetIntegrationTests.Services;
using Contracts = NetIntegrationTests.Expenses.Contracts;

[Route("expenses")]
public class ExpensesController(
    ExpensesRepository expensesRepository,
    UsersService usersService,
    IBus bus) : ControllerBase
{
    [HttpPost("get-expense")]
    [ProducesResponseType(typeof(Contracts.Expenses.V1.Get.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<IActionResult> GetExpense([FromBody] Contracts.Expenses.V1.Get.Request request)
    {
        var expense = await expensesRepository.GetExpenseById(request.Id);

        if (expense == null)
        {
            return NotFound();
        }
        
        return Ok(new Contracts.Expenses.V1.Get.Response()
        {
            Id = expense.Id,
            Name = expense.Name,
            Amount = expense.Amount,
        });
    }

    [HttpPost("create-expense")]
    [ProducesResponseType(typeof(Contracts.Expenses.V1.Create.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<IActionResult> CreateExpense([FromBody] Contracts.Expenses.V1.Create.Request request)
    {
        if(string.IsNullOrEmpty(request.Name))
            return BadRequest("Name is required");
        
        if(request.Amount <= 0)
            return BadRequest("Amount must be greater than 0");

        var user = await usersService.GetUser(request.UserId);
        
        if(user == null)
            return BadRequest("User does not exist");
        
        if(user.ExpensesCount == user.MaxExpenseCount)
            return BadRequest("User has reached max expense count");
        
        var expenseId = Guid.NewGuid();

        await expensesRepository.SaveExpense(
            expenseId,
            request.Name,
            request.Amount, 
            request.UserId);
        
        await bus.Publish(new ExpenseCreatedEvent()
        {
            Id = expenseId,
            UserId = request.UserId
        });
        
        return Ok(new Contracts.Expenses.V1.Create.Response()
        {
            Id = expenseId
        });
    }

    [HttpPost("delete-expense")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<IActionResult> DeleteExpense([FromBody] Contracts.Expenses.V1.Delete.Request request)
    {
        var expense = await expensesRepository.GetExpenseById(request.Id);

        if (expense == null)
        {
            return NotFound();
        }
        
        await expensesRepository.DeleteExpense(request.Id);
        
        return Ok();
    }
}