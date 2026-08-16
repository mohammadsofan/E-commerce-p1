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
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public ProductsController(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var items = await _db.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dto = _mapper.Map<List<ProductDto>>(items);
            return Ok(dto);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(_mapper.Map<ProductDto>(product));
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return BadRequest();
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug);
            if (product == null) return NotFound();
            return Ok(_mapper.Map<ProductDto>(product));
        }
    }
}
