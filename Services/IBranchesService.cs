using APIlog.Server.DTOs.Branches;

namespace APIlog.Server.Services;

public interface IBranchesService
{
    Task<IEnumerable<BranchListItemDto>> GetBranchesAsync();
    Task<BranchListItemDto> CreateBranchAsync(CreateBranchDto dto);
    Task<BranchListItemDto> UpdateBranchAsync(int id, UpdateBranchDto dto);
    Task DeleteBranchAsync(int id);
}
