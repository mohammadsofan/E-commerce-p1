using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Application.DTOs;
using Ecommerce.Infrastructure.Persistence;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public OrdersController(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var items = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dto = _mapper.Map<List<OrderDto>>(items);
            return Ok(dto);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return Ok(_mapper.Map<OrderDto>(order));
        }
    }
}
