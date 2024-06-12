using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionService.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;

namespace TransactionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly TransactionContext _context;

        public TransactionController(TransactionContext context)
        {
            _context = context;
        }

        
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User ID is missing in the token.");
            }
            var userIdInt = int.Parse(userId);

            var balance = await _context.Transactions
                .Where(t => t.UserId == userIdInt)
                .FirstOrDefaultAsync();

            return Ok(balance);
        }

        [Authorize]
[HttpPost("assign-balance")]
public async Task<IActionResult> AssignBalance([FromBody] AssignBalanceRequest request)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null)
    {
        return Unauthorized("User ID is missing in the token.");
    }
    int userIdInt = int.Parse(userId);

    var existingTransaction = await _context.Transactions
        .Where(t => t.UserId == userIdInt)
        .FirstOrDefaultAsync();

    if (existingTransaction != null)
    {
        existingTransaction.Balance = request.Amount;
        _context.Transactions.Update(existingTransaction);
        await _context.SaveChangesAsync();
        return Ok(existingTransaction);
    }
    else
    {
        var newTransaction = new Transaction
        {
            UserId = userIdInt,
            Balance = request.Amount,

        };
        _context.Transactions.Add(newTransaction);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBalance), new { id = newTransaction.Id }, newTransaction);
    }
    }


        [Authorize]
        [HttpPut("deduct-balance")]
public async Task<IActionResult> DeductBalance([FromBody] BalanceRequestDTO request)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null)
    {
        return Unauthorized("User ID is missing in the token.");
    }
    int userIdInt = int.Parse(userId);

    var existingTransaction = await _context.Transactions
        .Where(t => t.UserId == userIdInt)
        .FirstOrDefaultAsync();

    if (existingTransaction == null || existingTransaction.Balance < request.Amount)
    {
        return BadRequest("Insufficient balance.");
    }

    existingTransaction.Balance -= request.Amount;
    _context.Transactions.Update(existingTransaction);
    await _context.SaveChangesAsync();

    return Ok(existingTransaction);
}
[Authorize]
[HttpPut("refund-balance")]
public async Task<IActionResult> RefundBalance([FromBody] BalanceRequestDTO request)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null)
    {
        return Unauthorized("User ID is missing in the token.");
    }
    int userIdInt = int.Parse(userId);

    var existingTransaction = await _context.Transactions
        .Where(t => t.UserId == userIdInt)
        .FirstOrDefaultAsync();

    if (existingTransaction == null)
    {
        return BadRequest("Transaction not found.");
    }

    existingTransaction.Balance += request.Amount;
    _context.Transactions.Update(existingTransaction);
    await _context.SaveChangesAsync();

    return Ok(existingTransaction);
}


    }
}
