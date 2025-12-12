using AutoMapper;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.API.Extensions;
using Clbio.Application.DTOs.V1.Base;
using Clbio.Domain.Entities.V1.Base;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1.Base
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ApiControllerBase<TEntity, TResponseDto, TCreateDto, TUpdateDto>(
        IService<TEntity> service,
        IMapper mapper)
        : ControllerBase
        where TEntity : EntityBase
        where TResponseDto : ResponseDtoBase
        where TCreateDto : RequestDtoBase
        where TUpdateDto : RequestDtoBase
    {
        protected readonly IService<TEntity> _service = service;
        protected readonly IMapper _mapper = mapper;

        [HttpGet]
        public virtual async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _service.GetAllAsync(ct);
            if (!result.Success)
                return BadRequest(ApiResponse<List<TResponseDto>>.Fail(result.Error!, result.Code));

            var dtoList = _mapper.Map<List<TResponseDto>>(result.Value);
            return Ok(ApiResponse<List<TResponseDto>>.Ok(dtoList));
        }

        [HttpGet("paged")]
        public virtual async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            // only supports sorting by createdAt for now
            var result = await _service.GetPagedAsync(page, pageSize, q => q.OrderBy(x => x.CreatedAt), ct);
            if (!result.Success)
                return BadRequest(ApiResponse<object>.Fail(result.Error!, result.Code));

            var (items, total) = result.Value;
            var dtoList = _mapper.Map<List<TResponseDto>>(items);

            return Ok(ApiResponse<object>.Ok(new { page, pageSize, total, items = dtoList }));
        }

        [HttpGet("{id:guid}")]
        public virtual async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var result = await _service.GetByIdAsync(id, ct);
            if (!result.Success)
                return BadRequest(ApiResponse<TResponseDto>.Fail(result.Error!, result.Code));

            if (result.Value is null)
                return NotFound(ApiResponse<TResponseDto>.Fail("Entity not found", "NOT_FOUND"));

            var dto = _mapper.Map<TResponseDto>(result.Value);
            return Ok(ApiResponse<TResponseDto>.Ok(dto));
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] TCreateDto dto, CancellationToken ct)
        {
            var entity = _mapper.Map<TEntity>(dto);
            var result = await _service.CreateAsync(entity, ct);

            if (!result.Success)
                return BadRequest(ApiResponse<TResponseDto>.Fail(result.Error!, result.Code));

            var responseDto = _mapper.Map<TResponseDto>(result.Value);
            return CreatedAtAction(nameof(Get), new { id = responseDto.GetType().GetProperty("Id")?.GetValue(responseDto) }, ApiResponse<TResponseDto>.Ok(responseDto));
        }

        [HttpPut("{id:guid}")]
        public virtual async Task<IActionResult> Update(Guid id, [FromBody] TUpdateDto dto, CancellationToken ct)
        {
            var entity = _mapper.Map<TEntity>(dto);
            if (id != entity.Id)
                return BadRequest(ApiResponse<TResponseDto>.Fail("Mismatched IDs"));

            var result = await _service.UpdateAsync(entity, ct);
            return result.Success ? NoContent() : BadRequest(ApiResponse<TResponseDto>.Fail(result.Error!, result.Code));
        }

        [HttpDelete("{id:guid}")]
        public virtual async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _service.DeleteAsync(id, ct);
            return result.Success ? NoContent() : BadRequest(ApiResponse<object>.Fail(result.Error!, result.Code));
        }
    }
}
