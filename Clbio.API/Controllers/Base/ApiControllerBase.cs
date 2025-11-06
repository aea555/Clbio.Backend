using Clbio.Abstractions.Interfaces.Services;
using Clbio.Domain.Entities.Base;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.Base
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ApiControllerBase<T> : ControllerBase where T : EntityBase
    {
        protected readonly IService<T> _service;

        protected ApiControllerBase(IService<T> service)
        {
            _service = service;
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("paged")]
        public virtual async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var (items, total) = await _service.GetPagedAsync(page, pageSize, ct);
            return Ok(new { page, pageSize, total, items });
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var entity = await _service.GetByIdAsync(id);
            return entity is null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] T entity, CancellationToken ct)
        {
            var created = await _service.CreateAsync(entity);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public virtual async Task<IActionResult> Update(Guid id, [FromBody] T entity, CancellationToken ct)
        {
            if (id != entity.Id) return BadRequest("Mismatched IDs");
            await _service.UpdateAsync(entity);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public virtual async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
